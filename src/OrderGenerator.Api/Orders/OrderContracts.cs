namespace OrderGenerator.Api.Orders;

public enum OrderSide
{
    Buy,
    Sell
}

public sealed record OrderRequest
{
    public string Symbol { get; init; } = string.Empty;
    public string Side { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}

public enum OrderOutcome
{
    New,
    Rejected,
    Error
}

public sealed record OrderResult
{
    public required OrderOutcome Outcome { get; init; }
    public required string ClOrdId { get; init; }
    public string? Symbol { get; init; }
    public string? Side { get; init; }
    public int? Quantity { get; init; }
    public decimal? Price { get; init; }
    public string? ExecType { get; init; }
    public string? OrdStatus { get; init; }
    public string? OrderId { get; init; }
    public string? ExecId { get; init; }
    public string? Text { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
