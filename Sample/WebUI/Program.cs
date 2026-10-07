using JetFlow.UI;
using JetFlow.UI.Extensions;
using JetFlow.UI.Interfaces;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();

IDbConnection connection = new PostgresqlDbConnection(builder.Configuration.GetConnectionString("jetflow")!);
await connection.InitAsync();
await connection.RegisterNamespaceAsync(Constants.PrimaryNamespace);
await connection.RegisterNamespaceAsync(Constants.SecondayNamespace);

await builder.Services
    .RegisterJetflowUIAsync(
        new() { Url = builder.Configuration.GetValue<string>("nats_Uri")!},
        connection
    );

var app = builder.Build();

app.UseHttpsRedirection()
    .UseRouting()
    .UseStaticFiles()
    .UseEndpoints(configure =>
        configure.RegisterJetflowUIEndpoints()
    );

app.Run();