using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderAccumulator.Domain;
using OrderAccumulator.Fix;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o =>
{
    o.SingleLine = true;
    o.TimestampFormat = "HH:mm:ss ";
});

var configPath = Path.Combine(AppContext.BaseDirectory, "config", "acceptor.cfg");

builder.Services.AddSingleton<ExposureService>();
builder.Services.AddSingleton<AcceptorApplication>();
builder.Services.AddSingleton<IHostedService>(sp => new FixAcceptorHostedService(
    sp.GetRequiredService<AcceptorApplication>(),
    sp.GetRequiredService<ILogger<FixAcceptorHostedService>>(),
    configPath));

var host = builder.Build();

host.Services.GetRequiredService<ILogger<Program>>()
    .LogInformation("OrderAccumulator — limite por símbolo: R$ {Limit:N2}", ExposureService.LimitPerSymbol);

await host.RunAsync();

public partial class Program;
