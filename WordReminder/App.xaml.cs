using System.Windows;

namespace WordReminder;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 创建主窗口
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}
