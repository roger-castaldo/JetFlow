using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AppHost.NUI;

public static class NuiBuilderExtensions
{
    public static IResourceBuilder<NatsServerResource> WithNui(
        this IResourceBuilder<NatsServerResource> builder,
        Action<IResourceBuilder<NuiResource>>? configure = null,
        string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        containerName ??= $"{builder.Resource.Name}-nui";

        var nuiResource = new NuiResource(containerName);

        var nuiBuilder = builder.ApplicationBuilder
            .AddResource(nuiResource)
            .WithImage(
                NuiContainerImageTags.Image,
                NuiContainerImageTags.Tag)
            .WithImageRegistry(NuiContainerImageTags.Registry)
            .WithHttpEndpoint(
                targetPort: NuiContainerImageTags.Port,
                name: NuiResource.HttpEndpointName)
            .WithArgs(
                "--nats-cli-contexts=/nats-cli-contexts")
            .WaitFor(builder)
            .WithRelationship(builder.Resource, "NUI")
            .ExcludeFromManifest();

        nuiBuilder.WithContainerFiles(
            "/nats-cli-contexts",
            async (context, cancellationToken) =>
            {
                var nats = builder.Resource;

                var username = await nats.UserNameReference
                    .GetValueAsync(cancellationToken);

                var password = nats.PasswordParameter is { } parameter
                    ? await parameter.GetValueAsync(cancellationToken)
                    : null;

                var connection = new NuiConnectionContext
                {
                    Description = nats.Name,
                    Url = $"nats://{nats.Name}:4222",
                    Username = username,
                    Password = password
                };

                return
                [
                    new ContainerFile
                    {
                        Name = $"{nats.Name}.json",
                        Contents = JsonSerializer.Serialize(connection)
                    }
                ];
            });

        configure?.Invoke(nuiBuilder);

        return builder;
    }

    public static IResourceBuilder<NuiResource> WithHostPort(
        this IResourceBuilder<NuiResource> builder,
        int? port)
    {
        return builder.WithEndpoint(
            NuiResource.HttpEndpointName,
            endpoint => endpoint.Port = port);
    }

    public static IResourceBuilder<NuiResource> WithDataVolume(
        this IResourceBuilder<NuiResource> builder,
        string? name = null)
    {
        return builder.WithVolume(
            name ?? $"{builder.Resource.Name}-data",
            "/db");
    }
}
