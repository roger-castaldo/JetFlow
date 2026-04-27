using JetFlow.Configs;
using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using System.IO.Compression;
using System.Text.Json;

namespace JetFlow;

internal class MessageSerializer
{
    private const int CompressionThreshold = 32 * 1024; // 32 KB
    private const string ContentEncodingHeader = "x-jetflow-content-encoding";
    private const string JsonEncoding = "application/json";
    private const string BrotliEncoding = "application/brotli";
    private const string GZipEncoding = "application/gzip";

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
        if (headers?.TryGetValue(ContentEncodingHeader, out var encoding)==false)
            encoding = JsonEncoding;
        using Stream input = encoding.ToString() switch
        {
            BrotliEncoding => new BrotliStream(new MemoryStream(data), CompressionMode.Decompress),
            GZipEncoding => new GZipStream(new MemoryStream(data), CompressionMode.Decompress),
            JsonEncoding => new MemoryStream(data),
            _ => throw new InvalidContentTypeException(encoding)
        };
        TraceHelper.AddMessageDecodedEvent(encoding!);
        return JsonSerializer.Deserialize<T>(input, options);
    }

    public async ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput>(TInput? input)
    {
        var headers = new NatsHeaders();
        var data = JsonSerializer.SerializeToUtf8Bytes<TInput?>(input, options);

        if (data.Length < CompressionThreshold)
        {
            headers.Add(ContentEncodingHeader, JsonEncoding);
            return (data, headers);
        }
        using var output = new MemoryStream();
        if (Equals(compressionType, CompressionTypes.Brotli))
        {
            headers.Add(ContentEncodingHeader, BrotliEncoding);
            using var brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true);
            await brotli.WriteAsync(data);
        }
        else
        {
            headers.Add(ContentEncodingHeader, GZipEncoding);
            using var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true);
            await gzip.WriteAsync(data);
        }
        return (output.ToArray(), headers);
    }

    public ValueTask<TInput?> DecodeAsync<TInput>(byte[]? data, NatsHeaders? headers)
        => DecodeObjectAsync<TInput>(data, headers);
    public ValueTask<object?> DecodeAsync(byte[]? data, NatsHeaders? headers)
        => DecodeObjectAsync<object>(data, headers);
}
