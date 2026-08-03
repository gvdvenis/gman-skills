using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ReportServer.Endpoints;

/// <summary>Serves the embedded SPA shell (index.html) so the exe is fully self-contained.</summary>
internal static class UiEndpoints
{
    private const string HtmlResourceName = "ReportServer.wwwroot.index.html";
    private static readonly string HtmlContent = LoadEmbeddedHtml();

    public static IEndpointRouteBuilder MapEmbeddedHtml(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", ServeHtml).WithName("Index");
        app.MapGet("/index.html", ServeHtml).WithName("IndexHtml");
        return app;
    }

    private static IResult ServeHtml() =>
            Results.Content(HtmlContent, "text/html; charset=utf-8");

    private static string LoadEmbeddedHtml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(HtmlResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{HtmlResourceName}' not found. " +
                "Ensure wwwroot/index.html is configured as an EmbeddedResource.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}