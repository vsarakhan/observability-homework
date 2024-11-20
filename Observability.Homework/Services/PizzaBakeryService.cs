using System.Collections.Concurrent;
using System.Diagnostics;
using Observability.Homework.Exceptions;
using Observability.Homework.Models;
using OpenTelemetry.Trace;

namespace Observability.Homework.Services;

public interface IPizzaBakeryService
{
    Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default);
    void SomeMethodWithTracing();
}

public class PizzaBakeryService(Tracer tracer, ILogger<PizzaBakeryService> logger, PizzeriaMetrics metrics)
    : IPizzaBakeryService
{
    private readonly ConcurrentDictionary<Guid, Product> _bake = new();

    public async Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Start preparing pizza");
        using var span = tracer.StartActiveSpan("DoPizza");
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await MakePizza(product, cancellationToken);
            await BakePizza(product, cancellationToken);
            stopwatch.Stop();
            metrics.RecordProductCookingTime(product, stopwatch.ElapsedMilliseconds);

            await PackPizza(product, cancellationToken);
            return product;
        }
        catch (OperationCanceledException ex)
        {
            span.SetStatus(Status.Error.WithDescription(ex.Message));
            span.RecordException(ex);
            DropPizza(product);
            throw;
        }
        catch (BurntPizzaException ex)
        {
            span.SetStatus(Status.Error.WithDescription(ex.Message));
            span.RecordException(ex);
            logger.LogWarning("Burnt pizza");
            return await DoPizza(product, cancellationToken);
        }
    }

    public void SomeMethodWithTracing()
    {
        logger.LogInformation("SomeMethodWithTracing");
        using var span = tracer.StartActiveSpan("Some method with tracing");
    }

    private async Task<Product> BakePizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bake pizza");
        using var span = tracer.StartActiveSpan("BakePizza");
        PushToBake(product);
        var bakeForSeconds = new Random().Next(3, 9);
        await Task.Delay(TimeSpan.FromSeconds(bakeForSeconds), cancellationToken);
        if (bakeForSeconds > 7)
        {
            metrics.RecordBurntProduct(product);
            DropPizza(product);
            throw new BurntPizzaException("The pizza is burnt");
        }

        return PopFromBake(product);
    }

    private async Task<Product> MakePizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Make pizza");
        using var span = tracer.StartActiveSpan("MakePizza");
        await Task.Delay(new Random().Next(1, 3) * 1000, cancellationToken);
        return product;
    }

    private async Task<Product> PackPizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Pack pizza");
        using var span = tracer.StartActiveSpan("PackPizza");
        await Task.Delay(new Random().Next(1, 2) * 1000, cancellationToken);
        return product;
    }

    private void PushToBake(Product product)
    {
        _bake[product.Id] = product;
        metrics.RecordProductsCountFromOven(_bake.Count);
    }

    private Product PopFromBake(Product product)
    {
        _bake.Remove(product.Id, out var pizza);
        return pizza!; //пусть у нас всегда есть пицца
    }

    private void DropPizza(Product product)
    {
        logger.LogWarning("Drop pizza");
        using var span = tracer.StartActiveSpan("DropPizza");
        metrics.RecordOrderCancellation(product);
        _bake.Remove(product.Id, out _);
    }

}
