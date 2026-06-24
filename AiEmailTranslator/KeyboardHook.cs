using System.Runtime.InteropServices;

namespace AiEmailTranslator;

internal sealed class KeyboardHook : IDisposable
{
    private readonly Control _owner;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;
    private IntPtr _hookHandle;
    private DateTime _lastTranslate = DateTime.MinValue;
    private DateTime _lastSettings = DateTime.MinValue;

    public event EventHandler? TranslatePressed;
    public event EventHandler? SettingsPressed;

    public KeyboardHook(Control owner)
    {
        _owner = owner;
        _proc = HookCallback;
    }

    public bool Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return true;
        }

        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WhKeyboardLl,
            _proc,
            NativeMethods.GetModuleHandle(null),
            0);

        return _hookHandle != IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsKeyDownMessage(wParam))
        {
            var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            if (info.VkCode == NativeMethods.VkT && IsWinDown() && !IsControlDown())
            {
                RaiseThrottled(TranslatePressed, ref _lastTranslate);
                return 1;
            }

            if (info.VkCode == NativeMethods.VkT && IsWinDown() && IsControlDown())
            {
                RaiseThrottled(SettingsPressed, ref _lastSettings);
                return 1;
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void RaiseThrottled(EventHandler? handler, ref DateTime lastRaised)
    {
        var now = DateTime.UtcNow;
        if ((now - lastRaised).TotalMilliseconds < 500)
        {
            return;
        }

        lastRaised = now;
        if (handler is null || _owner.IsDisposed)
        {
            return;
        }

        _owner.BeginInvoke(() => handler(this, EventArgs.Empty));
    }

    private static bool IsKeyDownMessage(IntPtr wParam)
    {
        var message = wParam.ToInt32();
        return message is NativeMethods.WmKeydown or NativeMethods.WmSyskeydown;
    }

    private static bool IsWinDown() =>
        IsKeyDown(NativeMethods.VkLwin) || IsKeyDown(NativeMethods.VkRwin);

    private static bool IsControlDown() =>
        IsKeyDown(NativeMethods.VkControl);

    private static bool IsShiftDown() =>
        IsKeyDown(NativeMethods.VkShift);

    private static bool IsKeyDown(int virtualKey) =>
        (NativeMethods.GetAsyncKeyState(virtualKey) & unchecked((short)0x8000)) != 0;
}
