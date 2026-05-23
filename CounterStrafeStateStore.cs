using System.Text.Json;

namespace CounterStrafe;

internal sealed class CounterStrafeStateStore
{
    private const string FileName = "counterstrafe.state.json";

    public int LoadStrengthOrDefault(int fallbackStrength)
    {
        foreach (var configDirectory in AppPaths.EnumerateCandidateConfigDirectories())
        {
            var path = Path.Combine(configDirectory, FileName);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<CounterStrafeState>(json);
                if (state is not null)
                {
                    return state.CurrentStrengthMilliseconds;
                }
            }
            catch
            {
                return fallbackStrength;
            }
        }

        return fallbackStrength;
    }

    public void SaveStrength(int strengthMilliseconds)
    {
        try
        {
            var configDirectory = AppPaths.EnsurePreferredConfigDirectory();
            var path = Path.Combine(configDirectory, FileName);
            var json = JsonSerializer.Serialize(
                new CounterStrafeState(strengthMilliseconds),
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch
        {
        }
    }
}

internal sealed record CounterStrafeState(int CurrentStrengthMilliseconds);
