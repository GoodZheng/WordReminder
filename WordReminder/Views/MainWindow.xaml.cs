using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.Messaging;
using WordReminder.Messages;
using WordReminder.ViewModels;

namespace WordReminder.Views;

/// <summary>
/// MainWindow Code-behind - 仅处理 UI 交互
/// </summary>
public partial class MainWindow : Window
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTCLIENT = 1;
    private HwndSource? _hwndSource;
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        // 禁用 Windows 11 的 Snap Layouts
        this.SourceInitialized += (s, e) =>
        {
            _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
            }
        };

        // 注册消息监听
        WeakReferenceMessenger.Default.Register<ExitApplicationMessage>(this, (_, _) => Close());

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel = DataContext as MainViewModel;
        if (_viewModel != null)
        {
            // 绑定窗口事件到 ViewModel 命令
            SizeChanged += (_, _) => _viewModel.WindowSizeChangedCommand.Execute(null);
            LocationChanged += (_, _) => _viewModel.WindowLocationChangedCommand.Execute(null);
            Closing += (_, _) => _viewModel.WindowClosingCommand.Execute(null);
        }
    }

    // 窗口过程处理，禁用 Snap Layouts
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            handled = true;
            return new IntPtr(HTCLIENT);
        }
        return IntPtr.Zero;
    }

    // 窗口拖动（双击打开设置）
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ClickCount == 2)
        {
            // 双击打开设置
            if (_viewModel != null)
            {
                _viewModel.OpenSettingsCommand.Execute(null);
            }
        }
        else
        {
            DragMove();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }
        base.OnClosing(e);
    }
}
