using Microsoft.Extensions.Logging;
using OrderAccumulator.Domain;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using Message = QuickFix.Message;

namespace OrderAccumulator.Fix;

public sealed class AcceptorApplication : MessageCracker, IApplication
{
    private readonly ExposureService _exposure;
    private readonly ILogger<AcceptorApplication> _logger;
    private long _execIdSequence;
    private long _orderIdSequence;

    public AcceptorApplication(ExposureService exposure, ILogger<AcceptorApplication> logger)
    {
        _exposure = exposure;
        _logger = logger;
    }

    public void OnCreate(SessionID sessionID) => _logger.LogInformation("Sessão criada: {Session}", sessionID);
    public void OnLogon(SessionID sessionID) => _logger.LogInformation("Logon: {Session}", sessionID);
    public void OnLogout(SessionID sessionID) => _logger.LogInformation("Logout: {Session}", sessionID);
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
            _logger.LogError(ex, "Falha ao processar mensagem da sessão {Session}", sessionID);
            throw;
        }
    }

    public void OnMessage(NewOrderSingle order, SessionID sessionID)
    {
        var clOrdId = order.ClOrdID.Value;
        var symbol = order.Symbol.Value;
        var side = order.Side.Value == Side.BUY ? OrderSide.Buy : OrderSide.Sell;
        var quantity = (long)order.OrderQty.Value;
        var price = order.Price.Value;

        if (!SymbolCatalog.IsSupported(symbol))
        {
            _logger.LogWarning("Ordem {ClOrdId} rejeitada: símbolo não suportado {Symbol}", clOrdId, symbol);
            SendRejection(sessionID, order, OrdRejReason.UNKNOWN_SYMBOL, $"Símbolo não suportado: {symbol}",
                exposureAfter: 0m, positionAfter: 0L);
            return;
        }

        var result = _exposure.Evaluate(symbol, side, price, quantity);

        if (result.Accepted)
        {
            _logger.LogInformation(
                "ACEITA {ClOrdId} {Side} {Qty}x{Symbol}@{Price} | exposição {Symbol}={Exposure:N2} posição={Position}",
                clOrdId, side, quantity, symbol, price, symbol, result.ExposureAfter, result.PositionAfter);
            SendAcceptance(sessionID, order, result.ExposureAfter, result.PositionAfter);
        }
        else
        {
            _logger.LogWarning(
                "REJEITADA {ClOrdId} {Side} {Qty}x{Symbol}@{Price} | exposição projetada {Projected:N2} excede limite {Limit:N2}",
                clOrdId, side, quantity, symbol, price, result.ProjectedExposure, ExposureService.LimitPerSymbol);
            SendRejection(sessionID, order, OrdRejReason.ORDER_EXCEEDS_LIMIT,
                $"Exposição projetada {result.ProjectedExposure:N2} excede o limite de {ExposureService.LimitPerSymbol:N2}",
                result.ExposureAfter, result.PositionAfter);
        }
    }

    private void SendAcceptance(SessionID sessionID, NewOrderSingle order, decimal exposureAfter, long positionAfter)
    {
        var report = CreateReport(order, ExecType.NEW, OrdStatus.NEW, exposureAfter, positionAfter);
        report.Set(new LeavesQty(order.OrderQty.Value));
        Session.SendToTarget(report, sessionID);
    }

    private void SendRejection(SessionID sessionID, NewOrderSingle order, int rejectReason, string text,
        decimal exposureAfter, long positionAfter)
    {
        var report = CreateReport(order, ExecType.REJECTED, OrdStatus.REJECTED, exposureAfter, positionAfter);
        report.Set(new LeavesQty(0m));
        report.Set(new OrdRejReason(rejectReason));
        report.Set(new Text(text));
        Session.SendToTarget(report, sessionID);
    }

    private ExecutionReport CreateReport(NewOrderSingle order, char execType, char ordStatus,
        decimal exposureAfter, long positionAfter)
    {
        var report = new ExecutionReport();
        report.Set(new OrderID(NextOrderId()));
        report.Set(new ExecID(NextExecId()));
        report.Set(new ExecType(execType));
        report.Set(new OrdStatus(ordStatus));
        report.Set(order.ClOrdID);
        report.Set(order.Symbol);
        report.Set(order.Side);
        report.Set(new OrderQty(order.OrderQty.Value));
        report.Set(order.Price);
        report.Set(new CumQty(0m));
        report.Set(new AvgPx(0m));
        report.SetField(new DecimalField(FixCustomTags.SymbolExposure, exposureAfter));
        report.SetField(new DecimalField(FixCustomTags.SymbolPosition, positionAfter));
        return report;
    }

    private string NextExecId() => $"EX-{Interlocked.Increment(ref _execIdSequence):D9}";
    private string NextOrderId() => $"OA-{Interlocked.Increment(ref _orderIdSequence):D9}";
}
