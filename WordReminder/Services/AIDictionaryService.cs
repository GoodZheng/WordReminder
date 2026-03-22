using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using WordReminder.Models;

namespace WordReminder.Services;

public class AIDictionaryService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigService _configService;

    public AIDictionaryService(ConfigService configService)
    {
        _configService = configService;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(90);
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
            {
                Console.WriteLine($"[AI] 智谱 API Key 格式不正确");
                return apiKey;
            }

            var id = parts[0];
            var secret = parts[1];

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var exp = timestamp + 3600; // 1小时后过期

            var payload = new
            {
                api_key = id,
                exp = exp,
                timestamp = timestamp
            };

            var header = new
            {
                alg = "HS256",
                sign_type = "SIGN"
            };

            var headerBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header));
            var payloadBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

            var headerBase64 = Base64UrlEncode(headerBytes);
            var payloadBase64 = Base64UrlEncode(payloadBytes);

            var message = $"{headerBase64}.{payloadBase64}";
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var signature = hmac.ComputeHash(messageBytes);
            var signatureBase64 = Base64UrlEncode(signature);

            return $"{message}.{signatureBase64}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI] 生成智谱 token 失败: {ex.Message}");
            return apiKey;
        }
    }

    private string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public async Task<Word?> GetWordInfoAsync(string wordText)
    {
        var config = _configService.Settings.AIDictionary;

        // 检查是否启用 AI 词典
        if (!config.Enabled || string.IsNullOrEmpty(config.ApiKey) || config.ApiKey == "your-api-key-here")
        {
            Console.WriteLine($"[AI] AI 词典未配置或已禁用，返回基础单词信息");
            return new Word { Text = wordText };
        }

        try
        {
            // 构建请求
            var requestBody = new
            {
                model = config.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = config.SystemPrompt + @"

请严格按照以下 JSON 格式返回单词信息：
{
  ""phonetic"": ""音标（如：[ˈdeɪmən]）"",
  ""partOfSpeech"": ""词性（如：n.、v.、adj. 等）"",
  ""definition"": ""中文释义"",
  ""example"": ""英文例句（必须包含该单词）""
}

只返回 JSON 对象，不要包含其他说明文字。"
                    },
                    new
                    {
                        role = "user",
                        content = $"请查询单词 \"{wordText}\" 的音标、词性、中文释义和一个英文例句。"
                    }
                },
                temperature = 0.3,
                max_tokens = 500,
                top_p = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 设置请求头
            _httpClient.DefaultRequestHeaders.Clear();

            // 根据提供商使用不同的认证方式
            string authToken = config.ApiKey;
            if (config.Provider == "zhipuai")
            {
                // 智谱 AI 需要生成 JWT token
                authToken = GenerateZhipuToken(config.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            }
            else
            {
                // 其他提供商使用标准 Bearer token
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            }

            Console.WriteLine($"[AI] 正在调用 {config.Provider} API: {config.ApiUrl}");
            Console.WriteLine($"[AI] 使用模型: {config.Model}");

            // 发送请求
            var response = await _httpClient.PostAsync(config.ApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[AI] API 调用失败: {response.StatusCode} - {responseBody}");
                return new Word { Text = wordText };
            }

            // 解析响应
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            // 提取 AI 返回的内容（支持多种响应格式）
            string aiContent = ExtractAIContent(jsonResponse, config.Provider);

            if (string.IsNullOrEmpty(aiContent))
            {
                Console.WriteLine($"[AI] 未能从响应中提取内容");
                return new Word { Text = wordText };
            }

            Console.WriteLine($"[AI] 原始响应: {aiContent}");

            // 解析 AI 返回的 JSON
            var wordInfo = ParseAIResponse(aiContent);

            return new Word
            {
                Text = wordText,
                Phonetic = wordInfo.GetValueOrDefault("phonetic"),
                PartOfSpeech = wordInfo.GetValueOrDefault("partOfSpeech"),
                Definition = wordInfo.GetValueOrDefault("definition"),
                Example = wordInfo.GetValueOrDefault("example")
            };
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[AI] 网络请求失败: {ex.Message}");
            return new Word { Text = wordText };
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"[AI] 请求超时: {ex.Message}");
            return new Word { Text = wordText };
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[AI] JSON 解析失败: {ex.Message}");
            return new Word { Text = wordText };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI] 未知错误: {ex.Message}");
            return new Word { Text = wordText };
        }
    }

    private string ExtractAIContent(JsonElement jsonResponse, string provider)
    {
        // OpenAI / 智谱 AI 兼容格式：choices[0].message.content
        if (jsonResponse.TryGetProperty("choices", out var choices) &&
            choices.GetArrayLength() > 0)
        {
            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }

        // 某些国产大模型直接返回 data
        if (jsonResponse.TryGetProperty("data", out var data))
        {
            if (data.GetArrayLength() > 0)
            {
                var firstData = data[0];
                if (firstData.TryGetProperty("content", out var content))
                {
                    return content.GetString() ?? string.Empty;
                }
            }
        }

        // 直接返回 content（某些简单格式）
        if (jsonResponse.TryGetProperty("content", out var directContent))
        {
            return directContent.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private Dictionary<string, string?> ParseAIResponse(string aiContent)
    {
        var result = new Dictionary<string, string?>();

        try
        {
            // 尝试直接解析 JSON
            var jsonDoc = JsonDocument.Parse(aiContent);
            var root = jsonDoc.RootElement;

            result["phonetic"] = GetStringValue(root, "phonetic");
            result["partOfSpeech"] = GetStringValue(root, "partOfSpeech") ?? GetStringValue(root, "pos");
            result["definition"] = GetStringValue(root, "definition") ?? GetStringValue(root, "def");
            result["example"] = GetStringValue(root, "example");

            return result;
        }
        catch (JsonException)
        {
            // 如果直接解析失败，尝试提取 JSON 代码块
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(aiContent, @"```(?:json)?\s*(\{.*?\})\s*```",
                System.Text.RegularExpressions.RegexOptions.Singleline);

            if (jsonMatch.Success)
            {
                try
                {
                    var jsonDoc = JsonDocument.Parse(jsonMatch.Groups[1].Value);
                    var root = jsonDoc.RootElement;

                    result["phonetic"] = GetStringValue(root, "phonetic");
                    result["partOfSpeech"] = GetStringValue(root, "partOfSpeech") ?? GetStringValue(root, "pos");
                    result["definition"] = GetStringValue(root, "definition") ?? GetStringValue(root, "def");
                    result["example"] = GetStringValue(root, "example");

                    return result;
                }
                catch
                {
                    // 解析失败，返回空字典
                }
            }

            // 尝试简单的正则提取
            result["phonetic"] = ExtractValue(aiContent, @"音标[：:]\s*([^\n]+)");
            result["partOfSpeech"] = ExtractValue(aiContent, @"词性[：:]\s*([^\n]+)");
            result["definition"] = ExtractValue(aiContent, @"释义[：:]\s*([^\n]+)");
            result["example"] = ExtractValue(aiContent, @"例句[：:]\s*([^\n]+)");
        }

        return result;
    }

    private string? GetStringValue(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetString();
        }
        return null;
    }

    private string? ExtractValue(string content, string pattern)
    {
        var match = System.Text.RegularExpressions.Regex.Match(content, pattern);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
