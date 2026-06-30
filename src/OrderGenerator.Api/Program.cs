using OrderGenerator.Api.Fix;
using OrderGenerator.Api.Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});

const string FrontendCorsPolicy = "frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var fixConfigPath = Path.Combine(AppContext.BaseDirectory, "config", "initiator.cfg");
builder.Services.AddSingleton<FixGateway>();
builder.Services.AddSingleton<IHostedService>(sp => new FixInitiatorHostedService(
    sp.GetRequiredService<FixGateway>(),
    sp.GetRequiredService<ILogger<FixInitiatorHostedService>>(),
    fixConfigPath));

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors(FrontendCorsPolicy);

var responseTimeout = TimeSpan.FromSeconds(
    builder.Configuration.GetValue("Fix:ResponseTimeoutSeconds", 10));

app.MapGet("/api/meta", () => Results.Ok(new
{
    symbols = OrderValidator.AllowedSymbols,
    sides = new[] { "Buy", "Sell" },
    maxQuantityExclusive = OrderValidator.MaxQuantityExclusive,
    maxPriceExclusive = OrderValidator.MaxPriceExclusive,
    priceTick = OrderValidator.PriceTick
}));

app.MapGet("/api/health", (FixGateway gateway) => Results.Ok(new
{
    fixSessionReady = gateway.IsReady
}));

app.MapPost("/api/orders", async (OrderRequest request, FixGateway gateway, CancellationToken ct) =>
{
    if (!OrderValidator.TryValidate(request, out var side, out var errors))
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]> { ["order"] = errors.ToArray() },
            title: "Ordem inválida");
    }

    var result = await gateway.SendNewOrderAsync(request, side, responseTimeout, ct);

    return result.Outcome == OrderOutcome.Error
        ? Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable)
        : Results.Ok(result);
});

app.Run();

public partial class Program;
