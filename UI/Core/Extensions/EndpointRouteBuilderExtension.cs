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
using System.Text.Json;

namespace JetFlow.UI.Extensions;

/// <summary>
/// Extension methods for mapping JetFlow UI endpoints to an <see cref="IEndpointRouteBuilder"/>.
/// </summary>
public static class EndpointRouteBuilderExtension
{
    internal static IEndpointConventionBuilder MapQuery(
        this IEndpointRouteBuilder endpoints, string pattern, Delegate handler)
    {
        return endpoints.MapMethods(pattern, new[] { "QUERY" }, handler);
    }

    /// <summary>
    /// Registers the JetFlow UI endpoints (static web files, dashboard streams and namespace APIs) on the provided route builder.
    /// </summary>
    /// <param name="routeBuilder">The route builder to register endpoints on.</param>
    /// <returns>The same <see cref="IEndpointRouteBuilder"/> for chaining.</returns>
    public static IEndpointRouteBuilder RegisterJetflowUIEndpoints(this IEndpointRouteBuilder routeBuilder, JetflowUIConfig? config = null)
        => routeBuilder
            .RegisterConfigCall(config??new())
            .RegisterWebFiles()
            .RegisterPerformanceEndpoints()
            .RegisterNamespaceEndpoints()
            .RegisterQueryEndpoints()
            .RegisterScheduledEndpoints();

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

    private static IEndpointRouteBuilder RegisterConfigCall(this IEndpointRouteBuilder routeBuilder, JetflowUIConfig config)
    {
        routeBuilder.MapGet("/jetflow/config", async () =>
        {
            return Results.Ok(config);
        });
        return routeBuilder;
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

    private static IEndpointRouteBuilder RegisterPerformanceEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapGet("/jetflow/dashboard", async (string? ns, [FromServices] IActiveFlowService activeFlowService, [FromServices] IDbConnection dbConnection, CancellationToken cancellation) =>
            {
                return TypedResults.ServerSentEvents(new DashboardEventStream(ns??string.Empty, activeFlowService, dbConnection));
            });
        routeBuilder
            .MapGet("/jetflow/performance/workflow", async ([FromQuery] string? ns, [FromQuery] string? workflowName, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime, [FromServices] IDbConnection dbConnection) => 
            {
                if (workflowName is null)
                    return Results.Ok(await dbConnection.ExtractWorkflowPerformanceDataAsync(ns, startTime, endTime));
                return Results.Ok(await dbConnection.ExtractWorkflowPerformanceDataAsync(ns, workflowName, startTime, endTime));
            });
        routeBuilder
            .MapGet("/jetflow/performance/activity", async ([FromQuery] string? ns, [FromQuery] string? activityName, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime, [FromServices] IDbConnection dbConnection) =>
            {
                if (activityName is null)
                    return Results.Ok(await dbConnection.ExtractActivityPerformanceDataAsync(ns, startTime, endTime));
                return Results.Ok(await dbConnection.ExtractActivityPerformanceDataAsync(ns, activityName, startTime, endTime));
            });
        return routeBuilder;
    }

    private static IEndpointRouteBuilder RegisterNamespaceEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapGet("/jetflow/namespaces", async ([FromServices] IConfigService configService, CancellationToken cancellation) =>
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

    private static IEndpointRouteBuilder RegisterQueryEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapQuery("/jetflow/query/activeflows", async ([FromBody] WorkflowQuery query, [FromServices] IActiveFlowService activeFlowService, CancellationToken cancellation) =>
            {
                if (query is null)
                    return Results.BadRequest("Query parameter is required.");
                var result = await activeFlowService.QueryActiveFlowsAsync(
                    query.WorkflowNamespace, 
                    query.WorkflowName, 
                    query.PageSize,
                    query.Offset,
                    (metaData) => CheckMetaData(metaData, query.MetaData),
                    (Func<JsonElement, bool>?)(!query.Argument.HasValue ? null : (arg) => CheckArgument(arg, query.Argument.Value))
                );
                return Results.Ok(new WorkflowQueryResponse(result, result.LastOffset));
            });
        return routeBuilder;
    }

    private static IEndpointRouteBuilder RegisterScheduledEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .MapGet("/jetflow/scheduledflows", async ([FromQuery]string ? ns, [FromServices] IActiveFlowService activeFlowService, CancellationToken cancellation) =>
            {
                var result = await activeFlowService.ListScheduledWorkflowsAsync(ns);
                return Results.Ok(result);
            });
        return routeBuilder;
    }

    private static bool CheckMetaData(Dictionary<string, string[]>? metaData, Dictionary<string, string?>? queryMetadata)
    {
        if (queryMetadata is null)
            return true;
        if (metaData is null)
            return false;
        foreach (var kvp in queryMetadata)
        {
            if (!metaData.TryGetValue(kvp.Key, out var values))
                return false;
            if (!values.Contains(kvp.Value))
                return false;
        }
        return true;
    }

    private static bool CheckArgument(JsonElement argument, JsonElement queryArgument)
    {
        if (argument.ValueKind != queryArgument.ValueKind)
            return false;
        return argument.ValueKind switch
        {
            JsonValueKind.String => argument.GetString() == queryArgument.GetString(),
            JsonValueKind.Number => argument.ToString() == queryArgument.ToString(),
            JsonValueKind.True => argument.GetBoolean() == queryArgument.GetBoolean(),
            JsonValueKind.False => argument.GetBoolean() == queryArgument.GetBoolean(),
            JsonValueKind.Array =>
                argument.EnumerateArray().Zip(queryArgument.EnumerateArray()).All(pair => CheckArgument(pair.First, pair.Second)),
            JsonValueKind.Object =>
                argument.EnumerateObject().All(prop => queryArgument.TryGetProperty(prop.Name, out var queryProp) ? CheckArgument(prop.Value, queryProp) : true),
            JsonValueKind.Null => true,
            JsonValueKind.Undefined => true,
            _ => false
        };
    }
}
