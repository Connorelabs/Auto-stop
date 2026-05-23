namespace CounterStrafe;

internal static class SelfTest
{
    public static int Run()
    {
        var tests = new (string Name, Func<bool> Check)[]
        {
            ("Release D taps A", ReleaseDTriggersA),
            ("A then D release A does nothing", ReleaseOlderKeyDoesNothing),
            ("A then D release D while A held does not tap", ReleaseLatestKeyWhileOlderHeldDoesNotTap),
            ("W and D are independent", AxesAreIndependent),
            ("Rifle does not suppress", RifleDoesNotSuppress),
            ("Grenade suppresses", GrenadeSuppresses),
            ("Knife suppresses", KnifeSuppresses),
        };

        foreach (var test in tests)
        {
            if (!test.Check())
            {
                Console.Error.WriteLine($"FAILED: {test.Name}");
                return 1;
            }
        }

        Console.WriteLine($"All {tests.Length} self-tests passed.");
        return 0;
    }

    private static bool ReleaseDTriggersA()
    {
        var engine = new CounterStrafeEngine();
        engine.HandleKeyDown(MovementKey.D);
        var action = engine.HandleKeyUp(MovementKey.D);
        return action?.KeyToTap == MovementKey.A;
    }

    private static bool ReleaseOlderKeyDoesNothing()
    {
        var engine = new CounterStrafeEngine();
        engine.HandleKeyDown(MovementKey.A);
        engine.HandleKeyDown(MovementKey.D);
        var action = engine.HandleKeyUp(MovementKey.A);
        return action is null;
    }

    private static bool ReleaseLatestKeyWhileOlderHeldDoesNotTap()
    {
        var engine = new CounterStrafeEngine();
        engine.HandleKeyDown(MovementKey.A);
        engine.HandleKeyDown(MovementKey.D);
        var action = engine.HandleKeyUp(MovementKey.D);
        return action is null && engine.GetEffectiveKey(MovementAxis.Horizontal) == MovementKey.A;
    }

    private static bool AxesAreIndependent()
    {
        var engine = new CounterStrafeEngine();
        engine.HandleKeyDown(MovementKey.W);
        engine.HandleKeyDown(MovementKey.D);
        var action = engine.HandleKeyUp(MovementKey.D);
        return action?.KeyToTap == MovementKey.A && engine.GetEffectiveKey(MovementAxis.Vertical) == MovementKey.W;
    }

    private static bool RifleDoesNotSuppress() =>
        !WeaponPolicy.ShouldSuppressCounterStrafe(
            new ActiveWeaponInfo("weapon_ak47", "Rifle", "active"),
            CounterStrafeConfig.Default.WeaponSuppress);

    private static bool GrenadeSuppresses() =>
        WeaponPolicy.ShouldSuppressCounterStrafe(
            new ActiveWeaponInfo("weapon_flashbang", "Grenade", "active"),
            CounterStrafeConfig.Default.WeaponSuppress);

    private static bool KnifeSuppresses() =>
        WeaponPolicy.ShouldSuppressCounterStrafe(
            new ActiveWeaponInfo("weapon_knife", "Knife", "active"),
            CounterStrafeConfig.Default.WeaponSuppress);
}
