using System.Runtime.InteropServices;

namespace AiEmailTranslator;

internal static class NativeMethods
{
    public const int WmHotkey = 0x0312;
    public const int WhKeyboardLl = 13;
    public const int WmKeydown = 0x0100;
    public const int WmSyskeydown = 0x0104;
    public const uint ModControl = 0x0002;
    public const uint ModAlt = 0x0001;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;
    public const int VkAlt = 0x12;
    public const int VkControl = 0x11;
    public const int VkShift = 0x10;
    public const int VkLwin = 0x5B;
    public const int VkRwin = 0x5C;
    public const uint VkF = 0x46;
    public const uint VkU = 0x55;
    public const uint VkY = 0x59;
    public const uint VkS = 0x53;
    public const uint VkT = 0x54;

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? pszSubAppName, string? pszSubIdList);

    public static void SetImmersiveDarkMode(IntPtr handle, bool enabled)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var value = enabled ? 1 : 0;
        if (DwmSetWindowAttribute(handle, 20, ref value, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(handle, 19, ref value, sizeof(int));
        }
    }

    public static void SetControlDarkTheme(IntPtr handle, bool enabled)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        _ = SetWindowTheme(handle, enabled ? "DarkMode_Explorer" : null, null);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct KbdLlHookStruct
{
    public uint VkCode;
    public uint ScanCode;
    public uint Flags;
    public uint Time;
    public IntPtr DwExtraInfo;
}
