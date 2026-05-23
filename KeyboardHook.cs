using System.Runtime.InteropServices;

namespace CounterStrafe;

internal sealed class KeyboardHook : IDisposable
{
    private readonly LowLevelKeyboardProc _callback;
    private readonly Action<MovementKey, bool> _onKeyChanged;
    private readonly Action<bool> _onShiftChanged;
    private readonly Action<bool> _onSpaceChanged;
    private readonly HashSet<uint> _heldShiftKeys = new();
    private readonly HashSet<uint> _heldSpaceKeys = new();
    private nint _hookHandle;
    private bool _disposed;

    public KeyboardHook(
        Action<MovementKey, bool> onKeyChanged,
        Action<bool> onShiftChanged,
        Action<bool> onSpaceChanged)
    {
        _callback = HookCallback;
        _onKeyChanged = onKeyChanged;
        _onShiftChanged = onShiftChanged;
        _onSpaceChanged = onSpaceChanged;
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
            throw new InvalidOperationException("Unable to install keyboard hook.");
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

                if (TryHandleShiftKey(info.VirtualKeyCode, isKeyDown, isKeyUp))
                {
                    return NativeMethods.CallNextHookEx(_hookHandle, code, wParam, lParam);
                }

                if (TryHandleSpaceKey(info.VirtualKeyCode, isKeyDown, isKeyUp))
                {
                    return NativeMethods.CallNextHookEx(_hookHandle, code, wParam, lParam);
                }

                if (TryMapMovementKey(info.VirtualKeyCode, out var movementKey))
                {
                    if (isKeyDown)
                    {
                        _onKeyChanged(movementKey, true);
                    }
                    else if (isKeyUp)
                    {
                        _onKeyChanged(movementKey, false);
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

    private static bool TryMapMovementKey(uint virtualKey, out MovementKey key)
    {
        switch (virtualKey)
        {
            case 0x57:
                key = MovementKey.W;
                return true;
            case 0x41:
                key = MovementKey.A;
                return true;
            case 0x53:
                key = MovementKey.S;
                return true;
            case 0x44:
                key = MovementKey.D;
                return true;
            default:
                key = default;
                return false;
        }
    }

    private bool TryHandleShiftKey(uint virtualKey, bool isKeyDown, bool isKeyUp)
    {
        if (!IsShiftKey(virtualKey))
        {
            return false;
        }

        if (isKeyDown)
        {
            if (_heldShiftKeys.Add(virtualKey) && _heldShiftKeys.Count == 1)
            {
                _onShiftChanged(true);
            }
        }
        else if (isKeyUp)
        {
            if (_heldShiftKeys.Remove(virtualKey) && _heldShiftKeys.Count == 0)
            {
                _onShiftChanged(false);
            }
        }

        return true;
    }

    private static bool IsShiftKey(uint virtualKey) =>
        virtualKey is 0x10 or 0xA0 or 0xA1;

    private bool TryHandleSpaceKey(uint virtualKey, bool isKeyDown, bool isKeyUp)
    {
        if (virtualKey != 0x20)
        {
            return false;
        }

        if (isKeyDown)
        {
            if (_heldSpaceKeys.Add(virtualKey) && _heldSpaceKeys.Count == 1)
            {
                _onSpaceChanged(true);
            }
        }
        else if (isKeyUp)
        {
            if (_heldSpaceKeys.Remove(virtualKey) && _heldSpaceKeys.Count == 0)
            {
                _onSpaceChanged(false);
            }
        }

        return true;
    }
}

internal delegate nint LowLevelKeyboardProc(int code, nuint wParam, nint lParam);

[StructLayout(LayoutKind.Sequential)]
internal struct KbdLlHookStruct
{
    public uint VirtualKeyCode;
    public uint ScanCode;
    public uint Flags;
    public uint Time;
    public nuint DwExtraInfo;
}

internal delegate nint LowLevelMouseProc(int code, nuint wParam, nint lParam);
