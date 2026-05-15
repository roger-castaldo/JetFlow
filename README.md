# JetFlow
[![.NET-Test-8x](https://github.com/roger-castaldo/JetFlow/actions/workflows/unittests8x.yml/badge.svg)](https://github.com/roger-castaldo/JetFlow/actions/workflows/unittests8x.yml)
[![CodeQL](https://github.com/roger-castaldo/JetFlow/actions/workflows/github-code-scanning/codeql/badge.svg)](https://github.com/roger-castaldo/JetFlow/actions/workflows/github-code-scanning/codeql)
DotNetCore workflows on NATS

The basis of this project is to allow the execution of workflows in both a distributed and maintained manner similar to temporal io but purely using Nats and specifically JetStream on top.

Eventually this service will include:
* An observation library that allows for querying and monitoring to be implemented by others
* An observability system wrapped around the querying and monitoring library that includes a UI, APIs and ease of placing additional data into an external database for more complete archiving
* ~~Support built in for Open Telemetry tracking and shared spans~~
* ~~Support for custom archive and cleanup configurations~~
* Examples of how to use this
* Full documentation
* ~~Unit Testing~~
* ~~Built in Retry/Circuit Break policies~~
* ~~Workflow Activity execution timeouts~~
* ~~Scheduled Workflows (Delayed start/Repeate Scheduling)~~
* Allow for archive TTL to be set on a per workflow basis
* Allow for multiple serializer types to be used (Json, Protobuf, etc)