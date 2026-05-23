using System.Runtime.InteropServices;

namespace CounterStrafe;

internal static class NativeMethods
{
    public const int WH_KEYBOARD_LL = 13;
    public const int WH_MOUSE_LL = 14;

    public const nuint WM_KEYDOWN = 0x0100;
    public const nuint WM_KEYUP = 0x0101;
    public const nuint WM_SYSKEYDOWN = 0x0104;
    public const nuint WM_SYSKEYUP = 0x0105;
    public const nuint WM_MOUSEWHEEL = 0x020A;

    public const uint LLKHF_INJECTED = 0x00000010;
    public const uint LLMHF_INJECTED = 0x00000001;
    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_SCANCODE = 0x0008;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    public const uint MAPVK_VK_TO_VSC = 0;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    public static extern nint CallNextHookEx(nint hhk, int nCode, nuint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

    [DllImport("user32.dll")]
    public static extern int GetMessage(out Msg lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
}

[StructLayout(LayoutKind.Sequential)]
internal struct Input
{
    public uint Type;
    public InputUnion Data;
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputUnion
{
    [FieldOffset(0)]
    public KeybdInput Keyboard;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KeybdInput
{
    public ushort Vk;
    public ushort Scan;
    public uint Flags;
    public uint Time;
    public nuint DwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Msg
{
    public nint Hwnd;
    public uint Message;
    public nuint WParam;
    public nint LParam;
    public uint Time;
    public int PtX;
    public int PtY;
    public uint LPrivate;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Point
{
    public int X;
    public int Y;
}
