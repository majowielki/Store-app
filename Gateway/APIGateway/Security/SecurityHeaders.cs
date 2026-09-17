namespace Store.GatewayService.Security;

/// <summary>
/// Response headers for an API: nothing the gateway answers is a document, so browsers must
/// not sniff it into one, frame it, or run anything from it. The UI's own headers (its content
/// security policy) are set by the nginx that serves it.
/// </summary>
public static class SecurityHeaders
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.Use((context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            headers["Cache-Control"] = "no-store";
            return next(context);
        });
}
