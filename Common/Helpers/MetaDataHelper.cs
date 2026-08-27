using NATS.Client.Core;

namespace JetFlow.Helpers;

internal static class MetaDataHelper
{
    private const string HeaderBase = "x-jetflow-metadata-";

    public static NatsHeaders EncodeMetaData(Dictionary<string, string[]>? metaData, NatsHeaders headers)
    {
        if (metaData==null || metaData.Count==0)
            return headers;
        foreach(var item in metaData)
            headers.Add($"{HeaderBase}{item.Key}", item.Value);
        return headers;
    }

    public static Dictionary<string, string[]>? ExtractMetaData(NatsHeaders? headers)
    {
        if (headers==null || headers.Count==0)
            return null;
        var result = headers
            .Where(pair => pair.Key.StartsWith(HeaderBase, StringComparison.InvariantCultureIgnoreCase))
            .Select(pair => new KeyValuePair<string, string[]>(pair.Key[HeaderBase.Length..], pair.Value.OfType<string>().ToArray()?? []))
            .Where(pair => pair.Value.Length>0)
            .ToDictionary();
        return (result.Count==0) ? null : result;
    }
}
