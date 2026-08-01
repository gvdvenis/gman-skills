using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ReportServer.Endpoints;

/// <summary>Maps the domain (report) endpoints for the local report server.</summary>
internal static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/report", GetReportAsync)
            .WithName("GetReport")
            .WithSummary("Get the raw improvement report JSON.")
            .WithDescription("Returns the on-disk report document as a JSON pass-through.");

        app.MapPost("/api/dismissals", DismissFindingAsync)
            .WithName("DismissFinding")
            .WithSummary("Record a dismissal decision for a finding.")
            .WithDescription("Persists a 'dismissed' decision for the finding with the given id. Idempotent: re-dismissing an already-dismissed finding returns the original decided_at.")
            .Produces<DismissalResponse>(200)
            .ProducesProblem(404)
            .ProducesProblem(400);

        app.MapPost("/api/ship-prompt", ShipPromptAsync)
            .WithName("ShipPrompt")
            .WithSummary("Compress and persist the assembled prompt.")
            .WithDescription("Compresses the readable prompt, persists it as shipped_prompt, queues the referenced finding ids, and writes the compressed form to the terminal clipboard escape sequence.")
            .Produces<ShipPromptResponse>(200)
            .ProducesProblem(400);

        return app;
    }

    private static async Task<IResult> GetReportAsync(
        ReportStore reportStore,
        HttpContext context)
    {
        var json = await reportStore.ReadRawAsync(context.RequestAborted);
        return Results.Content(json, "application/json");
    }

    private static async Task<Microsoft.AspNetCore.Http.HttpResults.Results<Ok<DismissalResponse>, NotFound, BadRequest>> DismissFindingAsync(
        DismissalRequest request,
        ReportStore reportStore,
        HttpContext context)
    {
        var result = await reportStore.DismissAsync(request, context.RequestAborted);
        return TypedResults.Ok(new DismissalResponse(request.Id, result.DecidedAt));
    }

    private static async Task<Microsoft.AspNetCore.Http.HttpResults.Results<Ok<ShipPromptResponse>, BadRequest>> ShipPromptAsync(
        ShipPromptRequest request,
        ReportStore reportStore,
        HttpContext context)
    {
        var transformed = PromptCompressor.Compress(request.Prompt);
        await reportStore.ShipPromptAsync(request, transformed, context.RequestAborted);
        var clipboard = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(transformed));
        Console.Write($"\x1b]52;c;{clipboard}\x07");
        return TypedResults.Ok(new ShipPromptResponse(transformed, []));
    }
}
