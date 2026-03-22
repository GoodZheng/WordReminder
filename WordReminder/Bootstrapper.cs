using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommunityToolkit.Mvvm.Messaging;
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
                services.AddSingleton<HotKeyService>();

                // 注册窗口管理服务
                services.AddSingleton<WindowManagerService>();

                // 注册 ViewModel（瞬态模式，每次创建新实例）
                services.AddTransient<MainViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<AllWordsViewModel>();
                services.AddTransient<AddWordViewModel>();
                services.AddTransient<TranslationViewModel>();
                services.AddTransient<ColorPickerViewModel>();
            })
            .Build();

        return host;
    }
}
