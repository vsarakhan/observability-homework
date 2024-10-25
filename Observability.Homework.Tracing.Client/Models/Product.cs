namespace Observability.Homework.Models;

public class Product
{
    public Guid Id { get; } = Guid.NewGuid();
    
    public required ProductType Type { get; set; }
}

public enum ProductType
{
    Pizza = 0,
    Desert = 1,
    Beverage = 2
}