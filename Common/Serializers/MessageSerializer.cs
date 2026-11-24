using JetFlow.Configs;
using NATS.Client.Core;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JetFlow.Serializers;

internal class MessageSerializer
{
    private const int CompressionThreshold = 32 * 1024; // 32 KB
    private const string JsonEncoding = "application/json";
    private readonly JsonSerializerOptions options;
    private readonly CompressionTypes compressionType;
    private readonly Action<string> decodedEventCall;

    public MessageSerializer(CompressionTypes compressionType, IJsonTypeInfoResolver? jsonTypeInfoResolver, Action<string>? decodedEventCall = null)
    {
        options = Constants.JsonOptions;
        if (jsonTypeInfoResolver!=null)
            options.TypeInfoResolver = JsonTypeInfoResolver.Combine(jsonTypeInfoResolver, Constants.JsonOptions.TypeInfoResolver);
        this.compressionType = compressionType;
        this.decodedEventCall = decodedEventCall ?? ((val)=>{ });
    }

    private async ValueTask<T> DecodeObjectAsync<T>(byte[]? data, NatsHeaders? headers)
    {
        if (data==null || data.Length==0)
            return default;

        // Ensure encoding is always assigned to avoid CS8887.
        string encoding = JsonEncoding;
        if (headers != null && headers.TryGetValue(Constants.ContentEncodingHeader, out var headerVal))
            encoding = headerVal.ToString() ?? JsonEncoding;
        var match = RegexPatterns.ContentEncoding.Match(encoding);
        if (!match.Success)
            throw new InvalidContentTypeException(encoding);

        using Stream input = (match.Groups["compression"].Success ? match.Groups["compression"].Value : string.Empty).ToLowerInvariant() switch
        {
            RegexPatterns.BrotliEncoding => new BrotliStream(new MemoryStream(data), CompressionMode.Decompress),
            RegexPatterns.GZipEncoding => new GZipStream(new MemoryStream(data), CompressionMode.Decompress),
            _ => new MemoryStream(data)
        };

        if (match.Groups["contenttype"].Value.Equals(JsonEncoding, StringComparison.InvariantCultureIgnoreCase))
        {
            decodedEventCall(encoding);
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
                encoding+=RegexPatterns.BrotliEncoding;
                using var brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true);
                await brotli.WriteAsync(data);
            }
            else
            {
                encoding+=RegexPatterns.GZipEncoding;
                using var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true);
                await gzip.WriteAsync(data);
            }
            data=output.ToArray();
        }
        headers.Add(Constants.ContentEncodingHeader, encoding);
        return (data, headers);
    }

    public ValueTask<TInput> DecodeAsync<TInput>(byte[]? data, NatsHeaders? headers)
        => DecodeObjectAsync<TInput>(data, headers);
    public ValueTask<object?> DecodeAsync(byte[]? data, NatsHeaders? headers)
        => DecodeObjectAsync<object?>(data, headers);

}
