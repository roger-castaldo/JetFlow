using JetFlow.Configs;
using JetFlow.Helpers;
using NATS.Client.Core;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JetFlow.Serializers;

internal class MessageSerializer
{
    private const int CompressionThreshold = 32 * 1024; // 32 KB
    private const string ContentEncodingHeader = "x-jetflow-content-encoding";
    private const string JsonEncoding = "application/json";
    private const string BrotliEncoding = "/brotli";
    private const string GZipEncoding = "/gzip";

    private static readonly Regex regContentEncoding = new($"^(?<contenttype>(?!(?:{BrotliEncoding}|{GZipEncoding})$).+?)(?<compression>{BrotliEncoding}|{GZipEncoding})?$", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500));

    private readonly JsonSerializerOptions options;
    private readonly CompressionTypes compressionType;

    public MessageSerializer(ConnectionOptions connectionOptions)
    {
        options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented=false,
            AllowTrailingCommas=true,
            PropertyNameCaseInsensitive=true,
            ReadCommentHandling=JsonCommentHandling.Skip,
            TypeInfoResolver = connectionOptions.JsonTypeInfoResolver
        };
        compressionType = connectionOptions.CompressionType;
    }

    private async ValueTask<T?> DecodeObjectAsync<T>(byte[]? data, NatsHeaders? headers)
    {
        if (data==null)
            return default;

        // Ensure encoding is always assigned to avoid CS8887.
        string encoding = JsonEncoding;
        if (headers != null && headers.TryGetValue(ContentEncodingHeader, out var headerVal))
            encoding = headerVal.ToString() ?? JsonEncoding;
        var match = regContentEncoding.Match(encoding);
        if (!match.Success)
            throw new InvalidContentTypeException(encoding);

        using Stream input = (match.Groups["compression"].Success ? match.Groups["compression"].Value : string.Empty).ToLowerInvariant() switch
        {
            BrotliEncoding => new BrotliStream(new MemoryStream(data), CompressionMode.Decompress),
            GZipEncoding => new GZipStream(new MemoryStream(data), CompressionMode.Decompress),
            _ => new MemoryStream(data)
        };

        if (match.Groups["contenttype"].Value.Equals(JsonEncoding, StringComparison.InvariantCultureIgnoreCase))
        {
            TraceHelper.AddMessageDecodedEvent(encoding);
            return JsonSerializer.Deserialize<T>(input, options);
        }

        throw new InvalidContentTypeException(encoding);
    }

    public async ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput>(TInput? input)
    {
        var headers = new NatsHeaders();
        var data = JsonSerializer.SerializeToUtf8Bytes<TInput?>(input, options);
        var encoding = JsonEncoding;

        if (data.Length >= CompressionThreshold)
        {
            using var output = new MemoryStream();
            if (Equals(compressionType, CompressionTypes.Brotli))
            {
                encoding+=BrotliEncoding;
                using var brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true);
                await brotli.WriteAsync(data);
            }
            else
            {
                encoding+=GZipEncoding;
                using var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true);
                await gzip.WriteAsync(data);
            }
            data=output.ToArray();
        }
        headers.Add(ContentEncodingHeader, encoding);
        return (data, headers);
    }

    public ValueTask<TInput?> DecodeAsync<TInput>(byte[]? data, NatsHeaders? headers)
        => DecodeObjectAsync<TInput>(data, headers);
    public ValueTask<object?> DecodeAsync(byte[]? data, NatsHeaders? headers)
        => DecodeObjectAsync<object>(data, headers);
}
