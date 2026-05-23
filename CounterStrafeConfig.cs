using System.Text.Json;

namespace CounterStrafe;

internal sealed record CounterStrafeConfig
{
    public const string FileName = "counterstrafe.json";

    public static CounterStrafeConfig Default => new();

    public string GameStateListenerPrefix { get; init; } = "http://127.0.0.1:3001/";

    public int StrengthStepMilliseconds { get; init; } = 5;

    public int MinimumStrengthMilliseconds { get; init; } = 20;

    public int MaximumStrengthMilliseconds { get; init; } = 120;

    public int DefaultStrengthMilliseconds { get; init; } = 60;

    public int SpaceReleaseSuspendMilliseconds { get; init; } = 40;

    public int MouseWheelJumpSuspendMilliseconds { get; init; } = 220;

    public int ManualOverrideSuppressMilliseconds { get; init; } = 80;

    public HotkeyConfig Hotkeys { get; init; } = new();

    public WeaponSuppressConfig WeaponSuppress { get; init; } = new();

    public static CounterStrafeConfig Load()
    {
        foreach (var path in AppPaths.EnumerateCandidateConfigDirectories().Select(directory => Path.Combine(directory, FileName)))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                };

                var parsed = JsonSerializer.Deserialize<CounterStrafeConfig>(json, options) ?? Default;
                return Normalize(parsed);
            }
            catch
            {
                return Default;
            }
        }

        return Default;
    }

    private static CounterStrafeConfig Normalize(CounterStrafeConfig config)
    {
        var min = Math.Clamp(config.MinimumStrengthMilliseconds, 1, 500);
        var max = Math.Clamp(config.MaximumStrengthMilliseconds, min, 500);
        var step = Math.Clamp(config.StrengthStepMilliseconds, 1, 100);
        var @default = Math.Clamp(config.DefaultStrengthMilliseconds, min, max);
        var spaceReleaseSuspend = Math.Clamp(config.SpaceReleaseSuspendMilliseconds, 0, 1000);
        var jumpSuspend = Math.Clamp(config.MouseWheelJumpSuspendMilliseconds, 0, 2000);
        var manualOverrideSuppress = Math.Clamp(config.ManualOverrideSuppressMilliseconds, 0, 1000);

        return config with
        {
            StrengthStepMilliseconds = step,
            MinimumStrengthMilliseconds = min,
            MaximumStrengthMilliseconds = max,
            DefaultStrengthMilliseconds = @default,
            SpaceReleaseSuspendMilliseconds = spaceReleaseSuspend,
            MouseWheelJumpSuspendMilliseconds = jumpSuspend,
            ManualOverrideSuppressMilliseconds = manualOverrideSuppress,
            Hotkeys = HotkeyConfig.Normalize(config.Hotkeys),
            WeaponSuppress = WeaponSuppressConfig.Normalize(config.WeaponSuppress),
        };
    }
}

internal sealed record HotkeyConfig
{
    public string DecreaseStrength { get; init; } = "F6";

    public string IncreaseStrength { get; init; } = "F7";

    public string ToggleEnabled { get; init; } = "F8";

    public string TestTapA { get; init; } = "F9";

    public string Exit { get; init; } = "F10";

    public static HotkeyConfig Normalize(HotkeyConfig? config)
    {
        config ??= new HotkeyConfig();
        return new HotkeyConfig
        {
            DecreaseStrength = NormalizeKey(config.DecreaseStrength, "F6"),
            IncreaseStrength = NormalizeKey(config.IncreaseStrength, "F7"),
            ToggleEnabled = NormalizeKey(config.ToggleEnabled, "F8"),
            TestTapA = NormalizeKey(config.TestTapA, "F9"),
            Exit = NormalizeKey(config.Exit, "F10"),
        };
    }

    private static string NormalizeKey(string? keyName, string fallback)
    {
        if (string.IsNullOrWhiteSpace(keyName))
        {
            return fallback;
        }

        return keyName.Trim().ToUpperInvariant();
    }
}

internal sealed record WeaponSuppressConfig
{
    public string[] Types { get; init; } = ["knife", "grenade", "c4"];

    public string[] Names { get; init; } =
    [
        "weapon_knife",
        "weapon_knife_t",
        "weapon_bayonet",
        "weapon_knife_css",
        "weapon_knife_flip",
        "weapon_knife_gut",
        "weapon_knife_karambit",
        "weapon_knife_m9_bayonet",
        "weapon_knife_tactical",
        "weapon_knife_falchion",
        "weapon_knife_survival_bowie",
        "weapon_knife_butterfly",
        "weapon_knife_push",
        "weapon_knife_cord",
        "weapon_knife_canis",
        "weapon_knife_ursus",
        "weapon_knife_gypsy_jackknife",
        "weapon_knife_outdoor",
        "weapon_knife_stiletto",
        "weapon_knife_widowmaker",
        "weapon_knifegg",
        "weapon_fists",
        "weapon_breachcharge",
        "weapon_bumpmine",
        "weapon_tablet"
    ];

    public static WeaponSuppressConfig Normalize(WeaponSuppressConfig? config)
    {
        config ??= new WeaponSuppressConfig();
        return new WeaponSuppressConfig
        {
            Types = NormalizeList(config.Types, ["knife", "grenade", "c4"]),
            Names = NormalizeList(
                config.Names,
                [
                    "weapon_knife",
                    "weapon_knife_t",
                    "weapon_bayonet",
                    "weapon_knife_css",
                    "weapon_knife_flip",
                    "weapon_knife_gut",
                    "weapon_knife_karambit",
                    "weapon_knife_m9_bayonet",
                    "weapon_knife_tactical",
                    "weapon_knife_falchion",
                    "weapon_knife_survival_bowie",
                    "weapon_knife_butterfly",
                    "weapon_knife_push",
                    "weapon_knife_cord",
                    "weapon_knife_canis",
                    "weapon_knife_ursus",
                    "weapon_knife_gypsy_jackknife",
                    "weapon_knife_outdoor",
                    "weapon_knife_stiletto",
                    "weapon_knife_widowmaker",
                    "weapon_knifegg",
                    "weapon_fists",
                    "weapon_breachcharge",
                    "weapon_bumpmine",
                    "weapon_tablet"
                ]),
        };
    }

    private static string[] NormalizeList(string[]? values, string[] fallback)
    {
        if (values is null || values.Length == 0)
        {
            return fallback;
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
