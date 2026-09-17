namespace Store.GatewayService.Cors;

/// <summary>
/// Origins allowed to call the API from a browser (<c>Cors:AllowedOrigins</c>). Empty in the
/// normal deployment: the UI's nginx (or the Vite dev server) proxies /api from the UI's own
/// origin, so the browser never makes a cross-origin request and no CORS headers are sent.
/// Set it only for a UI served from elsewhere - and note that the refresh cookie is
/// SameSite=Strict, so sessions only work same-origin anyway.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}
