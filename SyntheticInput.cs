using System.Runtime.InteropServices;

namespace CounterStrafe;

internal static class SyntheticInput
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<MovementAxis, ActiveTap> ActiveTaps = new();
    private static readonly HashSet<MovementKey> PhysicallyHeldKeys = new();
    private static readonly HashSet<MovementKey> SuppressedAutoTapReleaseKeys = new();
    private static readonly Dictionary<MovementAxis, DateTime> SuppressedNextAutoTapAxes = new();

    public static void Tap(MovementKey key, int holdMilliseconds)
    {
        var axis = MovementKeyHelper.GetAxis(key);
        ActiveTap tap;

        lock (SyncRoot)
        {
            CancelAxisNoLock(axis);
            tap = new ActiveTap(axis, key);
            ActiveTaps[axis] = tap;
        }

        SendKey(key, isKeyUp: false);
        _ = ReleaseAfterDelayAsync(tap, holdMilliseconds);
    }

    public static void NotifyPhysicalKeyChanged(MovementKey key, bool isDown, int manualOverrideSuppressMilliseconds)
    {
        lock (SyncRoot)
        {
            if (isDown)
            {
                var axis = MovementKeyHelper.GetAxis(key);
                if (HasOtherPhysicalKeyOnAxisNoLock(axis, key))
                {
                    SuppressNextAutoTapAxisNoLock(axis, manualOverrideSuppressMilliseconds);
                }

                PhysicallyHeldKeys.Add(key);

                if (ActiveTaps.TryGetValue(axis, out var tap) && tap.Key == key)
                {
                    tap.IsOwnedByPhysicalKey = true;
                    SuppressedAutoTapReleaseKeys.Add(key);
                    SuppressNextAutoTapAxisNoLock(axis, manualOverrideSuppressMilliseconds);
                }
            }
            else
            {
                PhysicallyHeldKeys.Remove(key);
            }
        }
    }

    public static bool ConsumeSuppressedAutoTapRelease(MovementKey key)
    {
        lock (SyncRoot)
        {
            return SuppressedAutoTapReleaseKeys.Remove(key);
        }
    }

    public static bool ConsumeSuppressedNextAutoTap(MovementAxis axis)
    {
        lock (SyncRoot)
        {
            if (!SuppressedNextAutoTapAxes.TryGetValue(axis, out var suppressUntilUtc))
            {
                return false;
            }

            if (DateTime.UtcNow > suppressUntilUtc)
            {
                SuppressedNextAutoTapAxes.Remove(axis);
                return false;
            }

            SuppressedNextAutoTapAxes.Remove(axis);
            return true;
        }
    }

    public static void CancelAxis(MovementAxis axis)
    {
        lock (SyncRoot)
        {
            CancelAxisNoLock(axis);
        }
    }

    private static uint ToVirtualKey(MovementKey key) =>
        key switch
        {
            MovementKey.W => 0x57,
            MovementKey.A => 0x41,
            MovementKey.S => 0x53,
            MovementKey.D => 0x44,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null),
        };

    private static async Task ReleaseAfterDelayAsync(ActiveTap tap, int holdMilliseconds)
    {
        try
        {
            await Task.Delay(holdMilliseconds, tap.Cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (!ActiveTaps.TryGetValue(tap.Axis, out var activeTap) || !ReferenceEquals(activeTap, tap))
            {
                return;
            }

            ActiveTaps.Remove(tap.Axis);
        }

        ReleaseTapIfNeeded(tap);
    }

    private static void SendKey(MovementKey key, bool isKeyUp)
    {
        var input = CreateKeyboardInput(key, isKeyUp, useScanCode: false);
        var sent = NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
        if (sent == 1)
        {
            return;
        }

        var virtualKey = (byte)ToVirtualKey(key);
        var scanCode = (byte)NativeMethods.MapVirtualKey(virtualKey, NativeMethods.MAPVK_VK_TO_VSC);
        var flags = isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0u;
        NativeMethods.keybd_event(virtualKey, scanCode, flags, 0);
    }

    private static Input CreateKeyboardInput(MovementKey key, bool isKeyUp, bool useScanCode)
    {
        var virtualKey = (ushort)ToVirtualKey(key);
        var scanCode = (ushort)NativeMethods.MapVirtualKey(virtualKey, NativeMethods.MAPVK_VK_TO_VSC);
        var flags = isKeyUp ? NativeMethods.KEYEVENTF_KEYUP : 0u;

        return new Input
        {
            Type = NativeMethods.INPUT_KEYBOARD,
            Data = new InputUnion
            {
                Keyboard = new KeybdInput
                {
                    Vk = useScanCode ? (ushort)0 : virtualKey,
                    Scan = useScanCode ? scanCode : (ushort)0,
                    Flags = useScanCode ? flags | NativeMethods.KEYEVENTF_SCANCODE : flags,
                },
            },
        };
    }

    private static void CancelAxisNoLock(MovementAxis axis)
    {
        if (!ActiveTaps.Remove(axis, out var tap))
        {
            return;
        }

        tap.Cancellation.Cancel();
        ReleaseTapIfNeeded(tap);
    }

    private static void ReleaseTapIfNeeded(ActiveTap tap)
    {
        lock (SyncRoot)
        {
            if (tap.IsOwnedByPhysicalKey && PhysicallyHeldKeys.Contains(tap.Key))
            {
                return;
            }
        }

        SendKey(tap.Key, isKeyUp: true);
    }

    private static bool HasOtherPhysicalKeyOnAxisNoLock(MovementAxis axis, MovementKey currentKey)
    {
        foreach (var heldKey in PhysicallyHeldKeys)
        {
            if (heldKey != currentKey && MovementKeyHelper.GetAxis(heldKey) == axis)
            {
                return true;
            }
        }

        return false;
    }

    private static void SuppressNextAutoTapAxisNoLock(MovementAxis axis, int suppressMilliseconds)
    {
        if (suppressMilliseconds <= 0)
        {
            return;
        }

        SuppressedNextAutoTapAxes[axis] = DateTime.UtcNow.AddMilliseconds(suppressMilliseconds);
    }

    private sealed class ActiveTap
    {
        public ActiveTap(MovementAxis axis, MovementKey key)
        {
            Axis = axis;
            Key = key;
            Cancellation = new CancellationTokenSource();
        }

        public MovementAxis Axis { get; }

        public MovementKey Key { get; }

        public CancellationTokenSource Cancellation { get; }

        public bool IsOwnedByPhysicalKey { get; set; }
    }
}
