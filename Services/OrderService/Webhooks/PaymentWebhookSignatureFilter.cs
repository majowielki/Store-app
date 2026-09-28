using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using Store.BuildingBlocks.Webhooks;
using System.Text;

namespace Store.OrderService.Webhooks;

/// <summary>
/// Lets a payment webhook reach its action only with a valid signature over the body exactly as
/// it was sent (<see cref="WebhookSignature"/>); a missing, wrong or stale one is answered 401.
/// It runs before model binding: the raw body is read, checked and rewound for the binder.
/// </summary>
public sealed class PaymentWebhookSignatureFilter : IAsyncResourceFilter
{
    private readonly PaymentWebhookOptions _options;
    private readonly TimeProvider _time;
    private readonly ProblemDetailsFactory _problems;
    private readonly ILogger<PaymentWebhookSignatureFilter> _logger;

    public PaymentWebhookSignatureFilter(
        IOptions<PaymentWebhookOptions> options,
        TimeProvider time,
        ProblemDetailsFactory problems,
        ILogger<PaymentWebhookSignatureFilter> logger)
    {
        _options = options.Value;
        _time = time;
        _problems = problems;
        _logger = logger;
    }

    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(context.HttpContext.RequestAborted);
        }

        request.Body.Position = 0;

        var check = WebhookSignature.Verify(request.Headers[WebhookSignature.HeaderName], body, _options.SigningSecret, _time.GetUtcNow(), _options.Tolerance);
        if (check != WebhookSignatureCheck.Valid)
        {
            _logger.LogWarning("Payment webhook refused: signature {Check}", check);
            var problem = _problems.CreateProblemDetails(context.HttpContext, StatusCodes.Status401Unauthorized,
                detail: "The webhook signature is missing, stale or wrong.");
            context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        await next();
    }
}
