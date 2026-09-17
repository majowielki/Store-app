using OpenTelemetry;
using System.Diagnostics;

namespace Store.BuildingBlocks.Observability;

/// <summary>
/// Keeps database spans that start a trace of their own out of the export: the outbox
/// delivery and the inbox clean-up poll the database every second, and each poll would
/// otherwise be a one-span trace that buries the requests. A database call made while serving
/// a request or consuming a message has a parent and is exported with it. This is a processor
/// rather than a sampler because Npgsql tags its activity after starting it, which is too
/// late for a sampler to see what the span is.
/// </summary>
internal sealed class BackgroundDatabaseSpanFilter : BaseProcessor<Activity>
{
    private const string NpgsqlSource = "Npgsql";

    public override void OnEnd(Activity activity)
    {
        var startsATrace = activity.Parent is null && activity.ParentId is null;
        if (startsATrace && activity.Kind == ActivityKind.Client && activity.Source.Name == NpgsqlSource)
        {
            // Exporters skip what is not recorded
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            activity.IsAllDataRequested = false;
        }
    }
}
