using JetFlow.UI.Handlers;
using JetFlow.UI.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Text;

namespace JetFlow.UI.Extensions;

public static class EndpointRouteBuilderExtension
{
    public static IEndpointRouteBuilder RegisterJetflowUIEndpoints(this IEndpointRouteBuilder routeBuilder)
        => routeBuilder
            .RegisterWebFiles()
            .RegisterDashBoardStreams();

    private static IEndpointRouteBuilder RegisterWebFiles(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/jetflow", async () =>
        {
            var asm = typeof(EndpointRouteBuilderExtension).Assembly;
            await using var stream = asm.GetManifestResourceStream("JetFlow.UI.WebFiles.index.html");
            if (stream == null)
                return Results.NotFound();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            return Results.Content(content, "text/html; charset=utf-8");
        });
        return routeBuilder;
    }

    private static IEndpointRouteBuilder RegisterDashBoardStreams(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapGet("/jetflow/dashboard", async (string? ns, [FromServices] IActiveFlowService activeFlowService, CancellationToken cancellation) =>
            {
                return TypedResults.ServerSentEvents(new DashboardEventStream(ns??string.Empty, activeFlowService));
            });
        return routeBuilder;
    }
}
