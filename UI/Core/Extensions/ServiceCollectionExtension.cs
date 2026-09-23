using JetFlow.UI.Interfaces;
using JetFlow.UI.Json;
using JetFlow.UI.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NATS.Client.Core;
using NATS.Net;

namespace JetFlow.UI.Extensions;

public static class ServiceCollectionExtension
{
    public static async ValueTask<IServiceCollection> RegisterJetflowUIAsync(this IServiceCollection services,
        NatsOpts natsOpts,
        IDbConnection dbConnection)
    {
        var connection = new NatsConnection(natsOpts);
        if (connection.ConnectionState != NatsConnectionState.Open)
        {
            try
            {
                await connection.ConnectAsync();
            }
            catch
            {
                //burying connection errors
            }
        }
        if (connection.ConnectionState != NatsConnectionState.Open)
            throw new UnableToConnectException();
        var jsContext = connection.CreateJetStreamContext();
        await dbConnection.InitAsync();
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(connection));
        await observationConnection.AddArchivingListenerAsync(async archiveEvent =>
        {
            await dbConnection.StoreArchiveAsync(archiveEvent.Archive, archiveEvent.WorkflowNamespace);
            return true;
        });
        await observationConnection.AddPerformanceMonitoringAsync(1,
            async workflowEvent =>
            {
                await dbConnection.StoreWorkflowPerformanceRecordAsync(workflowEvent.namespaceName, workflowEvent.PerformanceRecord);
            },
            async activityEvent =>
            {
                await dbConnection.StoreActivityPerformanceRecordAsync(activityEvent.namespaceName, activityEvent.PerformanceRecord);
            }
        );
        var configService = new ConfigService(dbConnection);
        var activeFlowService = new ActiveFlowService(dbConnection, configService, observationConnection);
        await activeFlowService.InitAsync();
        services.AddSingleton<IConfigService>(configService)
            .AddSingleton<IActiveFlowService>(activeFlowService)
            .TryAddSingleton<IDbConnection>(dbConnection);
        services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(new BigIntegerConverter());
            });
        return services;
    }
}
