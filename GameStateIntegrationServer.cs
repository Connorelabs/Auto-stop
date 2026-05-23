using System.Net;
using System.Text;
using System.Text.Json;

namespace CounterStrafe;

internal sealed class GameStateIntegrationServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly Action<ActiveWeaponInfo?> _onActiveWeaponChanged;
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _loopTask;
    private string? _lastWeaponKey;

    public GameStateIntegrationServer(string prefix, Action<ActiveWeaponInfo?> onActiveWeaponChanged)
    {
        _listener.Prefixes.Add(prefix);
        _onActiveWeaponChanged = onActiveWeaponChanged;
    }

    public void Start()
    {
        if (_listener.IsListening)
        {
            return;
        }

        _listener.Start();
        _loopTask = Task.Run(() => ListenAsync(_cancellation.Token));
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;

            try
            {
                context = await _listener.GetContextAsync();
                await HandleRequestAsync(context, cancellationToken);
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception)
            {
                if (context is not null)
                {
                    TryWriteStatus(context.Response, statusCode: 500);
                }
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
            var body = await reader.ReadToEndAsync(cancellationToken);
            var activeWeapon = GameStatePayloadParser.TryGetActiveWeapon(body);
            PublishIfChanged(activeWeapon);
            TryWriteStatus(context.Response, statusCode: 200);
        }
        catch (JsonException)
        {
            PublishIfChanged(null);
            TryWriteStatus(context.Response, statusCode: 400);
        }
    }

    private void PublishIfChanged(ActiveWeaponInfo? weapon)
    {
        var currentKey = weapon?.UniqueKey;
        if (string.Equals(currentKey, _lastWeaponKey, StringComparison.Ordinal))
        {
            return;
        }

        _lastWeaponKey = currentKey;
        _onActiveWeaponChanged(weapon);
    }

    private static void TryWriteStatus(HttpListenerResponse response, int statusCode)
    {
        try
        {
            response.StatusCode = statusCode;
            response.Close();
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();

        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        _cancellation.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal static class GameStatePayloadParser
{
    public static ActiveWeaponInfo? TryGetActiveWeapon(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("player", out var playerElement))
        {
            return null;
        }

        if (!playerElement.TryGetProperty("weapons", out var weaponsElement) || weaponsElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in weaponsElement.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var state = property.Value.TryGetProperty("state", out var stateElement) ? stateElement.GetString() : null;
            if (!string.Equals(state, "active", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = property.Value.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
            var type = property.Value.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
            return new ActiveWeaponInfo(name, type, state);
        }

        return null;
    }
}

internal sealed record ActiveWeaponInfo(string? Name, string? Type, string? State)
{
    public string UniqueKey => $"{Normalize(Name)}|{Normalize(Type)}|{Normalize(State)}";

    public string DisplayName => !string.IsNullOrWhiteSpace(Name) ? Name! : "unknown";

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}

internal static class WeaponPolicy
{
    public static bool ShouldSuppressCounterStrafe(ActiveWeaponInfo? weapon, WeaponSuppressConfig config)
    {
        if (weapon is null)
        {
            return false;
        }

        var type = Normalize(weapon.Type);
        if (config.Types.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var name = Normalize(weapon.Name);
        return config.Names.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    public static string Describe(ActiveWeaponInfo? weapon)
    {
        if (weapon is null)
        {
            return "unknown";
        }

        var type = string.IsNullOrWhiteSpace(weapon.Type) ? "unknown" : weapon.Type;
        return $"{weapon.DisplayName} [{type}]";
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
