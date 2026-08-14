<a name='assembly'></a>
# JetFlow.Observability

## Contents

- [ObservationConnectionFailedException](#T-JetFlow-ObservationConnectionFailedException 'JetFlow.ObservationConnectionFailedException')
- [ObservationConnectionOptions](#T-JetFlow-Configs-ObservationConnectionOptions 'JetFlow.Configs.ObservationConnectionOptions')
  - [#ctor(options)](#M-JetFlow-Configs-ObservationConnectionOptions-#ctor-NATS-Client-Core-NatsOpts- 'JetFlow.Configs.ObservationConnectionOptions.#ctor(NATS.Client.Core.NatsOpts)')
  - [#ctor(connection)](#M-JetFlow-Configs-ObservationConnectionOptions-#ctor-NATS-Client-Core-INatsConnection- 'JetFlow.Configs.ObservationConnectionOptions.#ctor(NATS.Client.Core.INatsConnection)')

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
