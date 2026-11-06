using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ModApi.Api.Configuration;

/// <summary>Writes a compact JSON health report suitable for load balancers and dashboards.</summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Serializes <paramref name="report"/> as <c>{ status, totalDurationMs, checks: [...] }</c>.</summary>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                description = entry.Value.Description,
            }),
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
