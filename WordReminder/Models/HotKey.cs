using System.Text.Json.Serialization;

namespace WordReminder.Models;

/// <summary>
/// 全局快捷键配置
/// </summary>
public class HotKey
{
    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 主键码（Key）
    /// </summary>
    [JsonPropertyName("key")]
    public int Key { get; set; } = 0;

    /// <summary>
    /// 修饰键（Ctrl/Shift/Alt/Win）
    /// </summary>
    [JsonPropertyName("modifiers")]
    public HotKeyModifiers Modifiers { get; set; } = HotKeyModifiers.None;

    /// <summary>
    /// 获取显示字符串
    /// </summary>
    public string GetDisplayString()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(HotKeyModifiers.Control))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotKeyModifiers.Shift))
            parts.Add("Shift");
        if (Modifiers.HasFlag(HotKeyModifiers.Alt))
            parts.Add("Alt");
        if (Modifiers.HasFlag(HotKeyModifiers.Windows))
            parts.Add("Win");

        if (Key > 0)
        {
            var keyName = KeyToString(Key);
            parts.Add(keyName);
        }

        return parts.Count > 0 ? string.Join("+", parts) : "未设置";
    }

    /// <summary>
    /// 将虚拟键码转换为字符串
    /// </summary>
    private static string KeyToString(int key)
    {
        return key switch
        {
            // 功能键
            0x01 => "Left Mouse",
            0x02 => "Right Mouse",
            0x03 => "Cancel",
            0x04 => "Middle Mouse",
            0x05 => "X1 Mouse",
            0x06 => "X2 Mouse",
            0x08 => "Backspace",
            0x09 => "Tab",
            0x0C => "Clear",
            0x0D => "Enter",
            0x10 => "Shift",
            0x11 => "Ctrl",
            0x12 => "Alt",
            0x13 => "Pause",
            0x14 => "Caps Lock",
            0x1B => "Esc",
            0x20 => "Space",
            0x21 => "Page Up",
            0x22 => "Page Down",
            0x23 => "End",
            0x24 => "Home",
            0x25 => "Left",
            0x26 => "Up",
            0x27 => "Right",
            0x28 => "Down",
            0x29 => "Select",
            0x2A => "Print",
            0x2B => "Execute",
            0x2C => "Print Screen",
            0x2D => "Insert",
            0x2E => "Delete",
            0x2F => "Help",
            0x30 => "0", 0x31 => "1", 0x32 => "2", 0x33 => "3", 0x34 => "4",
            0x35 => "5", 0x36 => "6", 0x37 => "7", 0x38 => "8", 0x39 => "9",
            0x41 => "A", 0x42 => "B", 0x43 => "C", 0x44 => "D", 0x45 => "E",
            0x46 => "F", 0x47 => "G", 0x48 => "H", 0x49 => "I", 0x4A => "J",
            0x4B => "K", 0x4C => "L", 0x4D => "M", 0x4E => "N", 0x4F => "O",
            0x50 => "P", 0x51 => "Q", 0x52 => "R", 0x53 => "S", 0x54 => "T",
            0x55 => "U", 0x56 => "V", 0x57 => "W", 0x58 => "X", 0x59 => "Y",
            0x5A => "Z",
            0x60 => "Num 0", 0x61 => "Num 1", 0x62 => "Num 2", 0x63 => "Num 3",
            0x64 => "Num 4", 0x65 => "Num 5", 0x66 => "Num 6", 0x67 => "Num 7",
            0x68 => "Num 8", 0x69 => "Num 9",
            0x6A => "Multiply", 0x6B => "Add", 0x6C => "Separator", 0x6D => "Subtract",
            0x6E => "Decimal", 0x6F => "Divide",
            0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4", 0x74 => "F5",
            0x75 => "F6", 0x76 => "F7", 0x77 => "F8", 0x78 => "F9", 0x79 => "F10",
            0x7A => "F11", 0x7B => "F12", 0x7C => "F13", 0x7D => "F14", 0x7E => "F15",
            0x7F => "F16", 0x80 => "F17", 0x81 => "F18", 0x82 => "F19", 0x83 => "F20",
            0x84 => "F21", 0x85 => "F22", 0x86 => "F23", 0x87 => "F24",
            0x90 => "Num Lock",
            0x91 => "Scroll Lock",
            0xBA => ";", 0xBB => "=", 0xBC => ",", 0xBD => "-", 0xBE => ".",
            0xBF => "/", 0xC0 => "`", 0xDB => "[", 0xDC => "\\", 0xDD => "]",
            0xDE => "'",
            _ => $"0x{key:X2}"
        };
    }

    /// <summary>
    /// 判断快捷键是否有效
    /// </summary>
    public bool IsValid()
    {
        return Enabled && Key > 0;
    }

    /// <summary>
    /// 创建快捷键的深拷贝
    /// </summary>
    public HotKey Clone()
    {
        return new HotKey
        {
            Enabled = Enabled,
            Key = Key,
            Modifiers = Modifiers
        };
    }
}

/// <summary>
/// 快捷键修饰键标志
/// </summary>
[Flags]
public enum HotKeyModifiers
{
    None = 0x0000,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Windows = 0x0008
}

/// <summary>
/// 快捷键动作类型
/// </summary>
public enum HotKeyAction
{
    Previous,
    Next,
    PlayPause,
    Translation,
    ToggleTopmost
}

/// <summary>
/// 快捷键设置集合
/// </summary>
public class HotKeySettings
{
    [JsonPropertyName("previous")]
    public HotKey Previous { get; set; } = new();

    [JsonPropertyName("next")]
    public HotKey Next { get; set; } = new();

    [JsonPropertyName("playPause")]
    public HotKey PlayPause { get; set; } = new();

    [JsonPropertyName("translation")]
    public HotKey Translation { get; set; } = new();

    [JsonPropertyName("toggleTopmost")]
    public HotKey ToggleTopmost { get; set; } = new();
}
