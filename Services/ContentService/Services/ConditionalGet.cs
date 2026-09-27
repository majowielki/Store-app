using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Store.ContentService.Models;

namespace Store.ContentService.Services;

/// <summary>
/// Weak ETags on the public reads. Content changes rarely and is read on every page of the shop,
/// so the browser keeps its copy and asks with If-None-Match; an unchanged answer is a bodiless
/// 304. The tag is the latest change time plus the number of entries, so an edit, an addition,
/// a removal or a publish all produce a new one.
/// </summary>
public static class ConditionalGet
{
    public static ActionResult<T> OkUnlessUnchanged<T>(this ControllerBase controller, T body, IReadOnlyCollection<ContentEntry> entries)
    {
        var latest = entries.Count == 0 ? 0 : entries.Max(e => e.UpdatedAt.Ticks);
        var tag = new EntityTagHeaderValue($"\"{latest:x}-{entries.Count}\"", isWeak: true);

        var response = controller.Response.GetTypedHeaders();
        response.ETag = tag;
        // Stored, but checked with the server before every use
        response.CacheControl = new CacheControlHeaderValue { NoCache = true };

        var ifNoneMatch = controller.Request.GetTypedHeaders().IfNoneMatch;
        if (ifNoneMatch.Any(candidate => candidate.Compare(tag, useStrongComparison: false)))
        {
            return controller.StatusCode(StatusCodes.Status304NotModified);
        }

        return controller.Ok(body);
    }
}
