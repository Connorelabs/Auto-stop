using System.Runtime.InteropServices;

namespace CounterStrafe;

internal sealed class MouseHook : IDisposable
{
    private readonly LowLevelMouseProc _callback;
    private readonly Action _onWheel;
    private nint _hookHandle;
    private bool _disposed;

    public MouseHook(Action onWheel)
    {
        _callback = HookCallback;
        _onWheel = onWheel;
    }

    public void Start()
    {
        if (_hookHandle != 0)
        {
            return;
        }

        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_MOUSE_LL,
            _callback,
            0,
            0);

        if (_hookHandle == 0)
        {
            throw new InvalidOperationException("Unable to install mouse hook.");
        }
    }

    private nint HookCallback(int code, nuint wParam, nint lParam)
    {
        if (code >= 0 && wParam == NativeMethods.WM_MOUSEWHEEL)
        {
            var info = Marshal.PtrToStructure<MsLlHookStruct>(lParam);
            var isInjected = (info.Flags & NativeMethods.LLMHF_INJECTED) != 0;
            if (!isInjected)
            {
                _onWheel();
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

[StructLayout(LayoutKind.Sequential)]
internal struct MsLlHookStruct
{
    public Point Pt;
    public uint MouseData;
    public uint Flags;
    public uint Time;
    public nuint DwExtraInfo;
}
