using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuickFix;
using QuickFix.Logger;
using QuickFix.Store;

namespace OrderAccumulator.Fix;

public sealed class FixAcceptorHostedService : IHostedService
{
    private readonly AcceptorApplication _application;
    private readonly ILogger<FixAcceptorHostedService> _logger;
    private readonly string _configPath;
    private ThreadedSocketAcceptor? _acceptor;

    public FixAcceptorHostedService(
        AcceptorApplication application,
        ILogger<FixAcceptorHostedService> logger,
        string configPath)
    {
        _application = application;
        _logger = logger;
        _configPath = configPath;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando acceptor FIX com config {Config}", _configPath);

        var settings = new SessionSettings(_configPath);
        ApplyEnvironmentOverrides(settings);
        var storeFactory = new FileStoreFactory(settings);
        var logFactory = new FileLogFactory(settings);

        _acceptor = new ThreadedSocketAcceptor(_application, storeFactory, settings, logFactory);
        _acceptor.Start();

        _logger.LogInformation("Acceptor FIX no ar. Aguardando conexões do OrderGenerator.");
        return Task.CompletedTask;
    }

    private static void ApplyEnvironmentOverrides(SessionSettings settings)
    {
        var host = Environment.GetEnvironmentVariable("FIX_ACCEPT_HOST");
        var port = Environment.GetEnvironmentVariable("FIX_ACCEPT_PORT");
        foreach (var sessionId in settings.GetSessions())
        {
            var session = settings.Get(sessionId);
            if (!string.IsNullOrWhiteSpace(host)) session.SetString("SocketAcceptHost", host);
            if (!string.IsNullOrWhiteSpace(port)) session.SetString("SocketAcceptPort", port);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parando acceptor FIX.");
        _acceptor?.Stop();
        _acceptor?.Dispose();
        return Task.CompletedTask;
    }
}
