using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ReportServer.Endpoints;

/// <summary>Maps infrastructure (system) endpoints for the local report server.</summary>
internal static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app, CancellationTokenSource shutdown)
    {
        app.MapGet("/ping", () => TypedResults.Ok(new ApiStatusResponse("ok")))
            .WithName("Ping")
            .WithSummary("Health-check endpoint.")
            .WithDescription("Returns a simple status payload so callers can verify the server is running.");

        app.Map("/shutdown", context => ShutdownAsync(context, shutdown))
            .WithName("Shutdown")
            .WithSummary("Gracefully shut down the server.")
            .WithDescription("Completes the response, then cancels the host shutdown token to stop the server.");

        return app;
    }

    private static async Task ShutdownAsync(HttpContext context, CancellationTokenSource shutdown)
    {
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new ShutdownResponse("shutting down"),
            ReportJsonContext.Default.ShutdownResponse,
            context.RequestAborted);
        await context.Response.CompleteAsync();
        shutdown.Cancel();
    }
}
