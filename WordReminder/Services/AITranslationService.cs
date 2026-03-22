using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WordReminder.Models;

namespace WordReminder.Services;

/// <summary>
/// 万能翻译服务 - 支持中英文互翻、单词和句子翻译
/// </summary>
public class AITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigService _configService;

    public AITranslationService(ConfigService configService)
    {
        _configService = configService;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 翻译请求结果
    /// </summary>
    public class TranslationResult
    {
        public string? Text { get; set; }           // 原文
        public string? TranslatedText { get; set; } // 译文
        public string? Type { get; set; }           // 类型：word/sentence
        public string? Direction { get; set; }      // 方向：en2zh/zh2en
        public List<WordInfo>? WordDetails { get; set; }  // 单词详情
        public List<TranslationOption>? Options { get; set; }  // 多种翻译选项
    }

    /// <summary>
    /// 单词详情
    /// </summary>
    public class WordInfo
    {
        public string? Word { get; set; }
        public string? Phonetic { get; set; }
        public string? PartOfSpeech { get; set; }
        public string? Definition { get; set; }
        public string? Example { get; set; }
        public string? ExampleTranslation { get; set; }
    }

    /// <summary>
    /// 翻译选项
    /// </summary>
    public class TranslationOption
    {
        public string? Text { get; set; }
        public string? Scenario { get; set; }
    }

    /// <summary>
    /// 执行翻译
    /// </summary>
    public async Task<TranslationResult?> TranslateAsync(string text)
    {
        var config = _configService.Settings.AIDictionary;

        // 检查是否启用 AI
        if (!config.Enabled || string.IsNullOrEmpty(config.ApiKey) || config.ApiKey == "your-api-key-here")
        {
            return new TranslationResult
            {
                Text = text,
                TranslatedText = "AI 词典未配置，请在设置中配置 API Key",
                Type = "error"
            };
        }

        try
        {
            // 判断文本类型和方向
            var (type, direction) = DetectTextType(text);

            // 构建提示词
            string systemPrompt = BuildSystemPrompt(type, direction);
            string userPrompt = BuildUserPrompt(text, type, direction);

            // 构建请求
            var requestBody = new
            {
                model = config.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new
                    {
                        role = "user",
                        content = userPrompt
                    }
                },
                temperature = 0.5,
                max_tokens = 2000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // 设置请求头
            _httpClient.DefaultRequestHeaders.Clear();

            // 根据提供商使用不同的认证方式
            string authToken = config.ApiKey;
            if (config.Provider == "zhipuai")
            {
                authToken = GenerateZhipuToken(config.ApiKey);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
            }

            Console.WriteLine($"[翻译] 正在调用 AI API，类型: {type}, 方向: {direction}");

            // 发送请求
            var response = await _httpClient.PostAsync(config.ApiUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[翻译] API 调用失败: {response.StatusCode}");
                return new TranslationResult
                {
                    Text = text,
                    TranslatedText = $"API 调用失败: {response.StatusCode}",
                    Type = "error"
                };
            }

            // 解析响应
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);
            string aiContent = ExtractAIContent(jsonResponse);

            if (string.IsNullOrEmpty(aiContent))
            {
                return new TranslationResult
                {
                    Text = text,
                    TranslatedText = "未能解析 AI 响应",
                    Type = "error"
                };
            }

            Console.WriteLine($"[翻译] AI 响应: {aiContent}");

            // 解析翻译结果
            return ParseTranslationResult(aiContent, text, type, direction);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[翻译] 错误: {ex.Message}");
            return new TranslationResult
            {
                Text = text,
                TranslatedText = $"翻译出错: {ex.Message}",
                Type = "error"
            };
        }
    }

    /// <summary>
    /// 检测文本类型和翻译方向
    /// </summary>
    private (string type, string direction) DetectTextType(string text)
    {
        var trimmedText = text.Trim();

        // 检测是否包含中文字符
        bool hasChinese = Regex.IsMatch(trimmedText, @"[\u4e00-\u9fa5]");
        // 检测是否包含英文字母
        bool hasEnglish = Regex.IsMatch(trimmedText, @"[a-zA-Z]");

        // 判断是否是单词（英文，不含空格或只有空格分隔的多个词）
        bool isEnglishWord = !hasChinese && !trimmedText.Contains(" ") ||
                             (!hasChinese && trimmedText.Split(' ').Length <= 2);

        if (hasChinese && !hasEnglish)
        {
            // 纯中文 -> 英文
            return ("sentence", "zh2en");
        }
        else if (!hasChinese && hasEnglish)
        {
            // 英文 -> 中文
            if (isEnglishWord)
            {
                return ("word", "en2zh");
            }
            else
            {
                return ("sentence", "en2zh");
            }
        }
        else
        {
            // 混合文本，默认视为英文句子翻译为中文
            return ("sentence", "en2zh");
        }
    }

    /// <summary>
    /// 构建系统提示词
    /// </summary>
    private string BuildSystemPrompt(string type, string direction)
    {
        return direction switch
        {
            "en2zh" when type == "word" => @"你是专业的英汉词典助手。请严格按照 JSON 格式返回单词信息。

返回格式：
{
  ""translatedText"": ""中文释义"",
  ""wordDetails"": [
    {
      ""word"": ""单词"",
      ""phonetic"": ""音标"",
      ""partOfSpeech"": ""词性（使用英文缩写：n./v./adj./adv./prep.等）"",
      ""definition"": ""中文释义"",
      ""example"": ""英文例句"",
      ""exampleTranslation"": ""例句的中文翻译""
    }
  ]
}

只返回 JSON 对象，不要包含其他说明文字。",

            "en2zh" => @"你是专业的英汉翻译助手。请严格按照 JSON 格式返回翻译结果。

返回格式：
{
  ""translatedText"": ""中文译文"",
  ""wordDetails"": [
    {
      ""word"": ""难词"",
      ""phonetic"": ""音标"",
      ""partOfSpeech"": ""词性（使用英文缩写：n./v./adj./adv./prep.等）"",
      ""definition"": ""中文释义"",
      ""example"": ""英文例句"",
      ""exampleTranslation"": ""例句的中文翻译""
    }
  ]
}

要求：
1. 准确翻译句子的含义
2. 从句子中提取 3-5 个较难或不常见的单词
3. 为每个难词提供音标、词性、释义、例句及例句的中文翻译

只返回 JSON 对象，不要包含其他说明文字。",

            "zh2en" => @"你是专业的汉英翻译助手。请严格按照 JSON 格式返回翻译结果。

返回格式：
{
  ""options"": [
    {
      ""text"": ""英文译文"",
      ""scenario"": ""适用场景说明""
    }
  ]
}

要求：
1. 提供 3-5 种不同的英文翻译方式
2. 必须包含口语化、日常生活中常用的表达方式
3. 每种翻译要说明其适用的场景，如：日常口语、朋友间对话、非正式场合、正式书面、商务场合等
4. 优先推荐最常用、最自然的表达方式
5. 翻译要准确传达原句的含义

只返回 JSON 对象，不要包含其他说明文字。",

            _ => "你是专业的翻译助手。"
        };
    }

    /// <summary>
    /// 构建用户提示词
    /// </summary>
    private string BuildUserPrompt(string text, string type, string direction)
    {
        return direction switch
        {
            "en2zh" when type == "word" => $"请翻译单词 \"{text}\"，提供其中文释义、音标、词性和例句。",
            "en2zh" => $"请将以下英文句子翻译为中文，并总结其中的难词：\n\n{text}",
            "zh2en" => $"请将以下中文句子翻译为英文，提供多种翻译方式：\n\n{text}",
            _ => $"请翻译：{text}"
        };
    }

    /// <summary>
    /// 解析翻译结果
    /// </summary>
    private TranslationResult ParseTranslationResult(string aiContent, string originalText, string type, string direction)
    {
        try
        {
            // 尝试提取 JSON
            var jsonMatch = Regex.Match(aiContent, @"```(?:json)?\s*(\{.*?\})\s*```", RegexOptions.Singleline);
            string jsonStr = jsonMatch.Success ? jsonMatch.Groups[1].Value : aiContent;

            var jsonDoc = JsonDocument.Parse(jsonStr);
            var root = jsonDoc.RootElement;

            var result = new TranslationResult
            {
                Text = originalText,
                Type = type,
                Direction = direction
            };

            // 解析译文
            if (root.TryGetProperty("translatedText", out var translatedText))
            {
                result.TranslatedText = translatedText.GetString();
            }

            // 解析单词详情
            if (root.TryGetProperty("wordDetails", out var wordDetails) && wordDetails.ValueKind == JsonValueKind.Array)
            {
                result.WordDetails = new List<WordInfo>();
                foreach (var item in wordDetails.EnumerateArray())
                {
                    result.WordDetails.Add(new WordInfo
                    {
                        Word = GetStringValue(item, "word"),
                        Phonetic = GetStringValue(item, "phonetic"),
                        PartOfSpeech = GetStringValue(item, "partOfSpeech"),
                        Definition = GetStringValue(item, "definition"),
                        Example = GetStringValue(item, "example"),
                        ExampleTranslation = GetStringValue(item, "exampleTranslation")
                    });
                }
            }

            // 解析翻译选项
            if (root.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
            {
                result.Options = new List<TranslationOption>();
                foreach (var item in options.EnumerateArray())
                {
                    result.Options.Add(new TranslationOption
                    {
                        Text = GetStringValue(item, "text"),
                        Scenario = GetStringValue(item, "scenario")
                    });
                }
            }

            // 如果没有翻译结果，使用整个响应作为翻译
            if (string.IsNullOrEmpty(result.TranslatedText) && result.Options == null)
            {
                result.TranslatedText = aiContent.Trim();
            }

            return result;
        }
        catch (JsonException)
        {
            // JSON 解析失败，直接返回原始内容
            return new TranslationResult
            {
                Text = originalText,
                TranslatedText = aiContent.Trim(),
                Type = type,
                Direction = direction
            };
        }
    }

    private string? GetStringValue(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetString();
        }
        return null;
    }

    private string ExtractAIContent(JsonElement jsonResponse)
    {
        // OpenAI / 智谱 AI 兼容格式
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

        // 直接返回 content
        if (jsonResponse.TryGetProperty("content", out var directContent))
        {
            return directContent.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// 为智谱 AI 生成 JWT token（复用自 AIDictionaryService）
    /// </summary>
    private string GenerateZhipuToken(string apiKey)
    {
        try
        {
            var parts = apiKey.Split('.');
            if (parts.Length != 2)
            {
                return apiKey;
            }

            var id = parts[0];
            var secret = parts[1];

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var exp = timestamp + 3600;

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

            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var signature = hmac.ComputeHash(messageBytes);
            var signatureBase64 = Base64UrlEncode(signature);

            return $"{message}.{signatureBase64}";
        }
        catch
        {
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
}
