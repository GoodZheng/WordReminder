using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WordReminder.ViewModels;
using WordReminder.Views;
using WordReminder.Services;

namespace WordReminder;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化依赖注入容器
        _host = Bootstrapper.ConfigureService();

        // 从 DI 容器获取 MainViewModel 和 HotKeyService
        var viewModel = _host.Services.GetRequiredService<MainViewModel>();
        var hotKeyService = _host.Services.GetRequiredService<HotKeyService>();

        // 创建主窗口并设置 DataContext
        var mainWindow = new MainWindow
        {
            DataContext = viewModel
        };

        // 在窗口初始化后注册快捷键
        mainWindow.SourceInitialized += (_, _) =>
        {
            var helper = new WindowInteropHelper(mainWindow);
            hotKeyService.Initialize(helper.Handle);

            // 从配置加载并注册快捷键
            var configService = _host.Services.GetRequiredService<ConfigService>();
            if (configService.Settings.HotKeys != null)
            {
                hotKeyService.RegisterAllHotKeys(configService.Settings.HotKeys);
            }
        };

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}
