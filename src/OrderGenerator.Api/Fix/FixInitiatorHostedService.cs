using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;
using QuickFix.Transport;

namespace OrderGenerator.Api.Fix;

public sealed class FixInitiatorHostedService : IHostedService
{
    private readonly FixGateway _gateway;
    private readonly ILogger<FixInitiatorHostedService> _logger;
    private readonly string _configPath;
    private SocketInitiator? _initiator;

    public FixInitiatorHostedService(
        FixGateway gateway,
        ILogger<FixInitiatorHostedService> logger,
        string configPath)
    {
        _gateway = gateway;
        _logger = logger;
        _configPath = configPath;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando initiator FIX com config {Config}", _configPath);

        var settings = new SessionSettings(_configPath);
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new FileLogFactory(settings);

        _initiator = new SocketInitiator(_gateway, storeFactory, settings, logFactory);
        _initiator.Start();

        _logger.LogInformation("Initiator FIX iniciado. Conectando ao OrderAccumulator.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parando initiator FIX.");
        _initiator?.Stop();
        _initiator?.Dispose();
        return Task.CompletedTask;
    }
}
