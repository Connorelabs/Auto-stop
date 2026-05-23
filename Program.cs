namespace CounterStrafe;

internal static class Program
{
    private static readonly CounterStrafeEngine Engine = new();
    private static readonly object SyncRoot = new();
    private static readonly object StateLock = new();
    private static readonly CounterStrafeConfig Config = CounterStrafeConfig.Load();
    private static readonly HotkeyBindings Hotkeys = HotkeyBindings.Create(Config.Hotkeys);
    private static readonly CounterStrafeStateStore StateStore = new();

    private static bool _enabled = true;
    private static bool _shiftSuspended;
    private static bool _spaceSuspended;
    private static DateTime _wheelSuspendUntilUtc = DateTime.MinValue;
    private static int _counterStrafeHoldMilliseconds =
        Math.Clamp(
            StateStore.LoadStrengthOrDefault(Config.DefaultStrengthMilliseconds),
            Config.MinimumStrengthMilliseconds,
            Config.MaximumStrengthMilliseconds);
    private static ActiveWeaponInfo? _activeWeapon;

    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0].Equals("--self-test", StringComparison.OrdinalIgnoreCase))
        {
            return SelfTest.Run();
        }

        Console.WriteLine("CounterStrafe is running.");
        Console.WriteLine($"{Hotkeys.DecreaseStrength.KeyName}: strength -{Config.StrengthStepMilliseconds}ms");
        Console.WriteLine($"{Hotkeys.IncreaseStrength.KeyName}: strength +{Config.StrengthStepMilliseconds}ms");
        Console.WriteLine($"{Hotkeys.ToggleEnabled.KeyName}: toggle on/off");
        Console.WriteLine($"{Hotkeys.TestTapA.KeyName}: test tap A");
        Console.WriteLine($"{Hotkeys.Exit.KeyName}: exit");
        Console.WriteLine("Hold Shift: temporary suspend");
        Console.WriteLine("Space / wheel jump: temporary suspend");
        Console.WriteLine("Knife / grenade / C4: temporary suspend via GSI");
        Console.WriteLine($"Current strength: {_counterStrafeHoldMilliseconds}ms");
        Console.WriteLine($"GSI listener: {Config.GameStateListenerPrefix}");
        Console.WriteLine($"Config folder: {AppPaths.GetPreferredConfigDirectory()}");
        Console.WriteLine();

        using var toggleListener = new HotkeyListener(ToggleHotkeys, Hotkeys.All);
        using var movementHook = new KeyboardHook(HandleMovementKeyChanged, HandleShiftChanged, HandleSpaceChanged);
        using var mouseHook = new MouseHook(HandleMouseWheelJump);
        using var gameStateServer = new GameStateIntegrationServer(Config.GameStateListenerPrefix, HandleActiveWeaponChanged);

        toggleListener.Start();
        movementHook.Start();
        mouseHook.Start();
        gameStateServer.Start();

        while (NativeMethods.GetMessage(out _, 0, 0, 0) > 0)
        {
        }

        return 0;
    }

    private static void HandleMovementKeyChanged(MovementKey key, bool isDown)
    {
        var axis = MovementKeyHelper.GetAxis(key);
        SyntheticInput.NotifyPhysicalKeyChanged(key, isDown, Config.ManualOverrideSuppressMilliseconds);
        SyntheticInput.CancelAxis(axis);

        CounterStrafeAction? action;
        lock (SyncRoot)
        {
            action = isDown ? Engine.HandleKeyDown(key) : Engine.HandleKeyUp(key);
        }

        var suppressNextAutoTapOnAxis =
            !isDown &&
            action is not null &&
            SyntheticInput.ConsumeSuppressedNextAutoTap(axis);
        var suppressAutoTapOnRelease = !isDown && SyntheticInput.ConsumeSuppressedAutoTapRelease(key);

        if (!_enabled || IsMovementSuppressed())
        {
            return;
        }

        if (suppressAutoTapOnRelease)
        {
            Console.WriteLine($"Manual override release: {key}");
            return;
        }

        if (suppressNextAutoTapOnAxis)
        {
            Console.WriteLine($"Manual priority suppress: {axis}");
            return;
        }

        if (action is not null)
        {
            Console.WriteLine($"CounterStrafe tap: {action.KeyToTap} ({_counterStrafeHoldMilliseconds}ms)");
            SyntheticInput.Tap(action.KeyToTap, holdMilliseconds: _counterStrafeHoldMilliseconds);
        }
    }

    private static void HandleShiftChanged(bool isDown)
    {
        lock (StateLock)
        {
            _shiftSuspended = isDown;
        }

        CancelAllSyntheticInput();

        Console.WriteLine(isDown ? "CounterStrafe: SUSPENDED (Shift)" : "CounterStrafe: RESUMED");
    }

    private static void HandleSpaceChanged(bool isDown)
    {
        lock (StateLock)
        {
            _spaceSuspended = isDown;
            if (!isDown)
            {
                _wheelSuspendUntilUtc = DateTime.UtcNow.AddMilliseconds(Config.SpaceReleaseSuspendMilliseconds);
            }
        }

        CancelAllSyntheticInput();
    }

    private static void HandleMouseWheelJump()
    {
        lock (StateLock)
        {
            _wheelSuspendUntilUtc = DateTime.UtcNow.AddMilliseconds(Config.MouseWheelJumpSuspendMilliseconds);
        }

        CancelAllSyntheticInput();
    }

    private static void ToggleHotkeys(HotkeyCommand command)
    {
        switch (command)
        {
            case HotkeyCommand.DecreaseStrength:
                _counterStrafeHoldMilliseconds = Math.Max(
                    Config.MinimumStrengthMilliseconds,
                    _counterStrafeHoldMilliseconds - Config.StrengthStepMilliseconds);
                StateStore.SaveStrength(_counterStrafeHoldMilliseconds);
                Console.WriteLine($"CounterStrafe strength: {_counterStrafeHoldMilliseconds}ms");
                break;
            case HotkeyCommand.IncreaseStrength:
                _counterStrafeHoldMilliseconds = Math.Min(
                    Config.MaximumStrengthMilliseconds,
                    _counterStrafeHoldMilliseconds + Config.StrengthStepMilliseconds);
                StateStore.SaveStrength(_counterStrafeHoldMilliseconds);
                Console.WriteLine($"CounterStrafe strength: {_counterStrafeHoldMilliseconds}ms");
                break;
            case HotkeyCommand.ToggleEnabled:
                _enabled = !_enabled;
                CancelAllSyntheticInput();
                Console.WriteLine(_enabled ? "CounterStrafe: ON" : "CounterStrafe: OFF");
                break;
            case HotkeyCommand.TestTapA:
                Console.WriteLine($"Manual test tap: A ({_counterStrafeHoldMilliseconds}ms)");
                SyntheticInput.Tap(MovementKey.A, holdMilliseconds: _counterStrafeHoldMilliseconds);
                break;
            case HotkeyCommand.Exit:
                Environment.Exit(0);
                break;
        }
    }

    private static bool IsMovementSuppressed()
    {
        lock (StateLock)
        {
            return
                _shiftSuspended ||
                _spaceSuspended ||
                DateTime.UtcNow < _wheelSuspendUntilUtc ||
                WeaponPolicy.ShouldSuppressCounterStrafe(_activeWeapon, Config.WeaponSuppress);
        }
    }

    private static void HandleActiveWeaponChanged(ActiveWeaponInfo? weapon)
    {
        var shouldCancelSyntheticInput = false;

        lock (StateLock)
        {
            _activeWeapon = weapon;
            shouldCancelSyntheticInput = WeaponPolicy.ShouldSuppressCounterStrafe(weapon, Config.WeaponSuppress);
        }

        if (shouldCancelSyntheticInput)
        {
            CancelAllSyntheticInput();
        }

        Console.WriteLine(
            shouldCancelSyntheticInput
                ? $"Weapon suppress: {WeaponPolicy.Describe(weapon)}"
                : $"Weapon active: {WeaponPolicy.Describe(weapon)}");
    }

    private static void CancelAllSyntheticInput()
    {
        SyntheticInput.CancelAxis(MovementAxis.Horizontal);
        SyntheticInput.CancelAxis(MovementAxis.Vertical);
    }
}
