namespace OrderGenerator.Api.Orders;

public static class OrderValidator
{
    public static readonly IReadOnlySet<string> AllowedSymbols =
        new HashSet<string>(StringComparer.Ordinal) { "PETR4", "VALE3", "VIIA4" };

    public const int MaxQuantityExclusive = 100_000;
    public const decimal MaxPriceExclusive = 1_000m;
    public const decimal PriceTick = 0.01m;
    public const decimal ExposureLimitPerSymbol = 100_000_000m;

    public static bool TryValidate(OrderRequest request, out OrderSide side, out IReadOnlyList<string> errors)
    {
        var problems = new List<string>();
        side = default;

        var symbol = request.Symbol?.Trim() ?? string.Empty;
        if (!AllowedSymbols.Contains(symbol))
        {
            problems.Add($"Símbolo inválido. Use um de: {string.Join(", ", AllowedSymbols)}.");
        }

        if (!TryParseSide(request.Side, out side))
        {
            problems.Add("Lado inválido. Use 'Buy'/'Compra' ou 'Sell'/'Venda'.");
        }

        if (request.Quantity <= 0 || request.Quantity >= MaxQuantityExclusive)
        {
            problems.Add($"Quantidade deve ser um inteiro positivo menor que {MaxQuantityExclusive:N0}.");
        }

        if (request.Price <= 0m || request.Price >= MaxPriceExclusive)
        {
            problems.Add($"Preço deve ser positivo e menor que {MaxPriceExclusive:N2}.");
        }
        else if (!IsMultipleOfTick(request.Price))
        {
            problems.Add("Preço deve ser múltiplo de 0,01.");
        }

        errors = problems;
        return problems.Count == 0;
    }

    public static bool TryParseSide(string? value, out OrderSide side)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "buy":
            case "compra":
                side = OrderSide.Buy;
                return true;
            case "sell":
            case "venda":
                side = OrderSide.Sell;
                return true;
            default:
                side = default;
                return false;
        }
    }

    private static bool IsMultipleOfTick(decimal price)
    {
        var cents = price * 100m;
        return cents == decimal.Truncate(cents);
    }
}
