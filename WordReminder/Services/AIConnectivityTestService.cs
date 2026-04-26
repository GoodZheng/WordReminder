using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WordReminder.Models;

namespace WordReminder.Services;

public class AIConnectivityTestService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIConnectivityTestService> _logger;

    public AIConnectivityTestService(ILogger<AIConnectivityTestService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public class TestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public long ElapsedMs { get; set; }
    }

    public async Task<TestResult> TestConnectionAsync(AIProviderConfig provider, string modelId)
    {
        if (string.IsNullOrEmpty(provider.ApiKey))
        {
            return new TestResult { Success = false, Message = "API Key 未填写" };
        }

        if (string.IsNullOrEmpty(provider.ApiUrl))
        {
            return new TestResult { Success = false, Message = "API URL 未填写" };
        }

        if (string.IsNullOrEmpty(modelId))
        {
            return new TestResult { Success = false, Message = "模型名称未填写" };
        }

        var sw = Stopwatch.StartNew();

        try
        {
            var requestBody = new
            {
                model = modelId,
                messages = new[]
                {
                    new { role = "user", content = "Hi" }
                },
                max_tokens = 5
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();

            if (provider.Name == "智谱AI")
            {
                var token = GenerateZhipuToken(provider.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {provider.ApiKey}");
            }

            var response = await _httpClient.PostAsync(provider.ApiUrl, content);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("连通性测试成功: {Provider}/{Model}, 耗时 {Ms}ms", provider.Name, modelId, sw.ElapsedMilliseconds);
                return new TestResult
                {
                    Success = true,
                    Message = $"连通成功，耗时 {sw.ElapsedMilliseconds}ms",
                    ElapsedMs = sw.ElapsedMilliseconds
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("连通性测试失败: {StatusCode} - {Body}", response.StatusCode, responseBody);

            var errorMsg = response.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => "认证失败，请检查 API Key",
                System.Net.HttpStatusCode.Forbidden => "无权限访问，请检查 API Key 和权限",
                System.Net.HttpStatusCode.NotFound => "API 地址无效，请检查 URL",
                System.Net.HttpStatusCode.TooManyRequests => "请求过于频繁，请稍后重试",
                _ => $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
            };

            return new TestResult { Success = false, Message = errorMsg, ElapsedMs = sw.ElapsedMilliseconds };
        }
        catch (TaskCanceledException)
        {
            sw.Stop();
            return new TestResult { Success = false, Message = $"请求超时（{sw.ElapsedMilliseconds}ms）", ElapsedMs = sw.ElapsedMilliseconds };
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogError(ex, "连通性测试网络错误");
            return new TestResult { Success = false, Message = $"网络错误: {ex.Message}", ElapsedMs = sw.ElapsedMilliseconds };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "连通性测试异常");
            return new TestResult { Success = false, Message = $"测试失败: {ex.Message}", ElapsedMs = sw.ElapsedMilliseconds };
        }
    }

    private static string GenerateZhipuToken(string apiKey)
    {
        var parts = apiKey.Split('.');
        if (parts.Length != 2)
            return apiKey;

        var id = parts[0];
        var secret = parts[1];
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var payload = new { api_key = id, exp = timestamp + 3600, timestamp };
        var header = new { alg = "HS256", sign_type = "SIGN" };

        var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
        var payloadBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var message = $"{headerBase64}.{payloadBase64}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));

        return $"{message}.{signature}";
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
