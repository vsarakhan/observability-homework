namespace Observability.Homework.Models;

public class Order
{
    public required Client Client { get; set; }
    
    public required Product Product { get; set; }

    public static Order Create(ProductType type)
    {
        return new Order
        {
            Client = new()
            {
                Id = Guid.NewGuid().ToString()
            },
            Product = new Product
            {
                Type = type
            }
        };
    }
}