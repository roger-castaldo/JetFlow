# JetFlow
DotNetCore workflows on NATS

The basis of this project is to allow the execution of workflows in both a distributed and maintained manner similar to temporal io but purely using Nats and specifically JetStream on top.

Eventually this service will include:
* An observability system that will both archive completed workflows as well as give an interface into viewing both archived and active
* ~~Support built in for Open Telemetry tracking and shared spans~~
* ~~Support for custom archive and cleanup configurations~~
* Examples of how to use this
* Full documentation
* Unit Testing
* ~~Built in Retry/Circuit Break policies~~
* ~~Workflow Activity execution timeouts~~