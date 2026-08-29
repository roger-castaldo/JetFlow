<a name='assembly'></a>
# JetFlow.Observability

## Contents

- [IObservationConnection](#T-JetFlow-Interfaces-IObservationConnection 'JetFlow.Interfaces.IObservationConnection')
  - [AddDefaultNamespaceAsync()](#M-JetFlow-Interfaces-IObservationConnection-AddDefaultNamespaceAsync 'JetFlow.Interfaces.IObservationConnection.AddDefaultNamespaceAsync')
  - [AddNamespaceAsync(workflowNamespace)](#M-JetFlow-Interfaces-IObservationConnection-AddNamespaceAsync-System-String- 'JetFlow.Interfaces.IObservationConnection.AddNamespaceAsync(System.String)')
  - [AddNamespacesAsync(workflowNamespaces)](#M-JetFlow-Interfaces-IObservationConnection-AddNamespacesAsync-System-Collections-Generic-IEnumerable{System-String}- 'JetFlow.Interfaces.IObservationConnection.AddNamespacesAsync(System.Collections.Generic.IEnumerable{System.String})')
  - [AddPerformanceMonitoringAsync(sampleDurationMinutes,workflowRecordReceived,activityRecordReceived)](#M-JetFlow-Interfaces-IObservationConnection-AddPerformanceMonitoringAsync-System-Byte,System-Func{JetFlow-Data-WorkflowPerformanceRecord,System-Threading-Tasks-ValueTask},System-Func{JetFlow-Data-ActivityPerformanceRecord,System-Threading-Tasks-ValueTask}- 'JetFlow.Interfaces.IObservationConnection.AddPerformanceMonitoringAsync(System.Byte,System.Func{JetFlow.Data.WorkflowPerformanceRecord,System.Threading.Tasks.ValueTask},System.Func{JetFlow.Data.ActivityPerformanceRecord,System.Threading.Tasks.ValueTask})')
  - [GetActiveActivityCountAsync()](#M-JetFlow-Interfaces-IObservationConnection-GetActiveActivityCountAsync 'JetFlow.Interfaces.IObservationConnection.GetActiveActivityCountAsync')
  - [GetActiveActivityCountAsync(workflowName)](#M-JetFlow-Interfaces-IObservationConnection-GetActiveActivityCountAsync-System-String- 'JetFlow.Interfaces.IObservationConnection.GetActiveActivityCountAsync(System.String)')
  - [GetActiveWorkflowCountAsync()](#M-JetFlow-Interfaces-IObservationConnection-GetActiveWorkflowCountAsync 'JetFlow.Interfaces.IObservationConnection.GetActiveWorkflowCountAsync')
  - [GetActiveWorkflowCountAsync(workflowNamespace)](#M-JetFlow-Interfaces-IObservationConnection-GetActiveWorkflowCountAsync-System-String- 'JetFlow.Interfaces.IObservationConnection.GetActiveWorkflowCountAsync(System.String)')
  - [GetSuspendedWorkflowCountAsync()](#M-JetFlow-Interfaces-IObservationConnection-GetSuspendedWorkflowCountAsync 'JetFlow.Interfaces.IObservationConnection.GetSuspendedWorkflowCountAsync')
  - [GetSuspendedWorkflowCountAsync(workflowNamespace)](#M-JetFlow-Interfaces-IObservationConnection-GetSuspendedWorkflowCountAsync-System-String- 'JetFlow.Interfaces.IObservationConnection.GetSuspendedWorkflowCountAsync(System.String)')
  - [RemoveDefaultNamespaceAsync()](#M-JetFlow-Interfaces-IObservationConnection-RemoveDefaultNamespaceAsync 'JetFlow.Interfaces.IObservationConnection.RemoveDefaultNamespaceAsync')
  - [RemoveNamespaceAsync(workflowNamespace)](#M-JetFlow-Interfaces-IObservationConnection-RemoveNamespaceAsync-System-String- 'JetFlow.Interfaces.IObservationConnection.RemoveNamespaceAsync(System.String)')
  - [RemoveNamespacesAsync(workflowNamespaces)](#M-JetFlow-Interfaces-IObservationConnection-RemoveNamespacesAsync-System-Collections-Generic-IEnumerable{System-String}- 'JetFlow.Interfaces.IObservationConnection.RemoveNamespacesAsync(System.Collections.Generic.IEnumerable{System.String})')
- [ObservationConnection](#T-JetFlow-ObservationConnection 'JetFlow.ObservationConnection')
  - [CreateInstanceAsync(options)](#M-JetFlow-ObservationConnection-CreateInstanceAsync-JetFlow-Configs-ObservationConnectionOptions- 'JetFlow.ObservationConnection.CreateInstanceAsync(JetFlow.Configs.ObservationConnectionOptions)')
- [ObservationConnectionFailedException](#T-JetFlow-ObservationConnectionFailedException 'JetFlow.ObservationConnectionFailedException')
- [ObservationConnectionOptions](#T-JetFlow-Configs-ObservationConnectionOptions 'JetFlow.Configs.ObservationConnectionOptions')
  - [#ctor(options)](#M-JetFlow-Configs-ObservationConnectionOptions-#ctor-NATS-Client-Core-NatsOpts- 'JetFlow.Configs.ObservationConnectionOptions.#ctor(NATS.Client.Core.NatsOpts)')
  - [#ctor(connection)](#M-JetFlow-Configs-ObservationConnectionOptions-#ctor-NATS-Client-Core-INatsConnection- 'JetFlow.Configs.ObservationConnectionOptions.#ctor(NATS.Client.Core.INatsConnection)')
  - [GroupName](#P-JetFlow-Configs-ObservationConnectionOptions-GroupName 'JetFlow.Configs.ObservationConnectionOptions.GroupName')

<a name='T-JetFlow-Interfaces-IObservationConnection'></a>
## IObservationConnection `type`

##### Namespace

JetFlow.Interfaces

##### Summary

Represents a connection used to observe runtime metrics and performance
for workflows and activities. Implementations provide methods to query
counters, manage observed namespaces, and subscribe to periodic performance
records.

<a name='M-JetFlow-Interfaces-IObservationConnection-AddDefaultNamespaceAsync'></a>
### AddDefaultNamespaceAsync() `method`

##### Summary

Adds the default namespace to the set of namespaces being observed.

##### Parameters

This method has no parameters.

<a name='M-JetFlow-Interfaces-IObservationConnection-AddNamespaceAsync-System-String-'></a>
### AddNamespaceAsync(workflowNamespace) `method`

##### Summary

Adds a namespace to the set of namespaces being observed.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| workflowNamespace | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to add. |

<a name='M-JetFlow-Interfaces-IObservationConnection-AddNamespacesAsync-System-Collections-Generic-IEnumerable{System-String}-'></a>
### AddNamespacesAsync(workflowNamespaces) `method`

##### Summary

Adds multiple namespaces to the set of namespaces being observed.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| workflowNamespaces | [System.Collections.Generic.IEnumerable{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.String}') | The namespaces to add. |

<a name='M-JetFlow-Interfaces-IObservationConnection-AddPerformanceMonitoringAsync-System-Byte,System-Func{JetFlow-Data-WorkflowPerformanceRecord,System-Threading-Tasks-ValueTask},System-Func{JetFlow-Data-ActivityPerformanceRecord,System-Threading-Tasks-ValueTask}-'></a>
### AddPerformanceMonitoringAsync(sampleDurationMinutes,workflowRecordReceived,activityRecordReceived) `method`

##### Summary

Starts periodic performance monitoring. The connection will sample
performance data at the specified interval and invoke the provided
callbacks for workflow and activity performance records.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| sampleDurationMinutes | [System.Byte](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Byte 'System.Byte') | Sampling interval in minutes. A small positive value is expected. |
| workflowRecordReceived | [System.Func{JetFlow.Data.WorkflowPerformanceRecord,System.Threading.Tasks.ValueTask}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{JetFlow.Data.WorkflowPerformanceRecord,System.Threading.Tasks.ValueTask}') | Callback invoked when a [WorkflowPerformanceRecord](#T-JetFlow-Data-WorkflowPerformanceRecord 'JetFlow.Data.WorkflowPerformanceRecord') is available. |
| activityRecordReceived | [System.Func{JetFlow.Data.ActivityPerformanceRecord,System.Threading.Tasks.ValueTask}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Func 'System.Func{JetFlow.Data.ActivityPerformanceRecord,System.Threading.Tasks.ValueTask}') | Callback invoked when an [ActivityPerformanceRecord](#T-JetFlow-Data-ActivityPerformanceRecord 'JetFlow.Data.ActivityPerformanceRecord') is available. |

<a name='M-JetFlow-Interfaces-IObservationConnection-GetActiveActivityCountAsync'></a>
### GetActiveActivityCountAsync() `method`

##### Summary

Gets the current active activity counts grouped by a performance counter key.

##### Returns

A collection of [PerformanceCounter](#T-JetFlow-Data-PerformanceCounter 'JetFlow.Data.PerformanceCounter') instances representing active activity counts.

##### Parameters

This method has no parameters.

<a name='M-JetFlow-Interfaces-IObservationConnection-GetActiveActivityCountAsync-System-String-'></a>
### GetActiveActivityCountAsync(workflowName) `method`

##### Summary

Gets the current active activity count for the specified workflow name.

##### Returns

The number of active activities for the specified workflow.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| workflowName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The workflow name to filter activity counts by, or `null` to use all workflows. |

<a name='M-JetFlow-Interfaces-IObservationConnection-GetActiveWorkflowCountAsync'></a>
### GetActiveWorkflowCountAsync() `method`

##### Summary

Gets the current active workflow counts grouped by a performance counter key.

##### Returns

A collection of [PerformanceCounter](#T-JetFlow-Data-PerformanceCounter 'JetFlow.Data.PerformanceCounter') instances representing active workflow counts.

##### Parameters

This method has no parameters.

<a name='M-JetFlow-Interfaces-IObservationConnection-GetActiveWorkflowCountAsync-System-String-'></a>
### GetActiveWorkflowCountAsync(workflowNamespace) `method`

##### Summary

Gets the current active workflow count for the specified namespace.

##### Returns

The number of active workflows in the requested namespace.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| workflowNamespace | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The workflow namespace to filter by, or `null` to use the default namespace. |

<a name='M-JetFlow-Interfaces-IObservationConnection-GetSuspendedWorkflowCountAsync'></a>
### GetSuspendedWorkflowCountAsync() `method`

##### Summary

Gets the current suspended workflow counts grouped by a performance counter key.

##### Returns

A collection of [PerformanceCounter](#T-JetFlow-Data-PerformanceCounter 'JetFlow.Data.PerformanceCounter') instances representing suspended workflow counts.

##### Parameters

This method has no parameters.

<a name='M-JetFlow-Interfaces-IObservationConnection-GetSuspendedWorkflowCountAsync-System-String-'></a>
### GetSuspendedWorkflowCountAsync(workflowNamespace) `method`

##### Summary

Gets the current suspended workflow count for the specified namespace.

##### Returns

The number of suspended workflows in the requested namespace.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| workflowNamespace | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The workflow namespace to filter by, or `null` to use the default namespace. |

<a name='M-JetFlow-Interfaces-IObservationConnection-RemoveDefaultNamespaceAsync'></a>
### RemoveDefaultNamespaceAsync() `method`

##### Summary

Removes the default namespace from the set of namespaces being observed.

##### Parameters

This method has no parameters.

<a name='M-JetFlow-Interfaces-IObservationConnection-RemoveNamespaceAsync-System-String-'></a>
### RemoveNamespaceAsync(workflowNamespace) `method`

##### Summary

Removes a namespace from the set of namespaces being observed.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| workflowNamespace | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to remove. |

<a name='M-JetFlow-Interfaces-IObservationConnection-RemoveNamespacesAsync-System-Collections-Generic-IEnumerable{System-String}-'></a>
### RemoveNamespacesAsync(workflowNamespaces) `method`

##### Summary

Removes multiple namespaces from the set of namespaces being observed.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| workflowNamespaces | [System.Collections.Generic.IEnumerable{System.String}](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Collections.Generic.IEnumerable 'System.Collections.Generic.IEnumerable{System.String}') | The namespaces to remove. |

<a name='T-JetFlow-ObservationConnection'></a>
## ObservationConnection `type`

##### Namespace

JetFlow

##### Summary

Factory methods for creating [IObservationConnection](#T-JetFlow-Interfaces-IObservationConnection 'JetFlow.Interfaces.IObservationConnection') instances.
Use [CreateInstanceAsync](#M-JetFlow-ObservationConnection-CreateInstanceAsync-JetFlow-Configs-ObservationConnectionOptions- 'JetFlow.ObservationConnection.CreateInstanceAsync(JetFlow.Configs.ObservationConnectionOptions)') to obtain
a runtime connection for observing workflow and activity performance data.

<a name='M-JetFlow-ObservationConnection-CreateInstanceAsync-JetFlow-Configs-ObservationConnectionOptions-'></a>
### CreateInstanceAsync(options) `method`

##### Summary

Creates an [IObservationConnection](#T-JetFlow-Interfaces-IObservationConnection 'JetFlow.Interfaces.IObservationConnection') based on the provided options.
This method performs any necessary connection setup and validation asynchronously.

##### Returns

A task that resolves to an [IObservationConnection](#T-JetFlow-Interfaces-IObservationConnection 'JetFlow.Interfaces.IObservationConnection') instance.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| options | [JetFlow.Configs.ObservationConnectionOptions](#T-JetFlow-Configs-ObservationConnectionOptions 'JetFlow.Configs.ObservationConnectionOptions') | Configuration options used to establish the observation connection. |

<a name='T-JetFlow-ObservationConnectionFailedException'></a>
## ObservationConnectionFailedException `type`

##### Namespace

JetFlow

##### Summary

Thrown when an error occurs attempting to connect to the NATS server.  
Specifically this will be thrown when the Ping that is executed on each initial connection fails.

<a name='T-JetFlow-Configs-ObservationConnectionOptions'></a>
## ObservationConnectionOptions `type`

##### Namespace

JetFlow.Configs

##### Summary

Houses the connection options for connecting to NATS, including the NATS connection and JetStream context. This class provides a convenient way to configure and manage the connection settings for interacting with NATS and JetStream.

<a name='M-JetFlow-Configs-ObservationConnectionOptions-#ctor-NATS-Client-Core-NatsOpts-'></a>
### #ctor(options) `constructor`

##### Summary

Constructs a new instance of the ObservationConnectionOptions class using the provided NatsOpts. This constructor initializes the NATS connection and JetStream context based on the specified options, allowing for easy configuration of the connection settings for interacting with NATS and JetStream.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| options | [NATS.Client.Core.NatsOpts](#T-NATS-Client-Core-NatsOpts 'NATS.Client.Core.NatsOpts') | The NatsOpts object containing the configuration options for the NATS connection. |

<a name='M-JetFlow-Configs-ObservationConnectionOptions-#ctor-NATS-Client-Core-INatsConnection-'></a>
### #ctor(connection) `constructor`

##### Summary

Constructs a new instance of the ObservationConnectionOptions class using the provided NATS connection. This constructor initializes the JetStream context based on the specified NATS connection, allowing for easy configuration of the connection settings for interacting with NATS and JetStream.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| connection | [NATS.Client.Core.INatsConnection](#T-NATS-Client-Core-INatsConnection 'NATS.Client.Core.INatsConnection') | The INatsConnection object representing the NATS connection. |

<a name='P-JetFlow-Configs-ObservationConnectionOptions-GroupName'></a>
### GroupName `property`

##### Summary

Identifies the connection to listen under a group name, this is used if multiple instances of a given service are being used to share load.
