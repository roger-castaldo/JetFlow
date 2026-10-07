using Bogus;
using JetFlow;
using Microsoft.Extensions.Configuration;
using Shared.dto;
using Shared.Workflows;
using System.Security.Cryptography;

var itemGenerator = new Faker<OrderItem>()
    .CustomInstantiator(f => new(
        Guid.NewGuid(),
        f.Commerce.ProductName(),
        f.Random.Int(1,5),
        f.Finance.Amount(1,100,2)
    ));

var orderGenerator = new Faker<Order>()
    .CustomInstantiator(f =>
    {
        var fname = f.Name.FirstName();
        var lname = f.Name.LastName();
        return new(
            f.Internet.Email(fname,lname),
            fname, 
            lname,
            new(
                $"{fname} {lname}",
                f.Address.StreetAddress(),
                null,
                f.Address.City(),
                f.Address.State(),
                f.Address.ZipCode(),
                f.Address.Country(),
                f.Phone.PhoneNumber()
            ),
            new(
                f.Finance.CreditCardNumber(),
                fname,
                lname
            ),
            itemGenerator.GenerateBetween(1,7)
        );
    });

var sourceCancel = new CancellationTokenSource();

Console.CancelKeyPress += delegate {
    sourceCancel.Cancel();
};

var config = (IConfiguration)(new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddUserSecrets(typeof(Program).Assembly)
    .Build());

var primaryConnection = await Connection.CreateInstanceAsync(new(new NATS.Client.Core.NatsOpts()
{
    Url = config.GetConnectionString("nats")!
})
{
    Namespace = Shared.Constants.PrimaryNamespace
});
var secondaryConnection = await Connection.CreateInstanceAsync(new(new NATS.Client.Core.NatsOpts()
{
    Url = config.GetConnectionString("nats")!
})
{
    Namespace = Shared.Constants.SecondayNamespace
});

var cron = new WorkflowScheduleBuilder()
    .MonthlyOnAt(15, 16, 30)
    .Build();

foreach(var order in orderGenerator.GenerateBetween(2, 10))
{
    await primaryConnection.ScheduleWorkflowAsync<IPlaceOrder, Order>(new(order), cron);
}

foreach(var order in orderGenerator.GenerateBetween(2, 10))
{
    await primaryConnection.DelayStartWorkflowAsync<IPlaceOrder, Order>(new(order), TimeSpan.FromMinutes(RandomNumberGenerator.GetInt32(30, 60*24)));
}

while (!sourceCancel.IsCancellationRequested)
{
    Guid id = Guid.Empty;
    foreach (var order in orderGenerator.GenerateBetween(2, 10))
    {
        id = await primaryConnection.StartWorkflowAsync<IPlaceOrder, Order>(new(order));
        Console.WriteLine($"Submitting order: {id} on {Shared.Constants.PrimaryNamespace}");
    }
    foreach (var order in orderGenerator.GenerateBetween(2, 10))
    {
        id = await secondaryConnection.StartWorkflowAsync<IPlaceOrder, Order>(new(order));
        Console.WriteLine($"Submitting order: {id} on {Shared.Constants.SecondayNamespace}");
    }
    try
    {
        await Task.Delay(TimeSpan.FromMinutes(1), sourceCancel.Token);
    }
    catch
    {
        break;
    }
}

await ((IAsyncDisposable)primaryConnection).DisposeAsync();
await ((IAsyncDisposable)secondaryConnection).DisposeAsync();