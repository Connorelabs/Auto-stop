namespace CounterStrafe;

internal sealed class HotkeyBindings
{
    public required HotkeyBinding DecreaseStrength { get; init; }

    public required HotkeyBinding IncreaseStrength { get; init; }

    public required HotkeyBinding ToggleEnabled { get; init; }

    public required HotkeyBinding TestTapA { get; init; }

    public required HotkeyBinding Exit { get; init; }

    public IEnumerable<HotkeyBinding> All
    {
        get
        {
            yield return DecreaseStrength;
            yield return IncreaseStrength;
            yield return ToggleEnabled;
            yield return TestTapA;
            yield return Exit;
        }
    }

    public static HotkeyBindings Create(HotkeyConfig config)
    {
        return new HotkeyBindings
        {
            DecreaseStrength = CreateBinding(config.DecreaseStrength, HotkeyCommand.DecreaseStrength),
            IncreaseStrength = CreateBinding(config.IncreaseStrength, HotkeyCommand.IncreaseStrength),
            ToggleEnabled = CreateBinding(config.ToggleEnabled, HotkeyCommand.ToggleEnabled),
            TestTapA = CreateBinding(config.TestTapA, HotkeyCommand.TestTapA),
            Exit = CreateBinding(config.Exit, HotkeyCommand.Exit),
        };
    }

    private static HotkeyBinding CreateBinding(string keyName, HotkeyCommand command)
    {
        if (!VirtualKeyParser.TryParse(keyName, out var virtualKey))
        {
            throw new InvalidOperationException($"Unsupported hotkey name: {keyName}");
        }

        return new HotkeyBinding(command, keyName, virtualKey);
    }
}

internal sealed record HotkeyBinding(HotkeyCommand Command, string KeyName, uint VirtualKey);

internal static class VirtualKeyParser
{
    private static readonly Dictionary<string, uint> KeyMap = CreateKeyMap();

    public static bool TryParse(string keyName, out uint virtualKey) =>
        KeyMap.TryGetValue(Normalize(keyName), out virtualKey);

    private static Dictionary<string, uint> CreateKeyMap()
    {
        var map = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            ["TAB"] = 0x09,
            ["ENTER"] = 0x0D,
            ["RETURN"] = 0x0D,
            ["SHIFT"] = 0x10,
            ["CTRL"] = 0x11,
            ["CONTROL"] = 0x11,
            ["ALT"] = 0x12,
            ["SPACE"] = 0x20,
            ["ESC"] = 0x1B,
            ["ESCAPE"] = 0x1B,
            ["LEFT"] = 0x25,
            ["UP"] = 0x26,
            ["RIGHT"] = 0x27,
            ["DOWN"] = 0x28,
            ["INSERT"] = 0x2D,
            ["DELETE"] = 0x2E,
            ["HOME"] = 0x24,
            ["END"] = 0x23,
            ["PAGEUP"] = 0x21,
            ["PAGEDOWN"] = 0x22,
        };

        for (var i = 0; i <= 9; i++)
        {
            map[i.ToString()] = (uint)(0x30 + i);
        }

        for (var c = 'A'; c <= 'Z'; c++)
        {
            map[c.ToString()] = c;
        }

        for (var i = 1; i <= 24; i++)
        {
            map[$"F{i}"] = (uint)(0x70 + i - 1);
        }

        return map;
    }

    private static string Normalize(string keyName) =>
        keyName.Trim().Replace(" ", string.Empty).ToUpperInvariant();
}
