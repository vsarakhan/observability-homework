using System.Collections.Concurrent;
using System.Diagnostics;
using Observability.Homework.Exceptions;
using Observability.Homework.Models;
using OpenTelemetry.Trace;

namespace Observability.Homework.Services;

public interface IPizzaBakeryService
{
    Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default);
}

public class PizzaBakeryService(
    ILogger<PizzaBakeryService> logger,
    MetricsService metricsService,
    Tracer tracer) : IPizzaBakeryService
{
    private readonly ConcurrentDictionary<Guid, Product> _bake = new();

    public async Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Start cooking: {productId}", product.Id);
        using var span = tracer.StartActiveSpan(nameof(DoPizza));
        try
        {
            var stopwatch = Stopwatch.StartNew();
            metricsService.IncreaseCookingProductsCount();

            await MakePizza(product, cancellationToken);
            await BakePizza(product, cancellationToken);
            await PackPizza(product, cancellationToken);

            stopwatch.Stop();
            metricsService.CookingTime(product, stopwatch.Elapsed.TotalSeconds);
            metricsService.DecreaseCookingProductsCount();
            logger.LogInformation("Finish cooking: {productId}", product.Id);

            return product;
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Cancel cooking: {productId}", product.Id);
            metricsService.OrderCanceled();

            DropPizza(product);
            throw;
        }
        catch (BurntPizzaException)
        {
            logger.LogError("Burnt product: {productId}", product.Id);
            metricsService.PizzaBurnt();
            return await DoPizza(product, cancellationToken);
        }
    }

    private async Task<Product> BakePizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Bake pizza: {productId}", product.Id);
        using var span = tracer.StartActiveSpan(nameof(BakePizza));

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
        logger.LogInformation("Make pizza: {productId}", product.Id);
        using var span = tracer.StartActiveSpan(nameof(MakePizza));

        await Task.Delay(new Random().Next(1, 3) * 1000, cancellationToken);
        return product;
    }
    
    private async Task<Product> PackPizza(Product product, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Pack pizza: {productId}", product.Id);
        using var span = tracer.StartActiveSpan(nameof(PackPizza));

        await Task.Delay(new Random().Next(1, 2) * 1000, cancellationToken);
        return product;
    }

    private void PushToBake(Product product)
    {
        _bake[product.Id] = product;
    }

    private Product PopFromBake(Product product)
    {
        _bake.Remove(product.Id, out var pizza);
        return pizza!; //пусть у нас всегда есть пицца
    }
    
    private void DropPizza(Product product)
    {
        using var span = tracer.StartActiveSpan(nameof(DropPizza));
        metricsService.DecreaseCookingProductsCount();

        _bake.Remove(product.Id, out _);
    }
}