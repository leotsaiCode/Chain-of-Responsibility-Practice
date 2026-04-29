
var paymentProcessor = new PaymentProcessor();
var result = await paymentProcessor.ProcessPaymentAsync(new PaymentRequest
{
    MemberId = "M001",
    RequestId = "REQ-001",
    Amount = 500
});
Console.WriteLine(result.IsSuccess
    ? $"付款成功: {result.Message}"
    : $"付款失敗: {result.Message}");

public class PaymentRequest
{
    public string MemberId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
public class PaymentResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public static PaymentResult Success(string message)
    {
        return new PaymentResult
        {
            IsSuccess = true,
            Message = message
        };
    }

    public static PaymentResult Fail(string message)
    {
        return new PaymentResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}

public interface IPaymentHandler
{
    IPaymentHandler SetNext(IPaymentHandler next);
    Task<PaymentResult> HandleAsync(PaymentRequest request);
}

public abstract class PaymentHandlerBase : IPaymentHandler
{
    private IPaymentHandler? _next;

    public IPaymentHandler SetNext(IPaymentHandler next)
    {
        _next = next;
        return next;
    }

    public async Task<PaymentResult> HandleAsync(PaymentRequest request)
    {
        var result = await ProcessAsync(request);

        if (!result.IsSuccess)
            return result;

        if (_next is null)
            return PaymentResult.Success("所有檢查通過");

        return await _next.HandleAsync(request);
    }

    protected abstract Task<PaymentResult> ProcessAsync(PaymentRequest request);
}

public sealed class MemberExistsHandler : PaymentHandlerBase
{
    protected sealed override Task<PaymentResult> ProcessAsync(PaymentRequest request)
    {

        if (string.IsNullOrWhiteSpace(request.MemberId))
            return Task.FromResult(PaymentResult.Fail("會員不存在"));

        Console.WriteLine("會員檢查通過");
        return Task.FromResult(PaymentResult.Success("會員檢查通過"));
    }
}

public sealed class AmountValidHandler : PaymentHandlerBase
{
    protected sealed override Task<PaymentResult> ProcessAsync(PaymentRequest request)
    {
        if (request.Amount <= 0)
            return Task.FromResult(PaymentResult.Fail("付款金額必須大於 0"));

        Console.WriteLine("金額檢查通過");
        return Task.FromResult(PaymentResult.Success("金額檢查通過"));
    }
}

public class PaymentProcessor
{
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        var memberHandler = new MemberExistsHandler();
        var amountHandler = new AmountValidHandler();

        memberHandler
            .SetNext(amountHandler);

        return await memberHandler.HandleAsync(request);
    }
}
