using JetFlow.Helpers;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace JetFlow.Testing.Helpers;

internal static class TestJetStreamHelper
{
    private const int MaxMessages = 64;
    public static async Task<IEnumerable<INatsJSMsg<byte[]>>> QueryStreamAsync(INatsJSContext jsContext, string streamName, bool headersOnly, params string[] filterSubjects)
    {
        var result = new List<INatsJSMsg<byte[]>>();
        await using var query = await JetStreamHelper.QueryStreamAsync(jsContext, streamName, headersOnly, filterSubjects);
        await foreach(var msg in query)
        {
            result.Add(msg);
        }
        return result;
    }
}
