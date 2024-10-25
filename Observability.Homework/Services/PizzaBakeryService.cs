using System.Collections.Concurrent;
using Observability.Homework.Exceptions;
using Observability.Homework.Models;
using OpenTelemetry.Trace;


namespace Observability.Homework.Services;

public interface IPizzaBakeryService
{
    Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default);
}

public class PizzaBakeryService(Tracer tracer, ILogger<PizzaBakeryService> logger, PizzeriaMetricsService pizzeriaMetricsService) : IPizzaBakeryService
{
    private readonly PizzeriaMetricsService _metrics = pizzeriaMetricsService;
    private readonly Tracer _tracer = tracer;
    private readonly ConcurrentDictionary<Guid, Product> _bake = new();

    public async Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("DoPizza id:{productId} type:{productType}",product.Id, product.Type);
        _metrics.ProductBakingCount(_bake.Count);
        using var span = tracer.StartActiveSpan("DoPizza");
        try
        {
            await MakePizza(product, cancellationToken);
            await BakePizza(product, cancellationToken);
            await PackPizza(product, cancellationToken);
            return product;
        }
        catch (OperationCanceledException ex)
        {
            _metrics.ProductCancel();
            logger.LogError("PizzaBakeryService cancel do pizza");
            span.SetStatus(Status.Error.WithDescription("PizzaBakeryService cancel do pizza"));
            span.RecordException(ex);
            DropPizza(product);
            throw;
        }
        catch (BurntPizzaException ex)
        {
            _metrics.ProductBurnt();
            logger.LogError("PizzaBakeryService burnt pizza");
            span.SetStatus(Status.Error.WithDescription("PizzaBakeryService burnt pizza"));
            span.RecordException(ex);
            return await DoPizza(product, cancellationToken);
        }
    }

    private async Task<Product> BakePizza(Product product, CancellationToken cancellationToken = default)
    {
        using var span = tracer.StartActiveSpan("BakePizza");
        logger.LogInformation("BakePizza id:{productId} type:{productType}",product.Id, product.Type);
       
        PushToBake(product);
        var bakeForSeconds = new Random().Next(3, 9);
        await Task.Delay(TimeSpan.FromSeconds(bakeForSeconds), cancellationToken);
        if (bakeForSeconds > 7)
        {
            DropPizza(product);
            throw new BurntPizzaException("The pizza is burnt");
        }
        return PopFromBake(product);
    }

    private async Task<Product> MakePizza(Product product, CancellationToken cancellationToken = default)
    {
        using var span = tracer.StartActiveSpan("MakePizza");
        logger.LogInformation("MakePizza id:{productId} type:{productType}",product.Id, product.Type);
        
        await Task.Delay(new Random().Next(1, 3) * 1000, cancellationToken);
        return product;
    }
    
    private async Task<Product> PackPizza(Product product, CancellationToken cancellationToken = default)
    {
        using var span = tracer.StartActiveSpan("PackPizza");
        logger.LogInformation("PackPizza id:{productId} type:{productType}",product.Id, product.Type);
        
        await Task.Delay(new Random().Next(1, 2) * 1000, cancellationToken);
        return product;
    }

    private void PushToBake(Product product)
    {
        using var span = tracer.StartActiveSpan("PushToBake");
        logger.LogInformation("PushToBake id:{productId} type:{productType}",product.Id, product.Type);
        
        _bake[product.Id] = product;
    }

    private Product PopFromBake(Product product)
    {
        using var span = tracer.StartActiveSpan("PopFromBake");
        logger.LogInformation("PopFromBake id:{productId} type:{productType}",product.Id, product.Type);
       
        _bake.Remove(product.Id, out var pizza);
        return pizza!; //пусть у нас всегда есть пицца
    }
    
    private void DropPizza(Product product)
    {
        using var span = tracer.StartActiveSpan("DropPizza");
        logger.LogInformation("DropPizza id:{productId} type:{productType}",product.Id, product.Type);
        
        _bake.Remove(product.Id, out _);
    }
}