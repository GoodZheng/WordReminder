using System.Runtime.InteropServices;
using System.Windows.Interop;
using WordReminder.Models;

namespace WordReminder.Services;

/// <summary>
/// 全局快捷键服务 - 使用 Windows API 注册全局快捷键
/// </summary>
public class HotKeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly Dictionary<int, RegisteredHotKey> _registeredHotKeys = new();
    private readonly Dictionary<HotKeyAction, HotKey> _actionHotKeys = new();
    private IntPtr _windowHandle;
    private HwndSource? _hwndSource;
    private int _nextId = 1;

    /// <summary>
    /// 快捷键按下事件
    /// </summary>
    public event EventHandler<HotKeyAction>? HotKeyPressed;

    /// <summary>
    /// 初始化快捷键服务（需要窗口句柄）
    /// </summary>
    public void Initialize(IntPtr windowHandle)
    {
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        _windowHandle = windowHandle;
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource?.AddHook(WndProc);
    }

    /// <summary>
    /// 注册所有快捷键
    /// </summary>
    public bool RegisterAllHotKeys(HotKeySettings settings)
    {
        UnregisterAllHotKeys();

        bool success = true;

        if (settings.Previous.IsValid())
            success &= RegisterHotKey(settings.Previous, HotKeyAction.Previous);
        if (settings.Next.IsValid())
            success &= RegisterHotKey(settings.Next, HotKeyAction.Next);
        if (settings.PlayPause.IsValid())
            success &= RegisterHotKey(settings.PlayPause, HotKeyAction.PlayPause);
        if (settings.Translation.IsValid())
            success &= RegisterHotKey(settings.Translation, HotKeyAction.Translation);
        if (settings.ToggleTopmost.IsValid())
            success &= RegisterHotKey(settings.ToggleTopmost, HotKeyAction.ToggleTopmost);

        return success;
    }

    /// <summary>
    /// 注册单个快捷键
    /// </summary>
    public bool RegisterHotKey(HotKey hotKey, HotKeyAction action)
    {
        if (!hotKey.Enabled || hotKey.Key == 0)
            return false;

        // 先取消该动作的旧快捷键
        if (_actionHotKeys.TryGetValue(action, out var oldHotKey))
        {
            UnregisterHotKeyByAction(action);
        }

        uint modifiers = ConvertModifiers(hotKey.Modifiers);
        int id = _nextId++;

        if (RegisterHotKey(_windowHandle, id, modifiers, (uint)hotKey.Key))
        {
            _registeredHotKeys[id] = new RegisteredHotKey
            {
                Id = id,
                Action = action,
                HotKey = hotKey
            };
            _actionHotKeys[action] = hotKey;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 注销单个快捷键
    /// </summary>
    private bool UnregisterHotKeyByAction(HotKeyAction action)
    {
        var entry = _registeredHotKeys.FirstOrDefault(kvp => kvp.Value.Action == action);
        if (entry.Value != null)
        {
            if (UnregisterHotKey(_windowHandle, entry.Value.Id))
            {
                _registeredHotKeys.Remove(entry.Key);
                _actionHotKeys.Remove(action);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 注销所有快捷键
    /// </summary>
    public void UnregisterAllHotKeys()
    {
        foreach (var entry in _registeredHotKeys.Values)
        {
            UnregisterHotKey(_windowHandle, entry.Id);
        }
        _registeredHotKeys.Clear();
        _actionHotKeys.Clear();
    }

    /// <summary>
    /// 检测快捷键是否与已注册的快捷键冲突
    /// </summary>
    public bool CheckConflict(HotKey hotKey, HotKeyAction? excludeAction = null)
    {
        if (!hotKey.Enabled || hotKey.Key == 0)
            return false;

        foreach (var entry in _registeredHotKeys.Values)
        {
            if (excludeAction.HasValue && entry.Action == excludeAction.Value)
                continue;

            if (entry.HotKey.Key == hotKey.Key && entry.HotKey.Modifiers == hotKey.Modifiers)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取冲突的快捷键动作
    /// </summary>
    public HotKeyAction? GetConflictAction(HotKey hotKey, HotKeyAction? excludeAction = null)
    {
        if (!hotKey.Enabled || hotKey.Key == 0)
            return null;

        foreach (var entry in _registeredHotKeys.Values)
        {
            if (excludeAction.HasValue && entry.Action == excludeAction.Value)
                continue;

            if (entry.HotKey.Key == hotKey.Key && entry.HotKey.Modifiers == hotKey.Modifiers)
                return entry.Action;
        }
        return null;
    }

    /// <summary>
    /// 获取动作的显示名称
    /// </summary>
    public static string GetActionDisplayName(HotKeyAction action)
    {
        return action switch
        {
            HotKeyAction.Previous => "上一个",
            HotKeyAction.Next => "下一个",
            HotKeyAction.PlayPause => "播放/暂停",
            HotKeyAction.Translation => "翻译",
            HotKeyAction.ToggleTopmost => "窗口置顶",
            _ => "未知"
        };
    }

    private uint ConvertModifiers(HotKeyModifiers modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(HotKeyModifiers.Alt))
            result |= 0x0001; // MOD_ALT
        if (modifiers.HasFlag(HotKeyModifiers.Control))
            result |= 0x0002; // MOD_CONTROL
        if (modifiers.HasFlag(HotKeyModifiers.Shift))
            result |= 0x0004; // MOD_SHIFT
        if (modifiers.HasFlag(HotKeyModifiers.Windows))
            result |= 0x0008; // MOD_WIN
        return result;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_registeredHotKeys.TryGetValue(id, out var entry))
            {
                HotKeyPressed?.Invoke(this, entry.Action);
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterAllHotKeys();
        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }
    }

    private class RegisteredHotKey
    {
        public int Id { get; set; }
        public HotKeyAction Action { get; set; }
        public HotKey HotKey { get; set; } = null!;
    }
}
