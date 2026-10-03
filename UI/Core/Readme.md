<a name='assembly'></a>
# JetFlow.UI

## Contents

- [EndpointRouteBuilderExtension](#T-JetFlow-UI-Extensions-EndpointRouteBuilderExtension 'JetFlow.UI.Extensions.EndpointRouteBuilderExtension')
  - [RegisterJetflowUIEndpoints(routeBuilder)](#M-JetFlow-UI-Extensions-EndpointRouteBuilderExtension-RegisterJetflowUIEndpoints-Microsoft-AspNetCore-Routing-IEndpointRouteBuilder,JetFlow-UI-JetflowUIConfig- 'JetFlow.UI.Extensions.EndpointRouteBuilderExtension.RegisterJetflowUIEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder,JetFlow.UI.JetflowUIConfig)')
- [PerformanceCounters](#T-JetFlow-UI-Data-PerformanceCounters 'JetFlow.UI.Data.PerformanceCounters')
  - [#ctor(ActiveWorkflows,SuspendedWorkflows,ActiveActivities)](#M-JetFlow-UI-Data-PerformanceCounters-#ctor-System-Numerics-BigInteger,System-Numerics-BigInteger,System-Numerics-BigInteger- 'JetFlow.UI.Data.PerformanceCounters.#ctor(System.Numerics.BigInteger,System.Numerics.BigInteger,System.Numerics.BigInteger)')
  - [ActiveActivities](#P-JetFlow-UI-Data-PerformanceCounters-ActiveActivities 'JetFlow.UI.Data.PerformanceCounters.ActiveActivities')
  - [ActiveWorkflows](#P-JetFlow-UI-Data-PerformanceCounters-ActiveWorkflows 'JetFlow.UI.Data.PerformanceCounters.ActiveWorkflows')
  - [SuspendedWorkflows](#P-JetFlow-UI-Data-PerformanceCounters-SuspendedWorkflows 'JetFlow.UI.Data.PerformanceCounters.SuspendedWorkflows')
- [ServiceCollectionExtension](#T-JetFlow-UI-Extensions-ServiceCollectionExtension 'JetFlow.UI.Extensions.ServiceCollectionExtension')
  - [RegisterJetflowUIAsync(services,natsOpts,dbConnection)](#M-JetFlow-UI-Extensions-ServiceCollectionExtension-RegisterJetflowUIAsync-Microsoft-Extensions-DependencyInjection-IServiceCollection,NATS-Client-Core-NatsOpts,JetFlow-UI-Interfaces-IDbConnection- 'JetFlow.UI.Extensions.ServiceCollectionExtension.RegisterJetflowUIAsync(Microsoft.Extensions.DependencyInjection.IServiceCollection,NATS.Client.Core.NatsOpts,JetFlow.UI.Interfaces.IDbConnection)')
- [UnableToConnectException](#T-JetFlow-UI-UnableToConnectException 'JetFlow.UI.UnableToConnectException')

<a name='T-JetFlow-UI-Extensions-EndpointRouteBuilderExtension'></a>
## EndpointRouteBuilderExtension `type`

##### Namespace

JetFlow.UI.Extensions

##### Summary

Extension methods for mapping JetFlow UI endpoints to an [IEndpointRouteBuilder](#T-Microsoft-AspNetCore-Routing-IEndpointRouteBuilder 'Microsoft.AspNetCore.Routing.IEndpointRouteBuilder').

<a name='M-JetFlow-UI-Extensions-EndpointRouteBuilderExtension-RegisterJetflowUIEndpoints-Microsoft-AspNetCore-Routing-IEndpointRouteBuilder,JetFlow-UI-JetflowUIConfig-'></a>
### RegisterJetflowUIEndpoints(routeBuilder) `method`

##### Summary

Registers the JetFlow UI endpoints (static web files, dashboard streams and namespace APIs) on the provided route builder.

##### Returns

The same [IEndpointRouteBuilder](#T-Microsoft-AspNetCore-Routing-IEndpointRouteBuilder 'Microsoft.AspNetCore.Routing.IEndpointRouteBuilder') for chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| routeBuilder | [Microsoft.AspNetCore.Routing.IEndpointRouteBuilder](#T-Microsoft-AspNetCore-Routing-IEndpointRouteBuilder 'Microsoft.AspNetCore.Routing.IEndpointRouteBuilder') | The route builder to register endpoints on. |

<a name='T-JetFlow-UI-Data-PerformanceCounters'></a>
## PerformanceCounters `type`

##### Namespace

JetFlow.UI.Data

##### Summary

Snapshot of various performance counters exposed by the UI for display or monitoring.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| ActiveWorkflows | [T:JetFlow.UI.Data.PerformanceCounters](#T-T-JetFlow-UI-Data-PerformanceCounters 'T:JetFlow.UI.Data.PerformanceCounters') | The number of active workflows currently running. |

<a name='M-JetFlow-UI-Data-PerformanceCounters-#ctor-System-Numerics-BigInteger,System-Numerics-BigInteger,System-Numerics-BigInteger-'></a>
### #ctor(ActiveWorkflows,SuspendedWorkflows,ActiveActivities) `constructor`

##### Summary

Snapshot of various performance counters exposed by the UI for display or monitoring.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| ActiveWorkflows | [System.Numerics.BigInteger](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Numerics.BigInteger 'System.Numerics.BigInteger') | The number of active workflows currently running. |
| SuspendedWorkflows | [System.Numerics.BigInteger](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Numerics.BigInteger 'System.Numerics.BigInteger') | The number of workflows that are currently suspended. |
| ActiveActivities | [System.Numerics.BigInteger](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Numerics.BigInteger 'System.Numerics.BigInteger') | The number of active activities currently executing. |

<a name='P-JetFlow-UI-Data-PerformanceCounters-ActiveActivities'></a>
### ActiveActivities `property`

##### Summary

The number of active activities currently executing.

<a name='P-JetFlow-UI-Data-PerformanceCounters-ActiveWorkflows'></a>
### ActiveWorkflows `property`

##### Summary

The number of active workflows currently running.

<a name='P-JetFlow-UI-Data-PerformanceCounters-SuspendedWorkflows'></a>
### SuspendedWorkflows `property`

##### Summary

The number of workflows that are currently suspended.

<a name='T-JetFlow-UI-Extensions-ServiceCollectionExtension'></a>
## ServiceCollectionExtension `type`

##### Namespace

JetFlow.UI.Extensions

##### Summary

Extension methods for registering JetFlow UI services into an IServiceCollection.

<a name='M-JetFlow-UI-Extensions-ServiceCollectionExtension-RegisterJetflowUIAsync-Microsoft-Extensions-DependencyInjection-IServiceCollection,NATS-Client-Core-NatsOpts,JetFlow-UI-Interfaces-IDbConnection-'></a>
### RegisterJetflowUIAsync(services,natsOpts,dbConnection) `method`

##### Summary

Registers the JetFlow UI services into the provided [IServiceCollection](#T-Microsoft-Extensions-DependencyInjection-IServiceCollection 'Microsoft.Extensions.DependencyInjection.IServiceCollection').
This will initialize the NATS connection, create the JetStream context, initialize the
database connection, wire up observation listeners, and register UI-related services.

##### Returns

The same [IServiceCollection](#T-Microsoft-Extensions-DependencyInjection-IServiceCollection 'Microsoft.Extensions.DependencyInjection.IServiceCollection') instance for chaining.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| services | [Microsoft.Extensions.DependencyInjection.IServiceCollection](#T-Microsoft-Extensions-DependencyInjection-IServiceCollection 'Microsoft.Extensions.DependencyInjection.IServiceCollection') | The service collection to add services to. |
| natsOpts | [NATS.Client.Core.NatsOpts](#T-NATS-Client-Core-NatsOpts 'NATS.Client.Core.NatsOpts') | Options used to configure the NATS connection. |
| dbConnection | [JetFlow.UI.Interfaces.IDbConnection](#T-JetFlow-UI-Interfaces-IDbConnection 'JetFlow.UI.Interfaces.IDbConnection') | The database connection implementation to use for persistence. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [JetFlow.UI.UnableToConnectException](#T-JetFlow-UI-UnableToConnectException 'JetFlow.UI.UnableToConnectException') | Thrown when a connection to NATS cannot be established. |

<a name='T-JetFlow-UI-UnableToConnectException'></a>
## UnableToConnectException `type`

##### Namespace

JetFlow.UI

##### Summary

Thrown when an error occurs attempting to connect to the NATS server.  
Specifically this will be thrown when the Ping that is executed on each initial connection fails.
