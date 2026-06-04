<a name='assembly'></a>
# JetFlow.Core

## Contents

- [CompressionTypes](#T-JetFlow-Configs-CompressionTypes 'JetFlow.Configs.CompressionTypes')
  - [Brotli](#F-JetFlow-Configs-CompressionTypes-Brotli 'JetFlow.Configs.CompressionTypes.Brotli')
  - [GZip](#F-JetFlow-Configs-CompressionTypes-GZip 'JetFlow.Configs.CompressionTypes.GZip')
- [Connection](#T-JetFlow-Connection 'JetFlow.Connection')
  - [MetricsMeterName](#F-JetFlow-Connection-MetricsMeterName 'JetFlow.Connection.MetricsMeterName')
  - [TraceProviderName](#F-JetFlow-Connection-TraceProviderName 'JetFlow.Connection.TraceProviderName')
  - [CreateInstanceAsync(options)](#M-JetFlow-Connection-CreateInstanceAsync-JetFlow-Configs-ConnectionOptions- 'JetFlow.Connection.CreateInstanceAsync(JetFlow.Configs.ConnectionOptions)')
- [ConnectionOptions](#T-JetFlow-Configs-ConnectionOptions 'JetFlow.Configs.ConnectionOptions')
  - [#ctor(options)](#M-JetFlow-Configs-ConnectionOptions-#ctor-NATS-Client-Core-NatsOpts- 'JetFlow.Configs.ConnectionOptions.#ctor(NATS.Client.Core.NatsOpts)')
  - [#ctor(connection)](#M-JetFlow-Configs-ConnectionOptions-#ctor-NATS-Client-Core-INatsConnection- 'JetFlow.Configs.ConnectionOptions.#ctor(NATS.Client.Core.INatsConnection)')
  - [#ctor(connection,jsContext)](#M-JetFlow-Configs-ConnectionOptions-#ctor-NATS-Client-Core-INatsConnection,NATS-Client-JetStream-INatsJSContext- 'JetFlow.Configs.ConnectionOptions.#ctor(NATS.Client.Core.INatsConnection,NATS.Client.JetStream.INatsJSContext)')
  - [CompressionType](#P-JetFlow-Configs-ConnectionOptions-CompressionType 'JetFlow.Configs.ConnectionOptions.CompressionType')
  - [DefaultWorkflowOptions](#P-JetFlow-Configs-ConnectionOptions-DefaultWorkflowOptions 'JetFlow.Configs.ConnectionOptions.DefaultWorkflowOptions')
  - [JsonTypeInfoResolver](#P-JetFlow-Configs-ConnectionOptions-JsonTypeInfoResolver 'JetFlow.Configs.ConnectionOptions.JsonTypeInfoResolver')
  - [Namespace](#P-JetFlow-Configs-ConnectionOptions-Namespace 'JetFlow.Configs.ConnectionOptions.Namespace')
- [InvalidContentTypeException](#T-JetFlow-InvalidContentTypeException 'JetFlow.InvalidContentTypeException')
- [InvalidDelayStepException](#T-JetFlow-InvalidDelayStepException 'JetFlow.InvalidDelayStepException')
- [InvalidStepException](#T-JetFlow-InvalidStepException 'JetFlow.InvalidStepException')
- [InvalidWorkflowEventMessage](#T-JetFlow-InvalidWorkflowEventMessage 'JetFlow.InvalidWorkflowEventMessage')
- [UnableToConnectException](#T-JetFlow-UnableToConnectException 'JetFlow.UnableToConnectException')
- [WorkflowEnd](#T-JetFlow-Messages-WorkflowEnd 'JetFlow.Messages.WorkflowEnd')
  - [#ctor(EndTime,ErrorMessage)](#M-JetFlow-Messages-WorkflowEnd-#ctor-System-DateTime,System-String- 'JetFlow.Messages.WorkflowEnd.#ctor(System.DateTime,System.String)')
  - [EndTime](#P-JetFlow-Messages-WorkflowEnd-EndTime 'JetFlow.Messages.WorkflowEnd.EndTime')
  - [ErrorMessage](#P-JetFlow-Messages-WorkflowEnd-ErrorMessage 'JetFlow.Messages.WorkflowEnd.ErrorMessage')
  - [IsSuccess](#P-JetFlow-Messages-WorkflowEnd-IsSuccess 'JetFlow.Messages.WorkflowEnd.IsSuccess')
- [WorkflowEndedException](#T-JetFlow-WorkflowEndedException 'JetFlow.WorkflowEndedException')

<a name='T-JetFlow-Configs-CompressionTypes'></a>
## CompressionTypes `type`

##### Namespace

JetFlow.Configs

##### Summary

Specifies the compression type to be used for compressing message content when sending messages to NATS. The available options are:

<a name='F-JetFlow-Configs-CompressionTypes-Brotli'></a>
### Brotli `constants`

##### Summary

Brotli is a general-purpose lossless compression algorithm that offers high compression ratios and fast decompression speeds. It is particularly effective for compressing text-based data, such as JSON or XML, making it a good choice for NATS messages that contain structured data.

<a name='F-JetFlow-Configs-CompressionTypes-GZip'></a>
### GZip `constants`

##### Summary

GZip is a widely used compression algorithm that provides a good balance between compression ratio and speed. It is suitable for compressing various types of data, including text and binary formats, making it a versatile option for NATS messages.

<a name='T-JetFlow-Connection'></a>
## Connection `type`

##### Namespace

JetFlow

##### Summary

Used to create a connection to the JetFlow runtime, and register workflows and activities. It also manages the lifecycle of the connection and all subscriptions created through it, so disposing it will close all connections and subscriptions created through it. You can have multiple instances of this class connected to the same NATS server, but it's recommended to reuse the same instance as much as possible to avoid unnecessary connections and subscriptions.

<a name='F-JetFlow-Connection-MetricsMeterName'></a>
### MetricsMeterName `constants`

##### Summary

The name of the metrics meter used by JetFlow, you can use it to correlate metrics with the JetFlow runtime. It's recommended to use the same name for both traces and metrics to make it easier to correlate them.

<a name='F-JetFlow-Connection-TraceProviderName'></a>
### TraceProviderName `constants`

##### Summary

The name of the trace provider and metrics meter used by JetFlow, you can use it to correlate traces and metrics with the JetFlow runtime. It's recommended to use the same name for both traces and metrics to make it easier to correlate them.

<a name='M-JetFlow-Connection-CreateInstanceAsync-JetFlow-Configs-ConnectionOptions-'></a>
### CreateInstanceAsync(options) `method`

##### Summary

Called to create a new instance of the connection, which will be used to register workflows and activities, and manage the lifecycle of the connection and all subscriptions created through it. It's recommended to reuse the same instance as much as possible to avoid unnecessary connections and subscriptions. This method will attempt to connect to the NATS server, and if it fails, it will throw an exception. If the connection is successful, it will create the necessary streams, key value stores, object stores and consumers required for the JetFlow runtime to function properly. The connection will be left open until the instance is disposed, so you can use it to register workflows and activities at any time after it's created.

##### Returns

A ValueTask representing the asynchronous operation, with a result of type IConnection.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| options | [JetFlow.Configs.ConnectionOptions](#T-JetFlow-Configs-ConnectionOptions 'JetFlow.Configs.ConnectionOptions') | The ConnectionOptions object containing the configuration options for the connection. |

<a name='T-JetFlow-Configs-ConnectionOptions'></a>
## ConnectionOptions `type`

##### Namespace

JetFlow.Configs

##### Summary

Houses the connection options for connecting to NATS, including the NATS connection, JetStream context, JSON type info resolver, compression type, default workflow options, and namespace. This class provides a convenient way to configure and manage the connection settings for interacting with NATS and JetStream.

<a name='M-JetFlow-Configs-ConnectionOptions-#ctor-NATS-Client-Core-NatsOpts-'></a>
### #ctor(options) `constructor`

##### Summary

Constructs a new instance of the ConnectionOptions class using the provided NatsOpts. This constructor initializes the NATS connection and JetStream context based on the specified options, allowing for easy configuration of the connection settings for interacting with NATS and JetStream.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| options | [NATS.Client.Core.NatsOpts](#T-NATS-Client-Core-NatsOpts 'NATS.Client.Core.NatsOpts') | The NatsOpts object containing the configuration options for the NATS connection. |

<a name='M-JetFlow-Configs-ConnectionOptions-#ctor-NATS-Client-Core-INatsConnection-'></a>
### #ctor(connection) `constructor`

##### Summary

Constructs a new instance of the ConnectionOptions class using the provided NATS connection. This constructor initializes the JetStream context based on the specified NATS connection, allowing for easy configuration of the connection settings for interacting with NATS and JetStream.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| connection | [NATS.Client.Core.INatsConnection](#T-NATS-Client-Core-INatsConnection 'NATS.Client.Core.INatsConnection') | The INatsConnection object representing the NATS connection. |

<a name='M-JetFlow-Configs-ConnectionOptions-#ctor-NATS-Client-Core-INatsConnection,NATS-Client-JetStream-INatsJSContext-'></a>
### #ctor(connection,jsContext) `constructor`

##### Summary

Constructs a new instance of the ConnectionOptions class using the provided NATS connection and JetStream context. This constructor allows for direct initialization of both the NATS connection and JetStream context, providing flexibility in configuring the connection settings for interacting with NATS and JetStream.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| connection | [NATS.Client.Core.INatsConnection](#T-NATS-Client-Core-INatsConnection 'NATS.Client.Core.INatsConnection') |  |
| jsContext | [NATS.Client.JetStream.INatsJSContext](#T-NATS-Client-JetStream-INatsJSContext 'NATS.Client.JetStream.INatsJSContext') |  |

<a name='P-JetFlow-Configs-ConnectionOptions-CompressionType'></a>
### CompressionType `property`

##### Summary

Specifies the compression type to be used for compressing message content when sending messages to NATS. The available options are Brotli and GZip, which offer different levels of compression and performance characteristics. By configuring the CompressionType property, you can optimize the size of your NATS messages and improve the efficiency of data transmission, especially when dealing with large payloads or high message volumes.

<a name='P-JetFlow-Configs-ConnectionOptions-DefaultWorkflowOptions'></a>
### DefaultWorkflowOptions `property`

##### Summary

Specifies the default workflow options to be used when executing workflows in JetFlow. The WorkflowOptions class contains various settings that can be configured to control the behavior of workflows, such as timeouts, retry policies, and error handling strategies. By setting the DefaultWorkflowOptions property, you can ensure that all workflows executed in JetFlow adhere to a consistent set of options, simplifying configuration and improving maintainability.

<a name='P-JetFlow-Configs-ConnectionOptions-JsonTypeInfoResolver'></a>
### JsonTypeInfoResolver `property`

##### Summary

Uses the System.Text.Json source generator to generate serialization code at compile time, improving performance and reducing memory allocations when serializing and deserializing JSON data. By providing a custom IJsonTypeInfoResolver, you can control how types are serialized and deserialized, allowing for customization of the JSON output and input when working with NATS messages.

<a name='P-JetFlow-Configs-ConnectionOptions-Namespace'></a>
### Namespace `property`

##### Summary

Used to logically group related workflows and resources within JetFlow. By specifying a namespace, you can organize your workflows and resources in a way that makes it easier to manage and maintain them, especially in larger applications with multiple teams or modules. The Namespace property allows you to create a clear separation between different parts of your application, improving readability and reducing the likelihood of naming conflicts when working with NATS and JetStream.

<a name='T-JetFlow-InvalidContentTypeException'></a>
## InvalidContentTypeException `type`

##### Namespace

JetFlow

##### Summary

Thrown when a workflow event message is received with a content type that is not recognized or supported by the system.  This can occur when a message is received with a content type that is not registered in the system, or when a message is received with a content type that is registered but does not have a corresponding decoder or handler.  This can also occur if a message is received with a content type that is registered but is not properly formatted or contains invalid values.

<a name='T-JetFlow-InvalidDelayStepException'></a>
## InvalidDelayStepException `type`

##### Namespace

JetFlow

##### Summary

Thrown when a delay finished event is received but the subject of the message does not match the expected subject for a delay finished event.  This can occur when a message is received with a subject that is not registered in the system, or when a message is received with a subject that is registered but does not correspond to a delay finished event.  This can also occur if a message is received with a subject that is registered but is not properly formatted or contains invalid values.

<a name='T-JetFlow-InvalidStepException'></a>
## InvalidStepException `type`

##### Namespace

JetFlow

##### Summary

Thrown when an attempt is made to execute a step that does not match the expected step.  This can occur when a workflow is executing and the next step is expected to be a specific step, but the step that is attempted to be executed does not match the expected step.  This can also occur if a step is attempted to be executed on a workflow that is not currently executing, but the step was not aware of the workflow's state.

<a name='T-JetFlow-InvalidWorkflowEventMessage'></a>
## InvalidWorkflowEventMessage `type`

##### Namespace

JetFlow

##### Summary

Thrown when a workflow event message is received that does not match the expected format for a workflow event message.  This can occur when a message is received that is not a workflow event message, or when a message is received that is a workflow event message but does not contain the expected fields or values.  This can also occur if a message is received that is a workflow event message but is not properly formatted, such as missing required fields or containing invalid values.

<a name='T-JetFlow-UnableToConnectException'></a>
## UnableToConnectException `type`

##### Namespace

JetFlow

##### Summary

Thrown when an error occurs attempting to connect to the NATS server.  
Specifically this will be thrown when the Ping that is executed on each initial connection fails.

<a name='T-JetFlow-Messages-WorkflowEnd'></a>
## WorkflowEnd `type`

##### Namespace

JetFlow.Messages

##### Summary

Houses the information about the end of a workflow, including the time it ended and any error message if it failed.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| EndTime | [T:JetFlow.Messages.WorkflowEnd](#T-T-JetFlow-Messages-WorkflowEnd 'T:JetFlow.Messages.WorkflowEnd') | The time the workflow ended. |

<a name='M-JetFlow-Messages-WorkflowEnd-#ctor-System-DateTime,System-String-'></a>
### #ctor(EndTime,ErrorMessage) `constructor`

##### Summary

Houses the information about the end of a workflow, including the time it ended and any error message if it failed.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| EndTime | [System.DateTime](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.DateTime 'System.DateTime') | The time the workflow ended. |
| ErrorMessage | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The error message if the workflow failed, otherwise null. |

<a name='P-JetFlow-Messages-WorkflowEnd-EndTime'></a>
### EndTime `property`

##### Summary

The time the workflow ended.

<a name='P-JetFlow-Messages-WorkflowEnd-ErrorMessage'></a>
### ErrorMessage `property`

##### Summary

The error message if the workflow failed, otherwise null.

<a name='P-JetFlow-Messages-WorkflowEnd-IsSuccess'></a>
### IsSuccess `property`

##### Summary

Indicates whether the workflow ended successfully, which is true if there is no error message.

<a name='T-JetFlow-WorkflowEndedException'></a>
## WorkflowEndedException `type`

##### Namespace

JetFlow

##### Summary

Thrown when an attempt is made to execute an activity on a workflow that has already completed.  This can occur when a workflow completes while an activity is executing, and the activity attempts to report completion after the workflow has already completed.  It can also occur if an activity is attempted to be executed on a workflow that has already completed, but the activity was not aware of the completion.
