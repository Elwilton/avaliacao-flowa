using System.Collections.Concurrent;

namespace OrderAccumulator.Domain;

public enum OrderSide
{
    Buy,
    Sell
}

public readonly record struct ExposureResult(
    bool Accepted,
    decimal ExposureAfter,
    decimal ProjectedExposure,
    long PositionAfter);

public sealed class ExposureService
{
    public const decimal LimitPerSymbol = 100_000_000m;

    private readonly ConcurrentDictionary<string, decimal> _exposureBySymbol = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> _positionBySymbol = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, object> _locksBySymbol = new(StringComparer.Ordinal);

    public ExposureResult Evaluate(string symbol, OrderSide side, decimal price, long quantity)
    {
        var signedFinancial = price * quantity * (side == OrderSide.Buy ? 1 : -1);
        var signedQuantity = side == OrderSide.Buy ? quantity : -quantity;
        var symbolLock = _locksBySymbol.GetOrAdd(symbol, static _ => new object());

        lock (symbolLock)
        {
            var currentExposure = _exposureBySymbol.GetValueOrDefault(symbol, 0m);
            var currentPosition = _positionBySymbol.GetValueOrDefault(symbol, 0L);
            var projectedExposure = currentExposure + signedFinancial;

            if (Math.Abs(projectedExposure) > LimitPerSymbol)
            {
                return new ExposureResult(false, currentExposure, projectedExposure, currentPosition);
            }

            var newPosition = currentPosition + signedQuantity;
            _exposureBySymbol[symbol] = projectedExposure;
            _positionBySymbol[symbol] = newPosition;
            return new ExposureResult(true, projectedExposure, projectedExposure, newPosition);
        }
    }

    public decimal GetExposure(string symbol) => _exposureBySymbol.GetValueOrDefault(symbol, 0m);

    public long GetPosition(string symbol) => _positionBySymbol.GetValueOrDefault(symbol, 0L);
}
