using Bogus;
using JetFlow;
using Microsoft.Extensions.Configuration;
using Shared.dto;
using Shared.Workflows;

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

while (!sourceCancel.IsCancellationRequested)
{
    foreach (var order in orderGenerator.GenerateBetween(2, 10))
        await primaryConnection.StartWorkflowAsync<IPlaceOrder, Order>(new(order));
    foreach (var order in orderGenerator.GenerateBetween(2, 10))
        await secondaryConnection.StartWorkflowAsync<IPlaceOrder, Order>(new(order));
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