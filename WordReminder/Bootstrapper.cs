using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using NLog.Extensions.Logging;
using WordReminder.Services;
using WordReminder.ViewModels;

namespace WordReminder;

/// <summary>
/// 应用程序启动配置，配置依赖注入和服务
/// </summary>
public static class Bootstrapper
{
    /// <summary>
    /// 配置服务和依赖注入
    /// </summary>
    public static IHost ConfigureService()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                try
                {
                    logging.AddNLog();
                }
                catch (Exception ex)
                {
                    // NLog 初始化失败不应阻止程序启动
                    System.Diagnostics.Debug.WriteLine($"NLog 初始化失败: {ex.Message}");
                }
            })
            .ConfigureServices((context, services) =>
            {
                // 注册 IMessenger（弱引用消息传递）
                services.AddSingleton<IMessenger>(sp => WeakReferenceMessenger.Default);

                // 注册服务（单例模式）
                services.AddSingleton<ConfigService>();
                services.AddSingleton<DatabaseService>();
                services.AddSingleton<AIDictionaryService>();
                services.AddSingleton<BingDictionaryService>();
                services.AddSingleton(sp => new UpdateService("GoodZheng", "WordReminder"));
                services.AddSingleton<AITranslationService>();
                services.AddSingleton<TranslationHistoryService>();
                services.AddSingleton<AIConnectivityTestService>();
                services.AddSingleton<HotKeyService>();
                services.AddSingleton<AssistantService>();
                services.AddSingleton<ChatService>();
                services.AddSingleton<ChatAIService>();

                // 注册窗口管理服务
                services.AddSingleton<WindowManagerService>();

                // 注册 ViewModel（瞬态模式，每次创建新实例）
                services.AddTransient<MainViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AllWordsViewModel>();
                services.AddTransient<AddWordViewModel>();
                services.AddTransient<TranslationViewModel>();
                services.AddTransient<ColorPickerViewModel>();
                // Task 6-8: Assistant List & Edit ViewModels
                services.AddTransient<AssistantListViewModel>();
                services.AddTransient<AssistantEditViewModel>();
                // Task 9: Chat ViewModel
                services.AddTransient<ChatViewModel>();
            })
            .Build();

        return host;
    }
}
