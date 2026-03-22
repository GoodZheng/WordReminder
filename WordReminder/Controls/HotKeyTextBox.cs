using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WordReminder.Models;

namespace WordReminder.Controls;

/// <summary>
/// 快捷键输入框 - 用户按下组合键后自动捕获
/// </summary>
public class HotKeyTextBox : System.Windows.Controls.TextBox
{
    public static readonly DependencyProperty HotKeyProperty =
        DependencyProperty.Register(
            nameof(HotKey),
            typeof(HotKey),
            typeof(HotKeyTextBox),
            new PropertyMetadata(null, OnHotKeyChanged));

    public HotKey? HotKey
    {
        get => (HotKey?)GetValue(HotKeyProperty);
        set => SetValue(HotKeyProperty, value);
    }

    private bool _isRecording;
    private Key? _finalKey;

    public HotKeyTextBox()
    {
        IsUndoEnabled = false;
        CaretIndex = 0;
        Text = "未设置";
        // 使用 PreviewKeyDown 事件，它在 KeyDown 之前触发
        PreviewKeyDown += OnPreviewKeyDown;
        PreviewKeyUp += OnPreviewKeyUp;
        // 阻止文本输入
        PreviewTextInput += OnPreviewTextInput;
        TextChanged += OnTextChanged;
        LostFocus += OnLostFocus;
        GotFocus += OnGotFocus;
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // 阻止所有文本输入
        e.Handled = true;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        // 如果在录制状态，强制显示提示文本
        if (_isRecording)
        {
            Text = "按下快捷键...";
            // 将光标移到末尾
            CaretIndex = Text.Length;
        }
        // 如果不在录制状态且有快捷键，确保显示正确的快捷键文本
        else if (HotKey != null && HotKey.Enabled)
        {
            string expectedText = HotKey.GetDisplayString();
            if (Text != expectedText)
            {
                Text = expectedText;
                CaretIndex = Text.Length;
            }
        }
    }

    private static void OnHotKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HotKeyTextBox textBox)
        {
            textBox.UpdateDisplay();
        }
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        _isRecording = true;
        _finalKey = null;
        Text = "按下快捷键...";
        SelectAll();
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_isRecording)
        {
            // 允许 Escape 键清除快捷键
            if (e.Key == Key.Escape)
            {
                HotKey = new HotKey { Enabled = false };
                UpdateDisplay();
                e.Handled = true;
            }
            return;
        }

        // 忽略重复按键
        if (e.IsRepeat)
        {
            return;
        }

        // 获取实际按键（处理 Alt 等系统键）
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        // 过滤掉无效的键（输入法键等）
        if (key == Key.ImeProcessed || key == Key.ImeConvert || key == Key.ImeAccept ||
            key == Key.ImeModeChange || key == Key.DbeAlphanumeric || key == Key.DbeKatakana ||
            key == Key.DbeHiragana || key == Key.DbeSbcsChar || key == Key.DbeDbcsChar ||
            key == Key.DbeEnterWordRegisterMode || key == Key.DbeEnterImeConfigureMode)
        {
            return;
        }

        // 处理修饰键（只记录状态，不完成录制）
        if (IsModifierKey(key))
        {
            return;
        }

        // 处理普通键 - 记录快捷键并完成录制
        _finalKey = key;
        RecordHotKey();
        _isRecording = false;
        e.Handled = true;

        // 立即更新显示
        UpdateDisplay();
    }

    private void OnPreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // 获取实际按键
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        // 如果在录制状态且松开了修饰键
        if (_isRecording && IsModifierKey(key))
        {
            // 检查是否还有其他修饰键被按下
            bool hasAnyModifier =
                Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ||
                Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) ||
                Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);

            // 如果没有其他键且没有最终键，取消录制
            if (!hasAnyModifier && !_finalKey.HasValue)
            {
                CancelRecording();
            }
        }
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        CancelRecording();
    }

    private void RecordHotKey()
    {
        var modifiers = HotKeyModifiers.None;
        int vkCode = 0;

        // 检测修饰键状态
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            modifiers |= HotKeyModifiers.Control;
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            modifiers |= HotKeyModifiers.Shift;
        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            modifiers |= HotKeyModifiers.Alt;
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            modifiers |= HotKeyModifiers.Windows;

        // 获取最终按键的虚拟键码
        if (_finalKey.HasValue)
        {
            vkCode = KeyInterop.VirtualKeyFromKey(_finalKey.Value);
        }

        HotKey = new HotKey
        {
            Enabled = true,
            Key = vkCode,
            Modifiers = modifiers
        };
    }

    private void CancelRecording()
    {
        _isRecording = false;
        _finalKey = null;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        Text = HotKey?.GetDisplayString() ?? "未设置";
    }

    private static bool IsModifierKey(Key key)
    {
        return key == Key.LeftCtrl || key == Key.RightCtrl ||
               key == Key.LeftShift || key == Key.RightShift ||
               key == Key.LeftAlt || key == Key.RightAlt ||
               key == Key.LWin || key == Key.RWin;
    }
}
