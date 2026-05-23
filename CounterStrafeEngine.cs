namespace CounterStrafe;

internal enum MovementKey
{
    W,
    A,
    S,
    D,
}

internal sealed record CounterStrafeAction(MovementKey KeyToTap);

internal sealed class CounterStrafeEngine
{
    private readonly Dictionary<MovementKey, long> _heldOrder = new();
    private long _nextOrder;

    public CounterStrafeAction? HandleKeyDown(MovementKey key)
    {
        if (_heldOrder.ContainsKey(key))
        {
            return null;
        }

        _heldOrder[key] = ++_nextOrder;
        return null;
    }

    public CounterStrafeAction? HandleKeyUp(MovementKey key)
    {
        if (!_heldOrder.ContainsKey(key))
        {
            return null;
        }

        var axis = MovementKeyHelper.GetAxis(key);
        var effectiveBeforeRelease = GetEffectiveKey(axis);

        _heldOrder.Remove(key);

        var effectiveAfterRelease = GetEffectiveKey(axis);
        if (effectiveBeforeRelease == key && effectiveAfterRelease is null)
        {
            return new CounterStrafeAction(GetOpposite(key));
        }

        return null;
    }

    public MovementKey? GetEffectiveKey(MovementAxis axis)
    {
        MovementKey? winner = null;
        long latestOrder = long.MinValue;

        foreach (var entry in _heldOrder)
        {
            if (MovementKeyHelper.GetAxis(entry.Key) != axis)
            {
                continue;
            }

            if (entry.Value > latestOrder)
            {
                latestOrder = entry.Value;
                winner = entry.Key;
            }
        }

        return winner;
    }

    private static MovementKey GetOpposite(MovementKey key) =>
        key switch
        {
            MovementKey.W => MovementKey.S,
            MovementKey.A => MovementKey.D,
            MovementKey.S => MovementKey.W,
            MovementKey.D => MovementKey.A,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null),
        };
}

internal static class MovementKeyHelper
{
    public static MovementAxis GetAxis(MovementKey key) =>
        key is MovementKey.A or MovementKey.D ? MovementAxis.Horizontal : MovementAxis.Vertical;
}

internal enum MovementAxis
{
    Horizontal,
    Vertical,
}
