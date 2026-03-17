using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.IO;

namespace WordReminder.Services;

/// <summary>
/// 更新服务，用于检查和下载应用更新
/// </summary>
public class UpdateService
{
    private const string GITHUB_API_RELEASES = "https://api.github.com/repos/{0}/{1}/releases/latest";
    private readonly string _owner;
    private readonly string _repo;
    private readonly HttpClient _httpClient;

    public UpdateService(string owner, string repo)
    {
        _owner = owner;
        _repo = repo;
        _httpClient = new HttpClient
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", "WordReminder" }
            }
        };
    }

    /// <summary>
    /// 获取当前应用版本
    /// </summary>
    public static Version GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyVersion = assembly.GetName().Version;
        return assemblyVersion ?? new Version("1.0.0");
    }

    /// <summary>
    /// 获取当前版本字符串
    /// </summary>
    public static string GetCurrentVersionString()
    {
        return GetCurrentVersion().ToString(3);
    }

    /// <summary>
    /// 检查是否有可用更新
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var url = string.Format(GITHUB_API_RELEASES, _owner, _repo);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(json);

            if (jsonNode == null)
                return null;

            var tagName = jsonNode["tag_name"]?.ToString();
            if (string.IsNullOrEmpty(tagName))
                return null;

            // 移除 'v' 前缀（如果有）
            var versionString = tagName.StartsWith("v") ? tagName[1..] : tagName;

            if (!Version.TryParse(versionString, out var latestVersion))
                return null;

            var currentVersion = GetCurrentVersion();

            if (latestVersion > currentVersion)
            {
                return new UpdateInfo
                {
                    Version = latestVersion,
                    VersionString = versionString,
                    ReleaseNotes = jsonNode["body"]?.ToString() ?? "",
                    ReleaseUrl = jsonNode["html_url"]?.ToString() ?? "",
                    PublishedAt = jsonNode["published_at"]?.ToString() ?? ""
                };
            }
        }
        catch (Exception)
        {
            // 网络错误或解析失败
        }

        return null;
    }

    /// <summary>
    /// 下载更新文件
    /// </summary>
    public async Task<bool> DownloadUpdateAsync(string downloadUrl, string destinationPath, IProgress<double>? progress = null)
    {
        try
        {
            var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var buffer = new byte[8192];
            var bytesRead = 0L;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            int read;
            while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                bytesRead += read;

                if (totalBytes > 0 && progress != null)
                {
                    var percent = (double)bytesRead / totalBytes * 100;
                    progress.Report(percent);
                }
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 获取最新版本的下载 URL
    /// </summary>
    public async Task<string?> GetDownloadUrlAsync()
    {
        try
        {
            var url = string.Format(GITHUB_API_RELEASES, _owner, _repo);
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(json);

            if (jsonNode == null)
                return null;

            var assets = jsonNode["assets"]?.AsArray();
            if (assets == null || assets.Count == 0)
                return null;

            // 查找合适的安装包（.exe 或 .zip）
            foreach (var asset in assets)
            {
                var name = asset["name"]?.ToString()?.ToLower() ?? "";
                if (name.EndsWith(".exe") || name.EndsWith(".zip"))
                {
                    return asset["browser_download_url"]?.ToString();
                }
            }
        }
        catch (Exception)
        {
            // 网络错误或解析失败
        }

        return null;
    }

    /// <summary>
    /// 启动安装程序
    /// </summary>
    public static void StartInstaller(string installerPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = installerPath,
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch (Exception)
        {
            // 启动失败
        }
    }

    /// <summary>
    /// 打开 releases 页面
    /// </summary>
    public static void OpenReleasesPage(string owner, string repo)
    {
        try
        {
            var url = $"https://github.com/{owner}/{repo}/releases/latest";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            // 打开浏览器失败
        }
    }
}

/// <summary>
/// 更新信息
/// </summary>
public class UpdateInfo
{
    public Version Version { get; set; } = new();
    public string VersionString { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string ReleaseUrl { get; set; } = "";
    public string PublishedAt { get; set; } = "";
}
