using JetFlow.UI.Handlers;
using JetFlow.UI.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System.Text;

namespace JetFlow.UI.Extensions;

public static class EndpointRouteBuilderExtension
{
    public static IEndpointRouteBuilder RegisterJetflowUIEndpoints(this IEndpointRouteBuilder routeBuilder)
        => routeBuilder
            .RegisterWebFiles()
            .RegisterDashBoardStreams()
            .RegisterNamespaceEndpoints();

    private static IFileInfo LocateFile(IFileProvider fileProvider, string path)
    {
        var fileInfo = fileProvider.GetFileInfo($"/jetflow/{path}");
        if (fileInfo.Exists)
            return fileInfo;
        return fileProvider.GetFileInfo($"/_content/JetFlow.UI/jetflow/{path}");
    }

    private static IFileInfo LocateFile(IWebHostEnvironment webHostEnvironment, string path)
    {
        var fileInfo = LocateFile(webHostEnvironment.WebRootFileProvider, path);
        if (!fileInfo.Exists)
            fileInfo = LocateFile(webHostEnvironment.ContentRootFileProvider, path);
        return fileInfo;
    }

    private static IEndpointRouteBuilder RegisterWebFiles(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/jetflow", async ([FromServices] IWebHostEnvironment webHostEnvironment) =>
        {
            var fileInfo = LocateFile(webHostEnvironment, "index.html");
            if (!fileInfo.Exists)
                return Results.NotFound();
            using var reader = new StreamReader(fileInfo.CreateReadStream(), Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            return Results.Content(content, "text/html; charset=utf-8");
        });
        routeBuilder.MapGet("/jetflow/resources/{fileName}", async (string fileName, [FromServices] IWebHostEnvironment webHostEnvironment) =>
        {
            var fileInfo = LocateFile(webHostEnvironment, $"resources/{fileName}");
            if (!fileInfo.Exists)
                return Results.NotFound();
            var provider = new FileExtensionContentTypeProvider();

            // 1. Detect content type from a file name or extension
            if (!provider.TryGetContentType(fileName, out var contentType))
                contentType = "application/octet-stream";
            else
                contentType+="; charset=utf-8";
            using var reader = new StreamReader(fileInfo.CreateReadStream(), Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            return Results.Content(content, contentType);
        });
        return routeBuilder;
    }

    private static IEndpointRouteBuilder RegisterDashBoardStreams(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapGet("/jetflow/dashboard", async (string? ns, [FromServices] IActiveFlowService activeFlowService, [FromServices] IDbConnection dbConnection, CancellationToken cancellation) =>
            {
                return TypedResults.ServerSentEvents(new DashboardEventStream(ns??string.Empty, activeFlowService, dbConnection));
            });
        return routeBuilder;
    }
    private static IEndpointRouteBuilder RegisterNamespaceEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapGet("/jetflow/namespaces", async (string? ns, [FromServices] IConfigService configService, CancellationToken cancellation) =>
            {
                return await configService.GetCurrentNamespacesAsync();
            });
        routeBuilder
            .MapPost("/jetflow/namespaces", async ([FromBody] string? ns, [FromServices] IConfigService configService, CancellationToken cancellation) =>
            {
                await configService.RegisterNamespaceAsync(ns);
                return;
            });
        routeBuilder
            .MapDelete("/jetflow/namespaces/{ns}", async (string? ns, [FromServices] IConfigService configService, CancellationToken cancellation) =>
            {
                await configService.UnregisterNamespaceAsync(ns);
                return;
            });
        return routeBuilder;
    }
}
