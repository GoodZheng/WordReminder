using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace WordReminder.Controls;

public class WindowBase : Window
{
    public static readonly DependencyProperty TitleTextProperty =
        DependencyProperty.Register(nameof(TitleText), typeof(string), typeof(WindowBase),
            new PropertyMetadata(string.Empty));

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public static readonly DependencyProperty CanResizeProperty =
        DependencyProperty.Register(nameof(CanResize), typeof(bool), typeof(WindowBase),
            new PropertyMetadata(false, OnCanResizeChanged));

    public bool CanResize
    {
        get => (bool)GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    private static void OnCanResizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WindowBase window)
            window.ResizeMode = window.CanResize ? ResizeMode.CanResize : ResizeMode.NoResize;
    }

    public WindowBase()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowBase),
            new FrameworkPropertyMetadata(typeof(WindowBase)));
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (GetTemplateChild("PART_TitleBar") is FrameworkElement titleBar)
        {
            titleBar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            };
        }

        if (GetTemplateChild("PART_CloseButton") is FrameworkElement closeButton)
        {
            closeButton.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                Close();
            };
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        if (CanResize)
        {
            var handle = new WindowInteropHelper(this).Handle;
            var hwndSource = HwndSource.FromHwnd(handle);
            hwndSource?.AddHook(WndProc);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_NCHITTEST = 0x0084;
        const int HTLEFT = 10;
        const int HTRIGHT = 11;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOM = 15;
        const int HTBOTTOMLEFT = 16;
        const int HTBOTTOMRIGHT = 17;

        if (msg == WM_NCHITTEST && CanResize)
        {
            int x = lParam.ToInt32() & 0xFFFF;
            int y = lParam.ToInt32() >> 16;
            var point = PointFromScreen(new System.Windows.Point(x, y));

            double resizeBorder = 6;
            double width = ActualWidth;
            double height = ActualHeight;

            bool onLeft = point.X <= resizeBorder;
            bool onRight = point.X >= width - resizeBorder;
            bool onTop = point.Y <= resizeBorder;
            bool onBottom = point.Y >= height - resizeBorder;

            if (IsTopLeft(onLeft, onTop)) { handled = true; return (IntPtr)HTTOPLEFT; }
            if (IsTopRight(onRight, onTop)) { handled = true; return (IntPtr)HTTOPRIGHT; }
            if (IsBottomLeft(onLeft, onBottom)) { handled = true; return (IntPtr)HTBOTTOMLEFT; }
            if (IsBottomRight(onRight, onBottom)) { handled = true; return (IntPtr)HTBOTTOMRIGHT; }
            if (onLeft) { handled = true; return (IntPtr)HTLEFT; }
            if (onRight) { handled = true; return (IntPtr)HTRIGHT; }
            if (onTop) { handled = true; return (IntPtr)HTTOP; }
            if (onBottom) { handled = true; return (IntPtr)HTBOTTOM; }
        }

        return IntPtr.Zero;
    }

    private static bool IsTopLeft(bool left, bool top) => left && top;
    private static bool IsTopRight(bool right, bool top) => right && top;
    private static bool IsBottomLeft(bool left, bool bottom) => left && bottom;
    private static bool IsBottomRight(bool right, bool bottom) => right && bottom;
}
