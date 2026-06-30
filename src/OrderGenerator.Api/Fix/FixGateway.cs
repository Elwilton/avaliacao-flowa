using System.Collections.Concurrent;
using OrderGenerator.Api.Orders;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace OrderGenerator.Api.Fix;

public sealed class FixGateway : MessageCracker, IApplication
{
    private readonly ILogger<FixGateway> _logger;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<OrderResult>> _pendingOrders = new(StringComparer.Ordinal);
    private volatile SessionID? _sessionId;
    private long _clOrdIdSequence;

    public FixGateway(ILogger<FixGateway> logger) => _logger = logger;

    public bool IsReady => _sessionId is not null;

    public void OnCreate(SessionID sessionID) => _logger.LogInformation("Sessão criada: {Session}", sessionID);

    public void OnLogon(SessionID sessionID)
    {
        _sessionId = sessionID;
        _logger.LogInformation("Logon com o OrderAccumulator: {Session}", sessionID);
    }

    public void OnLogout(SessionID sessionID)
    {
        _sessionId = null;
        _logger.LogWarning("Logout: {Session}", sessionID);
    }

    public void ToAdmin(Message message, SessionID sessionID) { }
    public void FromAdmin(Message message, SessionID sessionID) { }
    public void ToApp(Message message, SessionID sessionID) { }

    public void FromApp(Message message, SessionID sessionID)
    {
        try
        {
            Crack(message, sessionID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem recebida");
        }
    }

    public async Task<OrderResult> SendNewOrderAsync(
        OrderRequest request, OrderSide side, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var sessionId = _sessionId;
        var clOrdId = NextClOrdId();

        if (sessionId is null)
        {
            _logger.LogWarning("Ordem {ClOrdId} não enviada: sessão FIX indisponível", clOrdId);
            return ErrorResult(clOrdId, request, side, "Sessão FIX indisponível: o OrderAccumulator está conectado?");
        }

        var completion = new TaskCompletionSource<OrderResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingOrders[clOrdId] = completion;

        try
        {
            var order = BuildNewOrderSingle(clOrdId, request.Symbol.Trim(), side, request.Quantity, request.Price);
            if (!Session.SendToTarget(order, sessionId))
            {
                throw new InvalidOperationException("Session.SendToTarget retornou false.");
            }

            _logger.LogInformation("Enviada ordem {ClOrdId}: {Side} {Qty}x{Symbol}@{Price}",
                clOrdId, side, request.Quantity, request.Symbol, request.Price);

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);

            await using (timeoutSource.Token.Register(static state => ((TaskCompletionSource<OrderResult>)state!).TrySetCanceled(), completion))
            {
                return await completion.Task.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout aguardando ExecutionReport da ordem {ClOrdId}", clOrdId);
            return ErrorResult(clOrdId, request, side, "Tempo limite excedido aguardando resposta do OrderAccumulator.");
        }
        finally
        {
            _pendingOrders.TryRemove(clOrdId, out _);
        }
    }

    public void OnMessage(ExecutionReport report, SessionID sessionID)
    {
        var clOrdId = report.IsSetClOrdID() ? report.ClOrdID.Value : string.Empty;
        var execType = report.ExecType.Value;

        var outcome = execType switch
        {
            ExecType.NEW => OrderOutcome.New,
            ExecType.REJECTED => OrderOutcome.Rejected,
            _ => OrderOutcome.Error
        };

        var result = new OrderResult
        {
            Outcome = outcome,
            ClOrdId = clOrdId,
            Symbol = report.IsSetSymbol() ? report.Symbol.Value : null,
            Side = report.IsSetSide() ? (report.Side.Value == Side.BUY ? "Buy" : "Sell") : null,
            Quantity = report.IsSetOrderQty() ? (int)report.OrderQty.Value : null,
            Price = report.IsSetPrice() ? report.Price.Value : null,
            ExecType = execType.ToString(),
            OrdStatus = report.IsSetOrdStatus() ? report.OrdStatus.Value.ToString() : null,
            OrderId = report.IsSetOrderID() ? report.OrderID.Value : null,
            ExecId = report.IsSetExecID() ? report.ExecID.Value : null,
            Text = report.IsSetText() ? report.Text.Value : null
        };

        _logger.LogInformation("ExecutionReport recebido para {ClOrdId}: {Outcome}", clOrdId, outcome);

        if (clOrdId.Length > 0 && _pendingOrders.TryRemove(clOrdId, out var completion))
        {
            completion.TrySetResult(result);
        }
        else
        {
            _logger.LogWarning("ExecutionReport sem requisição pendente correspondente (ClOrdID={ClOrdId})", clOrdId);
        }
    }

    private static OrderResult ErrorResult(string clOrdId, OrderRequest request, OrderSide side, string text) => new()
    {
        Outcome = OrderOutcome.Error,
        ClOrdId = clOrdId,
        Symbol = request.Symbol,
        Side = side.ToString(),
        Quantity = request.Quantity,
        Price = request.Price,
        Text = text
    };

    private static NewOrderSingle BuildNewOrderSingle(
        string clOrdId, string symbol, OrderSide side, int quantity, decimal price)
    {
        var order = new NewOrderSingle();
        order.Set(new ClOrdID(clOrdId));
        order.Set(new Symbol(symbol));
        order.Set(new Side(side == OrderSide.Buy ? Side.BUY : Side.SELL));
        order.Set(new TransactTime(DateTime.UtcNow));
        order.Set(new OrdType(OrdType.LIMIT));
        order.Set(new OrderQty(quantity));
        order.Set(new Price(price));
        order.Set(new HandlInst(HandlInst.AUTOMATED_EXECUTION_ORDER_PRIVATE_NO_BROKER_INTERVENTION));
        return order;
    }

    private string NextClOrdId() => $"G{DateTime.UtcNow:yyyyMMddHHmmss}-{Interlocked.Increment(ref _clOrdIdSequence):D6}";
}
