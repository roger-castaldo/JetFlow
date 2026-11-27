using Aspire.Hosting.ApplicationModel;

namespace AppHost.NUI;

public class NuiResource(string name) : ContainerResource(name)
{
    public const string HttpEndpointName = "http";

    public EndpointReference HttpEndpoint =>
        new(this, HttpEndpointName);
}
