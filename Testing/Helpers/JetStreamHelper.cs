using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Org.BouncyCastle.Crypto.Paddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetFlow.Testing.Helpers;

internal static class JetStreamHelper
{
    private const int MaxMessages = 64;
    public static async Task<IEnumerable<INatsJSMsg<byte[]>>> QueryStreamAsync(INatsJSContext jsContext, string streamName, bool headersOnly, params string[] filterSubjects)
    {
        var result = new List<INatsJSMsg<byte[]>>();
        var consumer = await jsContext.CreateOrUpdateConsumerAsync(
                streamName,
                new ConsumerConfig
                {
                    Name = Guid.NewGuid().ToString(), // ephemeral identity
                    DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                    AckPolicy = ConsumerConfigAckPolicy.None,
                    FilterSubjects = filterSubjects,
                    HeadersOnly = headersOnly,
                    InactiveThreshold = TimeSpan.FromSeconds(10)
                }
            );
        while (true)
        {
            var cnt = 0;
            await foreach (var msg in consumer.FetchAsync<byte[]>(new() { MaxMsgs = MaxMessages, Expires = TimeSpan.FromSeconds(1) }))
            {
                cnt++;
                result.Add(msg);
            }

            if (cnt!=MaxMessages)
            {
                // No messages in this fetch, end the query.
                break;
            }
        }
        try
        {
            await jsContext.DeleteConsumerAsync(consumer.Info.StreamName, consumer.Info.Name);
        }
        catch
        {
            //bury error
        }
        return result;
    }
}
