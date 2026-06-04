using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WordReminder.Models;

namespace WordReminder.Services;

/// <summary>
/// AI 聊天服务 - 支持流式响应的对话 API 调用
/// </summary>
public class ChatAIService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigService _configService;
    private readonly ILogger<ChatAIService> _logger;

    public ChatAIService(ConfigService configService, ILogger<ChatAIService> logger)
    {
        _configService = configService;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
    }

    /// <summary>
    /// 发送消息并异步流式接收响应
    /// </summary>
    public async IAsyncEnumerable<string> SendMessageAsync(
        Assistant assistant,
        List<ChatMessage> history,
        string userMessage,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var provider = _configService.Settings.AIProviders
            .FirstOrDefault(p => p.Name == assistant.ProviderName);

        if (provider == null || string.IsNullOrEmpty(provider.ApiKey) || provider.ApiKey == "your-api-key-here")
            throw new InvalidOperationException("AI 未配置，请先在设置中配置 API Key");

        var modelId = assistant.ModelId;
        if (string.IsNullOrEmpty(modelId))
            modelId = _configService.GetActiveModelId();

        // 构建消息数组
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(assistant.SystemPrompt))
            messages.Add(new { role = "system", content = assistant.SystemPrompt });
        foreach (var msg in history)
            messages.Add(new { role = msg.Role, content = msg.Content });
        messages.Add(new { role = "user", content = userMessage });

        // 构建请求体
        var bodyDict = new Dictionary<string, object?>
        {
            ["model"] = modelId,
            ["messages"] = messages.ToArray(),
            ["temperature"] = assistant.Temperature,
            ["stream"] = true
        };

        if (assistant.MaxTokens > 0)
            bodyDict["max_tokens"] = assistant.MaxTokens;

        // DeepSeek V4 模型默认开启思考模式，对话场景通常不需要思考，显式关闭
        if (IsDeepSeekProvider(provider))
            bodyDict["thinking"] = new { type = "disabled" };

        var json = JsonSerializer.Serialize(bodyDict);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // 设置请求头
        _httpClient.DefaultRequestHeaders.Clear();
        if (provider.Name == "智谱AI")
        {
            var authToken = GenerateZhipuToken(provider.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {provider.ApiKey}");
        }

        _logger.LogInformation("ChatAI 请求: {Provider}@{Model}, 消息数: {Count}", provider.Name, modelId, messages.Count);

        // 发送请求
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, provider.ApiUrl)
        {
            Content = content
        }, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(GetUserFriendlyErrorMessage(response.StatusCode, errorBody));
        }

        // 读取 SSE 流
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break; // 流结束
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..].Trim();
            if (data == "[DONE]") break;

            string? chunk = null;
            try
            {
                var jsonDoc = JsonDocument.Parse(data);
                if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var choice = choices[0];
                    // 标准 SSE 格式使用 delta.content
                    if (choice.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var deltaContent))
                        chunk = deltaContent.GetString();
                    // 某些提供商可能使用 message.content（非标准）
                    else if (choice.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var msgContent))
                        chunk = msgContent.GetString();
                }
            }
            catch (JsonException) { }

            if (chunk != null)
                yield return chunk;
        }
    }

    /// <summary>
    /// 判断是否为 DeepSeek 厂商（通过 API URL 检测）
    /// </summary>
    private static bool IsDeepSeekProvider(AIProviderConfig config)
    {
        return config.ApiUrl.Contains("deepseek", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 为智谱 AI 生成 JWT token
    /// </summary>
    private string GenerateZhipuToken(string apiKey)
    {
        try
        {
            var parts = apiKey.Split('.');
            if (parts.Length != 2)
                return apiKey;

            var id = parts[0];
            var secret = parts[1];
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var exp = timestamp + 3600;

            var payload = new { api_key = id, exp, timestamp };
            var header = new { alg = "HS256", sign_type = "SIGN" };

            var headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header));
            var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            var headerBase64 = Base64UrlEncode(headerBytes);
            var payloadBase64 = Base64UrlEncode(payloadBytes);

            var message = $"{headerBase64}.{payloadBase64}";
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var signature = hmac.ComputeHash(messageBytes);
            var signatureBase64 = Base64UrlEncode(signature);

            return $"{message}.{signatureBase64}";
        }
        catch { return apiKey; }
    }

    /// <summary>
    /// Base64 URL 编码
    /// </summary>
    private static string Base64UrlEncode(byte[] input)
        => Convert.ToBase64String(input).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    /// <summary>
    /// 根据 HTTP 状态码和响应体生成用户友好的错误信息
    /// </summary>
    private static string GetUserFriendlyErrorMessage(System.Net.HttpStatusCode statusCode, string responseBody)
    {
        string? extractedMessage = null;
        try
        {
            var jsonDoc = JsonDocument.Parse(responseBody);
            if (jsonDoc.RootElement.TryGetProperty("error", out var error) && error.TryGetProperty("message", out var msg))
                extractedMessage = msg.GetString();
        }
        catch { }

        return statusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "API Key 无效，请在设置中检查 API Key 是否正确配置",
            System.Net.HttpStatusCode.Forbidden => "无权访问该 API，请检查 API Key 和 API URL 是否正确",
            System.Net.HttpStatusCode.NotFound => "API 地址或模型不存在，请检查配置",
            System.Net.HttpStatusCode.TooManyRequests => "请求过于频繁，请稍后再试",
            System.Net.HttpStatusCode.BadGateway => "API 网关错误，服务器暂时不可用",
            System.Net.HttpStatusCode.ServiceUnavailable => "API 服务暂时不可用，请稍后重试",
            System.Net.HttpStatusCode.GatewayTimeout => "API 响应超时，请稍后重试",
            System.Net.HttpStatusCode.RequestTimeout => "请求超时，请检查网络连接后重试",
            _ => !string.IsNullOrEmpty(extractedMessage)
                ? $"API 调用失败（{statusCode}）：{extractedMessage}"
                : $"API 调用失败（{statusCode}），请检查网络配置"
        };
    }
}
