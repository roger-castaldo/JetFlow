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
    private const string ContentEncodingHeader = "content-encoding";
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

    private async ValueTask<(byte[] data, NatsHeaders headers)> EncodeObjectAsync(object? input)
    {
        var headers = new NatsHeaders();
        var data = JsonSerializer.SerializeToUtf8Bytes(input, options);

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

    private async ValueTask<T?> DecodeObjectAsync<T>(INatsJSMsg<byte[]> msg)
    {
        if (msg.Data==null)
            return default;
        if (msg.Headers?.TryGetValue(ContentEncodingHeader, out var encoding)==false)
            encoding = JsonEncoding;
        using Stream input = encoding.ToString() switch
        {
            BrotliEncoding => new BrotliStream(new MemoryStream(msg.Data), CompressionMode.Decompress),
            GZipEncoding => new GZipStream(new MemoryStream(msg.Data), CompressionMode.Decompress),
            JsonEncoding => new MemoryStream(msg.Data),
            _ => throw new InvalidContentTypeException(encoding)
        };
        TraceHelper.AddMessageDecodedEvent(encoding!);
        return JsonSerializer.Deserialize<T>(input, options);
    }

    public ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput>(TInput? data)
        => EncodeObjectAsync(data);
    public ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput1, TInput2>(TInput1? input1, TInput2? input2)
        => EncodeObjectAsync(new object?[] { input1, input2 });
    public ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput1, TInput2, TInput3>(TInput1? input1, TInput2? input2, TInput3? input3)
        => EncodeObjectAsync(new object?[] { input1, input2, input3 });
    public ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput1, TInput2, TInput3, TInput4>(TInput1? input1, TInput2? input2, TInput3? input3, TInput4? input4)
        => EncodeObjectAsync(new object?[] { input1, input2, input3, input4 });

    public ValueTask<TInput?> DecodeAsync<TInput>(INatsJSMsg<byte[]> msg)
        => DecodeObjectAsync<TInput>(msg);
    public async ValueTask<(TInput1?, TInput2?)> DecodeAsync<TInput1, TInput2>(INatsJSMsg<byte[]> msg)
    {
        var array = await DecodeObjectAsync<JsonElement[]>(msg);
        if (array==null || array.Length < 2)
            return (default, default);
        return (
            JsonSerializer.Deserialize<TInput1>(array[0], options),
            JsonSerializer.Deserialize<TInput2>(array[1], options)
        );
    }

    public async ValueTask<(TInput1?, TInput2?, TInput3?)> DecodeAsync<TInput1, TInput2, TInput3>(INatsJSMsg<byte[]> msg)
    {
        var array = await DecodeObjectAsync<JsonElement[]>(msg);
        if (array==null || array.Length < 3)
            return (default, default, default);
        return (
            JsonSerializer.Deserialize<TInput1>(array[0], options),
            JsonSerializer.Deserialize<TInput2>(array[1], options),
            JsonSerializer.Deserialize<TInput3>(array[2], options)
        );
    }

    public async ValueTask<(TInput1?, TInput2?, TInput3?, TInput4?)> DecodeAsync<TInput1, TInput2, TInput3, TInput4>(INatsJSMsg<byte[]> msg)
    {
        var array = await DecodeObjectAsync<JsonElement[]>(msg);
        if (array==null || array.Length < 4)
            return (default, default, default, default);
        return (
            JsonSerializer.Deserialize<TInput1>(array[0], options),
            JsonSerializer.Deserialize<TInput2>(array[1], options),
            JsonSerializer.Deserialize<TInput3>(array[2], options),
            JsonSerializer.Deserialize<TInput4>(array[3], options)
        );
    }
}
