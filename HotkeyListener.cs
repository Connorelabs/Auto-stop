using System.Runtime.InteropServices;

namespace CounterStrafe;

internal enum HotkeyCommand
{
    DecreaseStrength,
    IncreaseStrength,
    ToggleEnabled,
    TestTapA,
    Exit,
}

internal sealed class HotkeyListener : IDisposable
{
    private readonly LowLevelKeyboardProc _callback;
    private readonly Action<HotkeyCommand> _onCommand;
    private readonly Dictionary<uint, HotkeyCommand> _commandByVirtualKey;
    private readonly HashSet<uint> _heldHotkeys = new();
    private nint _hookHandle;
    private bool _disposed;

    public HotkeyListener(Action<HotkeyCommand> onCommand, IEnumerable<HotkeyBinding> bindings)
    {
        _callback = HookCallback;
        _onCommand = onCommand;
        _commandByVirtualKey = bindings.ToDictionary(binding => binding.VirtualKey, binding => binding.Command);
    }

    public void Start()
    {
        if (_hookHandle != 0)
        {
            return;
        }

        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _callback,
            0,
            0);

        if (_hookHandle == 0)
        {
            throw new InvalidOperationException("Unable to install hotkey hook.");
        }
    }

    private nint HookCallback(int code, nuint wParam, nint lParam)
    {
        if (code >= 0)
        {
            var info = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            var isInjected = (info.Flags & NativeMethods.LLKHF_INJECTED) != 0;

            if (!isInjected)
            {
                var isKeyDown = wParam is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;
                var isKeyUp = wParam is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP;

                if (isKeyUp)
                {
                    _heldHotkeys.Remove(info.VirtualKeyCode);
                }
                else if (isKeyDown && _heldHotkeys.Add(info.VirtualKeyCode))
                {
                    if (_commandByVirtualKey.TryGetValue(info.VirtualKeyCode, out var command))
                    {
                        _onCommand(command);
                    }
                }
            }
        }

        return NativeMethods.CallNextHookEx(_hookHandle, code, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_hookHandle != 0)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
