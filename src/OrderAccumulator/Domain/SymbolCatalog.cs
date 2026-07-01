namespace OrderAccumulator.Domain;

public static class SymbolCatalog
{
    public static readonly IReadOnlySet<string> Allowed =
        new HashSet<string>(StringComparer.Ordinal) { "PETR4", "VALE3", "VIIA4" };

    public static bool IsSupported(string symbol) => Allowed.Contains(symbol);
}
