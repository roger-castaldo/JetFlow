using JetFlow.UI;
using JetFlow.UI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();

await builder.Services.RegisterJetflowUIAsync(
        new() { Url = builder.Configuration.GetValue<string>("nats_Uri")!},
        new PostgresqlDbConnection(builder.Configuration.GetConnectionString("jetflow")!)
    );

var app = builder.Build();

app.UseHttpsRedirection()
    .UseRouting()
    .UseEndpoints(configure =>
        configure.RegisterJetflowUIEndpoints()
    );

app.Run();