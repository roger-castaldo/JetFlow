using NATS.Client.Core;
using NATS.Client.JetStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace JetFlow.Configs
{
    public enum CompressionTypes
    {
        Brotli,
        GZip
    }

    public sealed class ConnectionOptions
    {
        public ConnectionOptions(NatsOpts options)
        {
            Connection = new NatsConnection(options);
            NatsJSContext = new NatsJSContext(Connection);
        }

        public ConnectionOptions(INatsConnection connection)
        {
            Connection = connection;
            NatsJSContext = new NatsJSContext(Connection);
        }

        public ConnectionOptions(INatsConnection connection, INatsJSContext jsContext)
        {
            Connection = connection;
            NatsJSContext = jsContext;
        }

        internal INatsConnection Connection { get; private init; }
        internal INatsJSContext NatsJSContext { get; private init; }
        public IJsonTypeInfoResolver? JsonTypeInfoResolver { get; init; }
        public CompressionTypes CompressionType { get; init; } = CompressionTypes.Brotli;
    }
}
