using JetFlow.UI;
using JetFlow.UI.Extensions;
using JetFlow.UI.Interfaces;
using Shared;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();

IDbConnection connection = new PostgresqlDbConnection(builder.Configuration.GetConnectionString("jetflow")!);
await connection.InitAsync();
await connection.RegisterNamespaceAsync(Constants.PrimaryNamespace);
await connection.RegisterNamespaceAsync(Constants.SecondayNamespace);
await connection.StoreWorkflowArgumentDefinitionAsync(Constants.PrimaryNamespace, "Place Order", JsonDocument.Parse(@"{
    ""$type"": ""object"",
    ""properties"": [
        {
            ""Key"": ""email"",
            ""Value"": {
                ""$type"": ""maskedString"",
                ""maskedType"": ""End""
            }
        },
        {
            ""Key"": ""firstName"",
            ""Value"": {
                ""$type"": ""string""
            }
        },
        {
            ""Key"": ""lastName"",
            ""Value"": {
                ""$type"": ""string""
            }
        },
        {
            ""Key"": ""paymentInformation"",
            ""Value"": {
                ""$type"": ""object"",
                ""properties"": [
                    {
                        ""Key"": ""cardNumber"",
                        ""Value"": {
                            ""$type"": ""maskedString""
                        }
                    },
                    {
                        ""Key"": ""firstName"",
                        ""Value"": {
                            ""$type"": ""string""
                        }
                    },
                    {
                        ""Key"": ""lastName"",
                        ""Value"": {
                            ""$type"": ""string""
                        }
                    }
                ]
            }
        }
    ]
}"));
await connection.StoreWorkflowArgumentDefinitionAsync(Constants.SecondayNamespace, "Place Order", JsonDocument.Parse(@"{
    ""$type"": ""object"",
    ""properties"": [
        {
            ""Key"": ""email"",
            ""Value"": {
                ""$type"": ""maskedString"",
                ""maskedType"": ""End""
            }
        },
        {
            ""Key"": ""firstName"",
            ""Value"": {
                ""$type"": ""string""
            }
        },
        {
            ""Key"": ""lastName"",
            ""Value"": {
                ""$type"": ""string""
            }
        },
        {
            ""Key"": ""paymentInformation"",
            ""Value"": {
                ""$type"": ""object"",
                ""properties"": [
                    {
                        ""Key"": ""cardNumber"",
                        ""Value"": {
                            ""$type"": ""maskedString""
                        }
                    },
                    {
                        ""Key"": ""firstName"",
                        ""Value"": {
                            ""$type"": ""string""
                        }
                    },
                    {
                        ""Key"": ""lastName"",
                        ""Value"": {
                            ""$type"": ""string""
                        }
                    }
                ]
            }
        }
    ]
}"));
await connection.StoreActivityArgumentDefinitionAsync(Constants.PrimaryNamespace, "Process Payment", JsonDocument.Parse(@"{
    ""$type"": ""object"",
    ""properties"": [
        {
            ""Key"": ""cardNumber"",
            ""Value"": {
                ""$type"": ""maskedString""
            }
        },
        {
            ""Key"": ""firstName"",
            ""Value"": {
                ""$type"": ""string""
            }
        },
        {
            ""Key"": ""lastName"",
            ""Value"": {
                ""$type"": ""string""
            }
        }
    ]
}"));
await connection.StoreActivityArgumentDefinitionAsync(Constants.PrimaryNamespace, "Refund Payment", JsonDocument.Parse(@"{
    ""$type"": ""object"",
    ""properties"": [
        {
            ""Key"": ""cardNumber"",
            ""Value"": {
                ""$type"": ""maskedString""
            }
        },
        {
            ""Key"": ""firstName"",
            ""Value"": {
                ""$type"": ""string""
            }
        },
        {
            ""Key"": ""lastName"",
            ""Value"": {
                ""$type"": ""string""
            }
        }
    ]
}"));
await connection.StoreActivityArgumentDefinitionAsync(Constants.SecondayNamespace, "Process Payment", JsonDocument.Parse(@"{
    ""$type"": ""object"",
    ""properties"": [
        {
            ""Key"": ""cardNumber"",
            ""Value"": {
                ""$type"": ""maskedString""
            }
        },
        {
            ""Key"": ""firstName"",
            ""Value"": {
                ""$type"": ""string""
            }
        },
        {
            ""Key"": ""lastName"",
            ""Value"": {
                ""$type"": ""string""
            }
        }
    ]
}"));
await connection.StoreActivityArgumentDefinitionAsync(Constants.SecondayNamespace, "Refund Payment", JsonDocument.Parse(@"{
    ""$type"": ""object"",
    ""properties"": [
        {
            ""Key"": ""cardNumber"",
            ""Value"": {
                ""$type"": ""maskedString""
            }
        },
        {
            ""Key"": ""firstName"",
            ""Value"": {
                ""$type"": ""string""
            }
        },
        {
            ""Key"": ""lastName"",
            ""Value"": {
                ""$type"": ""string""
            }
        }
    ]
}"));

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