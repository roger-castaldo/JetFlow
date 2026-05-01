using JetFlow.Configs;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JetFlow.Testing.Serializers;

[TestClass]
public class MessageSerializerTests
{
    private const string ContentHeaderKey = "x-jetflow-content-encoding";
    private const string JsonContentHeader = "application/json";

    private record User(string FirstName, string LastName, string UserName);

    [TestMethod]
    public async Task EnsureBaseSerializationCallsOperateProperly()
    {
        //Arrange
        var messageSerializer = new MessageSerializer(new(new NATS.Client.Core.NatsOpts()));
        var testUser = new User(
            TestsHelper.GenerateRandomString(32),
            TestsHelper.GenerateRandomString(32),
            TestsHelper.GenerateRandomString(64)
        );

        //Act
        var (data, headers) = await messageSerializer.EncodeAsync<User>(testUser);
        var specificDecodeResult = await messageSerializer.DecodeAsync<User>(data, headers);
        var genericDecodeResult = await messageSerializer.DecodeAsync(data, headers);

        //Assert
        Assert.AreEqual(1, headers.Count);
        Assert.IsTrue(headers.TryGetValue(ContentHeaderKey, out var contentEncoding));
        Assert.AreEqual(JsonContentHeader, contentEncoding.ToString());
        Assert.AreEqual(testUser, specificDecodeResult);
        Assert.IsNotNull(genericDecodeResult);
        Assert.IsInstanceOfType(genericDecodeResult, typeof(JsonElement));
        var jsonElement = (JsonElement)genericDecodeResult;
        Assert.AreEqual(testUser.FirstName, jsonElement.GetProperty("firstName").GetString());
        Assert.AreEqual(testUser.LastName, jsonElement.GetProperty("lastName").GetString());
        Assert.AreEqual(testUser.UserName, jsonElement.GetProperty("userName").GetString());
    }

    [TestMethod]
    [DataRow(CompressionTypes.Brotli,"/brotli")]
    [DataRow(CompressionTypes.GZip,"/gzip")]
    public async Task EnsureCompressionTypesOperateProperly(CompressionTypes compressionType, string addedEncoding)
    {
        //Arrange
        var messageSerializer = new MessageSerializer(new(new NATS.Client.Core.NatsOpts())
        {
            CompressionType = compressionType
        });
        var stringLength = 32 * 1024 / 4;
        var testUser = new User(
            TestsHelper.GenerateRandomString(stringLength),
            TestsHelper.GenerateRandomString(stringLength),
            TestsHelper.GenerateRandomString(stringLength*2)
        );

        //Act
        var (data, headers) = await messageSerializer.EncodeAsync<User>(testUser);
        var specificDecodeResult = await messageSerializer.DecodeAsync<User>(data, headers);
        var genericDecodeResult = await messageSerializer.DecodeAsync(data, headers);

        //Assert
        Assert.AreEqual(1, headers.Count);
        Assert.IsTrue(headers.TryGetValue(ContentHeaderKey, out var contentEncoding));
        Assert.AreEqual($"{JsonContentHeader}{addedEncoding}", contentEncoding.ToString());
        Assert.AreEqual(testUser, specificDecodeResult);
        Assert.IsNotNull(genericDecodeResult);
        Assert.IsInstanceOfType(genericDecodeResult, typeof(JsonElement));
        var jsonElement = (JsonElement)genericDecodeResult;
        Assert.AreEqual(testUser.FirstName, jsonElement.GetProperty("firstName").GetString());
        Assert.AreEqual(testUser.LastName, jsonElement.GetProperty("lastName").GetString());
        Assert.AreEqual(testUser.UserName, jsonElement.GetProperty("userName").GetString());
    }

    [TestMethod]
    public async Task EnsureInvalidHeaderThrowsException()
    {
        //Arrange
        var messageSerializer = new MessageSerializer(new(new NATS.Client.Core.NatsOpts()));
        var headers = new NatsHeaders();
        headers.Add(ContentHeaderKey, "/brotli");
        var data = Array.Empty<byte>();

        //Act
        var exception = await Assert.ThrowsAsync<InvalidContentTypeException>(async()=>_ = await messageSerializer.DecodeAsync(data, headers));

        //Assert
        Assert.IsNotNull(exception);
        Assert.AreEqual($"Content type: {headers[ContentHeaderKey]} is unknown, unable to decode", exception.Message);

    }

    [TestMethod]
    public async Task EnsureUnknownEncoderThrowsError()
    {
        //Arrange
        var messageSerializer = new MessageSerializer(new(new NATS.Client.Core.NatsOpts()));
        var headers = new NatsHeaders();
        headers.Add(ContentHeaderKey, "application/binary");
        var data = Array.Empty<byte>();

        //Act
        var exception = await Assert.ThrowsAsync<InvalidContentTypeException>(async () => _ = await messageSerializer.DecodeAsync(data, headers));

        //Assert
        Assert.IsNotNull(exception);
        Assert.AreEqual($"Content type: {headers[ContentHeaderKey]} is unknown, unable to decode", exception.Message);

    }
}
