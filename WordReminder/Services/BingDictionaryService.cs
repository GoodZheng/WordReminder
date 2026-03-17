using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using WordReminder.Models;

namespace WordReminder.Services;

public class BingDictionaryService
{
    private readonly HttpClient _httpClient;

    public BingDictionaryService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<Word?> GetWordInfoAsync(string wordText)
    {
        try
        {
            // 1. 访问必应词典
            var url = $"https://cn.bing.com/dict/search?q={HttpUtility.UrlEncode(wordText)}";

            // 2. 下载网页内容
            var html = await _httpClient.GetStringAsync(url);

            // 调试：保存 HTML 到文件
            var debugPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug_bing.html");
            await File.WriteAllTextAsync(debugPath, html);

            var word = new Word
            {
                Text = wordText
            };

            // 直接从 HTML 主体解析所有信息
            ParseFromHtmlBody(html, word);

            // 如果音标未获取到，尝试从 meta description 获取
            if (string.IsNullOrEmpty(word.Phonetic))
            {
                var metaMatch = Regex.Match(html, @"<meta\s+name=""description""\s+content=""([^""]*)""", RegexOptions.IgnoreCase);
                if (metaMatch.Success)
                {
                    var metaContent = metaMatch.Groups[1].Value;
                    var phoneticMatch = Regex.Match(metaContent, @"\[([^\]]+)\]");
                    if (phoneticMatch.Success)
                    {
                        word.Phonetic = $"[{phoneticMatch.Groups[1].Value}]";
                    }
                }
            }

            // 确保获取例句
            if (string.IsNullOrEmpty(word.Example))
            {
                word.Example = ExtractExample(html);
            }

            return word;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching {wordText}: {ex.Message}");
            return new Word { Text = wordText };
        }
    }

    private void ParseFromHtmlBody(string html, Word word)
    {
        // 提取音标 - 美音
        var usPhonetic = ExtractPattern(html, @"美\s*\[([^\]]+)\]");
        if (string.IsNullOrEmpty(usPhonetic))
        {
            usPhonetic = ExtractPattern(html, @"class=""hd_prUS[^""]*""[^>]*>([^<\]]+)");
        }
        if (!string.IsNullOrEmpty(usPhonetic))
        {
            word.Phonetic = $"[{usPhonetic}]";
        }

        // 提取词性 - 排除"网络"这种特殊词性
        var posMatches = Regex.Matches(html, @"<span[^>]*class=""pos(?:\s+[^""]*|)"")[^>]*>([^<]+)</span>", RegexOptions.IgnoreCase);
        foreach (Match match in posMatches)
        {
            var pos = match.Groups[1].Value.Trim();
            if (pos != "网络" && !pos.Contains("web"))
            {
                word.PartOfSpeech = pos;
                break;
            }
        }

        // 提取释义 - 先匹配整个def span内容，然后清理HTML
        var defMatch = Regex.Match(html, @"<span[^>]*class=""def[^""]*""[^>]*>(.*?)</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (defMatch.Success)
        {
            var defContent = defMatch.Groups[1].Value;
            // 清理HTML标签和实体
            defContent = Regex.Replace(defContent, "<[^>]+>", "");
            defContent = defContent.Replace("&quot;", "\"");
            defContent = defContent.Replace("&amp;", "&");
            defContent = defContent.Replace("&lt;", "<");
            defContent = defContent.Replace("&gt;", ">");
            defContent = defContent.Replace("&nbsp;", " ");
            defContent = Regex.Replace(defContent, @"\s+", " ").Trim();
            word.Definition = defContent;
        }

        // 调试输出
        Console.WriteLine($"[DEBUG] Word: {word.Text}");
        Console.WriteLine($"[DEBUG] Phonetic: {word.Phonetic}");
        Console.WriteLine($"[DEBUG] PartOfSpeech: {word.PartOfSpeech}");
        Console.WriteLine($"[DEBUG] Definition: {word.Definition}");
        Console.WriteLine($"[DEBUG] defMatch.Success: {defMatch.Success}");

        // 提取例句
        word.Example = ExtractExample(html);
    }

    private string? ExtractExample(string html)
    {
        // 尝试多种模式提取例句
        string[] patterns = new string[]
        {
            // 模式1: class="sen_en" - 例句英文部分
            @"<div[^>]*class=""sen_en[^""]*""[^>]*>(.*?)</div>",
            // 模式2: class="sen_cn" - 例句中文部分（作为备选）
            @"<div[^>]*class=""sen_cn[^""]*""[^>]*>(.*?)</div>",
            // 模式3: class包含 sen 的通用模式
            @"<div[^>]*class=""[^""]*sen[^""]*""[^>]*>([A-Z][^<]{15,200})</div>",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success && match.Groups.Count > 1)
            {
                var example = match.Groups[1].Value.Trim();
                // 清理HTML标签和实体
                example = Regex.Replace(example, "<[^>]+>", " ");
                example = example.Replace("&quot;", "\"");
                example = example.Replace("&amp;", "&");
                example = example.Replace("&lt;", "<");
                example = example.Replace("&gt;", ">");
                example = example.Replace("&nbsp;", " ");
                example = Regex.Replace(example, @"\s+", " ").Trim();

                // 验证例句：至少包含一个空格（英文句子通常有多个单词）
                if (example.Length > 10 && example.Contains(" "))
                {
                    return example;
                }
            }
        }

        return null;
    }

    private string? ExtractPattern(string html, string pattern)
    {
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            var result = match.Groups[1].Value.Trim();
            result = result.Replace("&quot;", "\"");
            result = result.Replace("&amp;", "&");
            result = result.Replace("&lt;", "<");
            result = result.Replace("&gt;", ">");
            return string.IsNullOrEmpty(result) ? null : result;
        }
        return null;
    }
}
