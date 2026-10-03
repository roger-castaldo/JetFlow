<a name='assembly'></a>
# JetFlow.UI.Abstractions

## Contents

- [ActivityPerformanceSample](#T-JetFlow-UI-ActivityPerformanceSample 'JetFlow.UI.ActivityPerformanceSample')
  - [#ctor(Started,Completed,Failed,TimedOut,QueueLatency,Duration)](#M-JetFlow-UI-ActivityPerformanceSample-#ctor-System-Int64,System-Int64,System-Int64,System-Int64,JetFlow-UI-PMetrics,JetFlow-UI-PMetrics- 'JetFlow.UI.ActivityPerformanceSample.#ctor(System.Int64,System.Int64,System.Int64,System.Int64,JetFlow.UI.PMetrics,JetFlow.UI.PMetrics)')
  - [Completed](#P-JetFlow-UI-ActivityPerformanceSample-Completed 'JetFlow.UI.ActivityPerformanceSample.Completed')
  - [Duration](#P-JetFlow-UI-ActivityPerformanceSample-Duration 'JetFlow.UI.ActivityPerformanceSample.Duration')
  - [Failed](#P-JetFlow-UI-ActivityPerformanceSample-Failed 'JetFlow.UI.ActivityPerformanceSample.Failed')
  - [QueueLatency](#P-JetFlow-UI-ActivityPerformanceSample-QueueLatency 'JetFlow.UI.ActivityPerformanceSample.QueueLatency')
  - [Started](#P-JetFlow-UI-ActivityPerformanceSample-Started 'JetFlow.UI.ActivityPerformanceSample.Started')
  - [TimedOut](#P-JetFlow-UI-ActivityPerformanceSample-TimedOut 'JetFlow.UI.ActivityPerformanceSample.TimedOut')
- [IDbConnection](#T-JetFlow-UI-Interfaces-IDbConnection 'JetFlow.UI.Interfaces.IDbConnection')
  - [ExtractActivityPerformanceDataAsync(namespaceName,activityName,startTime,endTime,aggregationType)](#M-JetFlow-UI-Interfaces-IDbConnection-ExtractActivityPerformanceDataAsync-System-String,System-String,System-DateTime,System-DateTime- 'JetFlow.UI.Interfaces.IDbConnection.ExtractActivityPerformanceDataAsync(System.String,System.String,System.DateTime,System.DateTime)')
  - [ExtractActivityPerformanceDataAsync(namespaceName,startTime,endTime,aggregationType)](#M-JetFlow-UI-Interfaces-IDbConnection-ExtractActivityPerformanceDataAsync-System-String,System-DateTime,System-DateTime- 'JetFlow.UI.Interfaces.IDbConnection.ExtractActivityPerformanceDataAsync(System.String,System.DateTime,System.DateTime)')
  - [ExtractWorkflowPerformanceDataAsync(namespaceName,workflowName,startTime,endTime,aggregationType)](#M-JetFlow-UI-Interfaces-IDbConnection-ExtractWorkflowPerformanceDataAsync-System-String,System-String,System-DateTime,System-DateTime- 'JetFlow.UI.Interfaces.IDbConnection.ExtractWorkflowPerformanceDataAsync(System.String,System.String,System.DateTime,System.DateTime)')
  - [ExtractWorkflowPerformanceDataAsync(namespaceName,startTime,endTime,aggregationType)](#M-JetFlow-UI-Interfaces-IDbConnection-ExtractWorkflowPerformanceDataAsync-System-String,System-DateTime,System-DateTime- 'JetFlow.UI.Interfaces.IDbConnection.ExtractWorkflowPerformanceDataAsync(System.String,System.DateTime,System.DateTime)')
  - [InitAsync()](#M-JetFlow-UI-Interfaces-IDbConnection-InitAsync 'JetFlow.UI.Interfaces.IDbConnection.InitAsync')
  - [ListNamespacesAsync()](#M-JetFlow-UI-Interfaces-IDbConnection-ListNamespacesAsync 'JetFlow.UI.Interfaces.IDbConnection.ListNamespacesAsync')
  - [PeakActivityPerformanceDataAsync(namespaceName)](#M-JetFlow-UI-Interfaces-IDbConnection-PeakActivityPerformanceDataAsync-System-String- 'JetFlow.UI.Interfaces.IDbConnection.PeakActivityPerformanceDataAsync(System.String)')
  - [PeakWorkflowPerformanceDataAsync(namespaceName)](#M-JetFlow-UI-Interfaces-IDbConnection-PeakWorkflowPerformanceDataAsync-System-String- 'JetFlow.UI.Interfaces.IDbConnection.PeakWorkflowPerformanceDataAsync(System.String)')
  - [RegisterNamespaceAsync(namespaceName)](#M-JetFlow-UI-Interfaces-IDbConnection-RegisterNamespaceAsync-System-String- 'JetFlow.UI.Interfaces.IDbConnection.RegisterNamespaceAsync(System.String)')
  - [StoreActivityPerformanceRecordAsync(namespaceName,record)](#M-JetFlow-UI-Interfaces-IDbConnection-StoreActivityPerformanceRecordAsync-System-String,JetFlow-Data-ActivityPerformanceRecord- 'JetFlow.UI.Interfaces.IDbConnection.StoreActivityPerformanceRecordAsync(System.String,JetFlow.Data.ActivityPerformanceRecord)')
  - [StoreArchiveAsync(archive,namespaceName)](#M-JetFlow-UI-Interfaces-IDbConnection-StoreArchiveAsync-JetFlow-ArchivedWorkflow,System-String- 'JetFlow.UI.Interfaces.IDbConnection.StoreArchiveAsync(JetFlow.ArchivedWorkflow,System.String)')
  - [StoreWorkflowPerformanceRecordAsync(namespaceName,record)](#M-JetFlow-UI-Interfaces-IDbConnection-StoreWorkflowPerformanceRecordAsync-System-String,JetFlow-Data-WorkflowPerformanceRecord- 'JetFlow.UI.Interfaces.IDbConnection.StoreWorkflowPerformanceRecordAsync(System.String,JetFlow.Data.WorkflowPerformanceRecord)')
  - [UnregisterNamespaceAsync(namespaceName)](#M-JetFlow-UI-Interfaces-IDbConnection-UnregisterNamespaceAsync-System-String- 'JetFlow.UI.Interfaces.IDbConnection.UnregisterNamespaceAsync(System.String)')
- [NamedActivityPerformanceSample](#T-JetFlow-UI-NamedActivityPerformanceSample 'JetFlow.UI.NamedActivityPerformanceSample')
  - [#ctor(Name,Started,Completed,Failed,TimedOut,QueueLatency,Duration)](#M-JetFlow-UI-NamedActivityPerformanceSample-#ctor-System-String,System-Int64,System-Int64,System-Int64,System-Int64,JetFlow-UI-PMetrics,JetFlow-UI-PMetrics- 'JetFlow.UI.NamedActivityPerformanceSample.#ctor(System.String,System.Int64,System.Int64,System.Int64,System.Int64,JetFlow.UI.PMetrics,JetFlow.UI.PMetrics)')
  - [Name](#P-JetFlow-UI-NamedActivityPerformanceSample-Name 'JetFlow.UI.NamedActivityPerformanceSample.Name')
- [NamedWorkflowPerformanceSample](#T-JetFlow-UI-NamedWorkflowPerformanceSample 'JetFlow.UI.NamedWorkflowPerformanceSample')
  - [#ctor(Name,Started,Completed,Failed,Purged,QueueLatency)](#M-JetFlow-UI-NamedWorkflowPerformanceSample-#ctor-System-String,System-Int64,System-Int64,System-Int64,System-Int64,JetFlow-UI-PMetrics- 'JetFlow.UI.NamedWorkflowPerformanceSample.#ctor(System.String,System.Int64,System.Int64,System.Int64,System.Int64,JetFlow.UI.PMetrics)')
  - [Name](#P-JetFlow-UI-NamedWorkflowPerformanceSample-Name 'JetFlow.UI.NamedWorkflowPerformanceSample.Name')
- [PMetrics](#T-JetFlow-UI-PMetrics 'JetFlow.UI.PMetrics')
  - [#ctor(P50,P95,P99)](#M-JetFlow-UI-PMetrics-#ctor-System-TimeSpan,System-TimeSpan,System-TimeSpan- 'JetFlow.UI.PMetrics.#ctor(System.TimeSpan,System.TimeSpan,System.TimeSpan)')
  - [P50](#P-JetFlow-UI-PMetrics-P50 'JetFlow.UI.PMetrics.P50')
  - [P95](#P-JetFlow-UI-PMetrics-P95 'JetFlow.UI.PMetrics.P95')
  - [P99](#P-JetFlow-UI-PMetrics-P99 'JetFlow.UI.PMetrics.P99')
- [WorkflowPerformanceSample](#T-JetFlow-UI-WorkflowPerformanceSample 'JetFlow.UI.WorkflowPerformanceSample')
  - [#ctor(Started,Completed,Failed,Purged,QueueLatency)](#M-JetFlow-UI-WorkflowPerformanceSample-#ctor-System-Int64,System-Int64,System-Int64,System-Int64,JetFlow-UI-PMetrics- 'JetFlow.UI.WorkflowPerformanceSample.#ctor(System.Int64,System.Int64,System.Int64,System.Int64,JetFlow.UI.PMetrics)')
  - [Completed](#P-JetFlow-UI-WorkflowPerformanceSample-Completed 'JetFlow.UI.WorkflowPerformanceSample.Completed')
  - [Failed](#P-JetFlow-UI-WorkflowPerformanceSample-Failed 'JetFlow.UI.WorkflowPerformanceSample.Failed')
  - [Purged](#P-JetFlow-UI-WorkflowPerformanceSample-Purged 'JetFlow.UI.WorkflowPerformanceSample.Purged')
  - [QueueLatency](#P-JetFlow-UI-WorkflowPerformanceSample-QueueLatency 'JetFlow.UI.WorkflowPerformanceSample.QueueLatency')
  - [Started](#P-JetFlow-UI-WorkflowPerformanceSample-Started 'JetFlow.UI.WorkflowPerformanceSample.Started')

<a name='T-JetFlow-UI-ActivityPerformanceSample'></a>
## ActivityPerformanceSample `type`

##### Namespace

JetFlow.UI

##### Summary

Aggregated activity performance metrics for a single sampling window.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Started | [T:JetFlow.UI.ActivityPerformanceSample](#T-T-JetFlow-UI-ActivityPerformanceSample 'T:JetFlow.UI.ActivityPerformanceSample') | Number of activity starts during the window. |

<a name='M-JetFlow-UI-ActivityPerformanceSample-#ctor-System-Int64,System-Int64,System-Int64,System-Int64,JetFlow-UI-PMetrics,JetFlow-UI-PMetrics-'></a>
### #ctor(Started,Completed,Failed,TimedOut,QueueLatency,Duration) `constructor`

##### Summary

Aggregated activity performance metrics for a single sampling window.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Started | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of activity starts during the window. |
| Completed | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of activity completions during the window. |
| Failed | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of activity failures during the window. |
| TimedOut | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of activity timeouts during the window. |
| QueueLatency | [JetFlow.UI.PMetrics](#T-JetFlow-UI-PMetrics 'JetFlow.UI.PMetrics') | Distribution metrics for queue latency observed. |
| Duration | [JetFlow.UI.PMetrics](#T-JetFlow-UI-PMetrics 'JetFlow.UI.PMetrics') | Distribution metrics for activity duration observed. |

<a name='P-JetFlow-UI-ActivityPerformanceSample-Completed'></a>
### Completed `property`

##### Summary

Number of activity completions during the window.

<a name='P-JetFlow-UI-ActivityPerformanceSample-Duration'></a>
### Duration `property`

##### Summary

Distribution metrics for activity duration observed.

<a name='P-JetFlow-UI-ActivityPerformanceSample-Failed'></a>
### Failed `property`

##### Summary

Number of activity failures during the window.

<a name='P-JetFlow-UI-ActivityPerformanceSample-QueueLatency'></a>
### QueueLatency `property`

##### Summary

Distribution metrics for queue latency observed.

<a name='P-JetFlow-UI-ActivityPerformanceSample-Started'></a>
### Started `property`

##### Summary

Number of activity starts during the window.

<a name='P-JetFlow-UI-ActivityPerformanceSample-TimedOut'></a>
### TimedOut `property`

##### Summary

Number of activity timeouts during the window.

<a name='T-JetFlow-UI-Interfaces-IDbConnection'></a>
## IDbConnection `type`

##### Namespace

JetFlow.UI.Interfaces

##### Summary

Abstraction over a persistence connection used by the UI to store archives, performance
records and to query available namespaces and performance samples.

<a name='M-JetFlow-UI-Interfaces-IDbConnection-ExtractActivityPerformanceDataAsync-System-String,System-String,System-DateTime,System-DateTime-'></a>
### ExtractActivityPerformanceDataAsync(namespaceName,activityName,startTime,endTime,aggregationType) `method`

##### Summary

Extracts activity performance samples for a specific activity over the provided time range.

##### Returns

A collection of activity performance samples.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to query. |
| activityName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The activity name to filter by. |
| startTime | [System.DateTime](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTime 'System.DateTime') | Start time (UTC) for the query. |
| endTime | [System.DateTime](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTime 'System.DateTime') | End time (UTC) for the query. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-ExtractActivityPerformanceDataAsync-System-String,System-DateTime,System-DateTime-'></a>
### ExtractActivityPerformanceDataAsync(namespaceName,startTime,endTime,aggregationType) `method`

##### Summary

Extracts activity performance samples across all activities in a namespace for the provided time range.

##### Returns

A collection of named activity performance samples.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to query. |
| startTime | [System.DateTime](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTime 'System.DateTime') | Start time (UTC) for the query. |
| endTime | [System.DateTime](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTime 'System.DateTime') | End time (UTC) for the query. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-ExtractWorkflowPerformanceDataAsync-System-String,System-String,System-DateTime,System-DateTime-'></a>
### ExtractWorkflowPerformanceDataAsync(namespaceName,workflowName,startTime,endTime,aggregationType) `method`

##### Summary

Extracts workflow performance samples for a specific workflow over the provided time range.

##### Returns

A collection of workflow performance samples.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to query. |
| workflowName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The workflow name to filter by. |
| startTime | [System.DateTime](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTime 'System.DateTime') | Start time (UTC) for the query. |
| endTime | [System.DateTime](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTime 'System.DateTime') | End time (UTC) for the query. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-ExtractWorkflowPerformanceDataAsync-System-String,System-DateTime,System-DateTime-'></a>
### ExtractWorkflowPerformanceDataAsync(namespaceName,startTime,endTime,aggregationType) `method`

##### Summary

Extracts workflow performance samples across all workflows in a namespace for the provided time range.

##### Returns

A collection of named workflow performance samples.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to query. |
| startTime | [System.DateTime](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTime 'System.DateTime') | Start time (UTC) for the query. |
| endTime | [System.DateTime](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTime 'System.DateTime') | End time (UTC) for the query. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-InitAsync'></a>
### InitAsync() `method`

##### Summary

Initializes the underlying database connection and any required schema or resources.

##### Parameters

This method has no parameters.

<a name='M-JetFlow-UI-Interfaces-IDbConnection-ListNamespacesAsync'></a>
### ListNamespacesAsync() `method`

##### Summary

Lists the currently registered namespaces.

##### Returns

A collection of namespace names (null values represent the default namespace).

##### Parameters

This method has no parameters.

<a name='M-JetFlow-UI-Interfaces-IDbConnection-PeakActivityPerformanceDataAsync-System-String-'></a>
### PeakActivityPerformanceDataAsync(namespaceName) `method`

##### Summary

Returns recent activity performance records for the specified namespace.

##### Returns

A collection of activity performance records.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to query. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-PeakWorkflowPerformanceDataAsync-System-String-'></a>
### PeakWorkflowPerformanceDataAsync(namespaceName) `method`

##### Summary

Returns recent workflow performance records for the specified namespace.

##### Returns

A collection of workflow performance records.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to query. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-RegisterNamespaceAsync-System-String-'></a>
### RegisterNamespaceAsync(namespaceName) `method`

##### Summary

Registers a new workflow namespace for tracking.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to register, or null for the default namespace. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-StoreActivityPerformanceRecordAsync-System-String,JetFlow-Data-ActivityPerformanceRecord-'></a>
### StoreActivityPerformanceRecordAsync(namespaceName,record) `method`

##### Summary

Stores an activity performance record for a namespace.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace the record belongs to. |
| record | [JetFlow.Data.ActivityPerformanceRecord](#T-JetFlow-Data-ActivityPerformanceRecord 'JetFlow.Data.ActivityPerformanceRecord') | The activity performance record to store. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-StoreArchiveAsync-JetFlow-ArchivedWorkflow,System-String-'></a>
### StoreArchiveAsync(archive,namespaceName) `method`

##### Summary

Stores an archived workflow record for the specified namespace.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| archive | [JetFlow.ArchivedWorkflow](#T-JetFlow-ArchivedWorkflow 'JetFlow.ArchivedWorkflow') | The archived workflow to store. |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to associate the archive with. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-StoreWorkflowPerformanceRecordAsync-System-String,JetFlow-Data-WorkflowPerformanceRecord-'></a>
### StoreWorkflowPerformanceRecordAsync(namespaceName,record) `method`

##### Summary

Stores a workflow performance record for a namespace.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace the record belongs to. |
| record | [JetFlow.Data.WorkflowPerformanceRecord](#T-JetFlow-Data-WorkflowPerformanceRecord 'JetFlow.Data.WorkflowPerformanceRecord') | The workflow performance record to store. |

<a name='M-JetFlow-UI-Interfaces-IDbConnection-UnregisterNamespaceAsync-System-String-'></a>
### UnregisterNamespaceAsync(namespaceName) `method`

##### Summary

Unregisters a workflow namespace.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| namespaceName | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The namespace to unregister, or null for the default namespace. |

<a name='T-JetFlow-UI-NamedActivityPerformanceSample'></a>
## NamedActivityPerformanceSample `type`

##### Namespace

JetFlow.UI

##### Summary

Named activity performance sample including the activity name and aggregated metrics.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Name | [T:JetFlow.UI.NamedActivityPerformanceSample](#T-T-JetFlow-UI-NamedActivityPerformanceSample 'T:JetFlow.UI.NamedActivityPerformanceSample') | The activity name these metrics belong to. |

<a name='M-JetFlow-UI-NamedActivityPerformanceSample-#ctor-System-String,System-Int64,System-Int64,System-Int64,System-Int64,JetFlow-UI-PMetrics,JetFlow-UI-PMetrics-'></a>
### #ctor(Name,Started,Completed,Failed,TimedOut,QueueLatency,Duration) `constructor`

##### Summary

Named activity performance sample including the activity name and aggregated metrics.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The activity name these metrics belong to. |
| Started | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of activity starts during the window. |
| Completed | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of activity completions during the window. |
| Failed | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of activity failures during the window. |
| TimedOut | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of activity timeouts during the window. |
| QueueLatency | [JetFlow.UI.PMetrics](#T-JetFlow-UI-PMetrics 'JetFlow.UI.PMetrics') | Distribution metrics for queue latency observed. |
| Duration | [JetFlow.UI.PMetrics](#T-JetFlow-UI-PMetrics 'JetFlow.UI.PMetrics') | Distribution metrics for activity duration observed. |

<a name='P-JetFlow-UI-NamedActivityPerformanceSample-Name'></a>
### Name `property`

##### Summary

The activity name these metrics belong to.

<a name='T-JetFlow-UI-NamedWorkflowPerformanceSample'></a>
## NamedWorkflowPerformanceSample `type`

##### Namespace

JetFlow.UI

##### Summary

Named workflow performance sample including the workflow name and aggregated metrics.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Name | [T:JetFlow.UI.NamedWorkflowPerformanceSample](#T-T-JetFlow-UI-NamedWorkflowPerformanceSample 'T:JetFlow.UI.NamedWorkflowPerformanceSample') | The workflow name these metrics belong to. |

<a name='M-JetFlow-UI-NamedWorkflowPerformanceSample-#ctor-System-String,System-Int64,System-Int64,System-Int64,System-Int64,JetFlow-UI-PMetrics-'></a>
### #ctor(Name,Started,Completed,Failed,Purged,QueueLatency) `constructor`

##### Summary

Named workflow performance sample including the workflow name and aggregated metrics.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The workflow name these metrics belong to. |
| Started | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of workflow starts during the window. |
| Completed | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of workflow completions during the window. |
| Failed | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of workflow failures during the window. |
| Purged | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of workflows purged during the window. |
| QueueLatency | [JetFlow.UI.PMetrics](#T-JetFlow-UI-PMetrics 'JetFlow.UI.PMetrics') | Distribution metrics for queue latency observed. |

<a name='P-JetFlow-UI-NamedWorkflowPerformanceSample-Name'></a>
### Name `property`

##### Summary

The workflow name these metrics belong to.

<a name='T-JetFlow-UI-PMetrics'></a>
## PMetrics `type`

##### Namespace

JetFlow.UI

##### Summary

Percentile metrics (P50/P95/P99) for latency or duration distributions.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| P50 | [T:JetFlow.UI.PMetrics](#T-T-JetFlow-UI-PMetrics 'T:JetFlow.UI.PMetrics') | 50th percentile (median) duration. |

<a name='M-JetFlow-UI-PMetrics-#ctor-System-TimeSpan,System-TimeSpan,System-TimeSpan-'></a>
### #ctor(P50,P95,P99) `constructor`

##### Summary

Percentile metrics (P50/P95/P99) for latency or duration distributions.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| P50 | [System.TimeSpan](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.TimeSpan 'System.TimeSpan') | 50th percentile (median) duration. |
| P95 | [System.TimeSpan](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.TimeSpan 'System.TimeSpan') | 95th percentile duration. |
| P99 | [System.TimeSpan](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.TimeSpan 'System.TimeSpan') | 99th percentile duration. |

<a name='P-JetFlow-UI-PMetrics-P50'></a>
### P50 `property`

##### Summary

50th percentile (median) duration.

<a name='P-JetFlow-UI-PMetrics-P95'></a>
### P95 `property`

##### Summary

95th percentile duration.

<a name='P-JetFlow-UI-PMetrics-P99'></a>
### P99 `property`

##### Summary

99th percentile duration.

<a name='T-JetFlow-UI-WorkflowPerformanceSample'></a>
## WorkflowPerformanceSample `type`

##### Namespace

JetFlow.UI

##### Summary

Aggregated workflow performance metrics for a single sampling window.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Started | [T:JetFlow.UI.WorkflowPerformanceSample](#T-T-JetFlow-UI-WorkflowPerformanceSample 'T:JetFlow.UI.WorkflowPerformanceSample') | Number of workflow starts during the window. |

<a name='M-JetFlow-UI-WorkflowPerformanceSample-#ctor-System-Int64,System-Int64,System-Int64,System-Int64,JetFlow-UI-PMetrics-'></a>
### #ctor(Started,Completed,Failed,Purged,QueueLatency) `constructor`

##### Summary

Aggregated workflow performance metrics for a single sampling window.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| Started | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of workflow starts during the window. |
| Completed | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of workflow completions during the window. |
| Failed | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of workflow failures during the window. |
| Purged | [System.Int64](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int64 'System.Int64') | Number of workflows purged during the window. |
| QueueLatency | [JetFlow.UI.PMetrics](#T-JetFlow-UI-PMetrics 'JetFlow.UI.PMetrics') | Distribution metrics for queue latency observed. |

<a name='P-JetFlow-UI-WorkflowPerformanceSample-Completed'></a>
### Completed `property`

##### Summary

Number of workflow completions during the window.

<a name='P-JetFlow-UI-WorkflowPerformanceSample-Failed'></a>
### Failed `property`

##### Summary

Number of workflow failures during the window.

<a name='P-JetFlow-UI-WorkflowPerformanceSample-Purged'></a>
### Purged `property`

##### Summary

Number of workflows purged during the window.

<a name='P-JetFlow-UI-WorkflowPerformanceSample-QueueLatency'></a>
### QueueLatency `property`

##### Summary

Distribution metrics for queue latency observed.

<a name='P-JetFlow-UI-WorkflowPerformanceSample-Started'></a>
### Started `property`

##### Summary

Number of workflow starts during the window.
