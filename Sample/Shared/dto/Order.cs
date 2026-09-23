namespace Shared.dto;

public record OrderItem(Guid InventoryID, string Name, int Quantity, decimal Price);
public record OrderShipment(Address ShippingAddress, IEnumerable<OrderItem> Items);
public record Order(string Email, string FirstName, string LastName, Address ShippingAddress, PaymentRequest PaymentInformation, IEnumerable<OrderItem> Items)
    : OrderShipment(ShippingAddress, Items);
