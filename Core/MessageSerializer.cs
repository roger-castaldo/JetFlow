using NATS.Client.Core;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JetFlow;

internal class MessageSerializer
{
    private const int CompressionThreshold = 16 * 1024; // 16 KB

    private readonly JsonSerializerOptions options;

    public MessageSerializer(IJsonTypeInfoResolver? jsonContext)
    {
        options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented=false,
            AllowTrailingCommas=true,
            PropertyNameCaseInsensitive=true,
            ReadCommentHandling=JsonCommentHandling.Skip,
            TypeInfoResolver = jsonContext
        };
    }

    private async ValueTask<(byte[] data, NatsHeaders headers)> EncodeObjectAsync(object? input)
    {
        var headers = new NatsHeaders();
        var data = JsonSerializer.SerializeToUtf8Bytes(input, options);

        if (data.Length < CompressionThreshold)
            return (data, headers);

        headers["content-encoding"] = "br";
        // Compress only when large enough
        using var output = new MemoryStream();

        using (var brotli = new BrotliStream(
            output,
            CompressionLevel.Optimal,
            leaveOpen: true))
        {
            brotli.Write(data);
        }

        return (output.ToArray(), headers);
    }

    private async ValueTask<T?> DecodeObjectAsync<T>(byte[] data, NatsHeaders headers)
    {
        if (headers.TryGetValue("content-encoding", out var encoding) && encoding == "br")
        {
            using var input = new MemoryStream(data);
            using var brotli = new BrotliStream(input, CompressionMode.Decompress);
            return await JsonSerializer.DeserializeAsync<T>(brotli, options);
        }
        else
        {
            return JsonSerializer.Deserialize<T>(data, options);
        }
    }

    public ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput>(TInput data)
        => EncodeObjectAsync(data);
    public ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput1, TInput2>(TInput1 input1, TInput2 input2)
        => EncodeObjectAsync(new object?[] { input1, input2 });
    public ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput1, TInput2, TInput3>(TInput1 input1, TInput2 input2, TInput3 input3)
        => EncodeObjectAsync(new object?[] { input1, input2, input3 });
    public ValueTask<(byte[] data, NatsHeaders headers)> EncodeAsync<TInput1, TInput2, TInput3, TInput4>(TInput1 input1, TInput2 input2, TInput3 input3, TInput4 input4)
        => EncodeObjectAsync(new object?[] { input1, input2, input3, input4 });

    public ValueTask<TInput?> DecodeAsync<TInput>(byte[] data, NatsHeaders headers)
        => DecodeObjectAsync<TInput>(data, headers);
    public async ValueTask<(TInput1?, TInput2?)> DecodeAsync<TInput1, TInput2>(byte[] data, NatsHeaders headers)
    {
        var array = await DecodeObjectAsync<JsonElement[]>(data, headers);
        if (array==null || array.Length < 2)
            return (default, default);
        return (
            JsonSerializer.Deserialize<TInput1>(array[0], options),
            JsonSerializer.Deserialize<TInput2>(array[1], options)
        );
    }

    public async ValueTask<(TInput1?, TInput2?, TInput3?)> DecodeAsync<TInput1, TInput2, TInput3>(byte[] data, NatsHeaders headers)
    {
        var array = await DecodeObjectAsync<JsonElement[]>(data, headers);
        if (array==null || array.Length < 3)
            return (default, default, default);
        return (
            JsonSerializer.Deserialize<TInput1>(array[0], options),
            JsonSerializer.Deserialize<TInput2>(array[1], options),
            JsonSerializer.Deserialize<TInput3>(array[2], options)
        );
    }

    public async ValueTask<(TInput1?, TInput2?, TInput3?, TInput4?)> DecodeAsync<TInput1, TInput2, TInput3, TInput4>(byte[] data, NatsHeaders headers)
    {
        var array = await DecodeObjectAsync<JsonElement[]>(data, headers);
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
