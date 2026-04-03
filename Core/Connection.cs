using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetFlow
{
    public static class Connection 
    {
        public static async ValueTask<IConnection> CreateInstanceAsync(NatsOpts options)
        {
            var result = new ConnectionInstance(options);
            return await result.OpenAsync();
        }
    }

    internal class ConnectionInstance : IConnection
    {
        private readonly NatsConnection connection;
        private readonly NatsJSContext jsContext;

        public ConnectionInstance(NatsOpts options)
        {
            connection = new(options);
            jsContext = new(connection);
        }

        public async ValueTask<IConnection> OpenAsync()
        {
            await connection.ConnectAsync();
            if (connection.ConnectionState != NatsConnectionState.Open)
                throw new UnableToConnectException();
            await jsContext.CreateOrUpdateStreamAsync(new(SubjectHelper.WorkflowEventsStreamsName, [
                SubjectHelper.WorkflowStart("*", "*"),
                SubjectHelper.WorkflowEnd("*", "*"),
                SubjectHelper.WorkflowStepStart("*", "*", "*"),
                SubjectHelper.WorkflowStepEnd("*", "*", "*"),
                SubjectHelper.WorkflowStepError("*", "*", "*"),
                SubjectHelper.WorkflowStepTimeout("*", "*", "*")
            ]));
            await jsContext.CreateOrUpdateStreamAsync(new(SubjectHelper.ActivityEventsStreamsName, [
                SubjectHelper.ActivityStart("*", "*"),
                SubjectHelper.ActivityTimer("*", "*")
            ])
            {
                AllowMsgSchedules = true,
                AllowMsgTTL = true
            });
            return this;
        }
    }
}
