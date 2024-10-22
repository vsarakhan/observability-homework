using System.Collections.Concurrent;
using System.Diagnostics;
using Observability.Homework.Exceptions;
using Observability.Homework.Models;
using OpenTelemetry.Trace;

namespace Observability.Homework.Services;

public interface IPizzaBakeryService
{
    Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default);
    int GetProductCountInOven();
}

public class PizzaBakeryService(
    ILogger<PizzaBakeryService> logger,
    Tracer tracer,
    OrderMetrics orderMetrics,
    PizzeriaMetrics pizzeriaMetrics)
    : IPizzaBakeryService
{
    private readonly ConcurrentDictionary<Guid, Product> _bake = new();

    public async Task<Product> DoPizza(Product product, CancellationToken cancellationToken = default)
    {
        using var span = tracer.StartActiveSpan(nameof(DoPizza));

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Product {@product} has been started cooking", product);
        }

        try
        {
            var stopwatch = Stopwatch.StartNew();

            await MakePizza(product, cancellationToken);
            await BakePizza(product, cancellationToken);
            await PackPizza(product, cancellationToken);

            stopwatch.Stop();
            pizzeriaMetrics.RecordProductCooking(product, stopwatch.ElapsedMilliseconds);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Product {@product} has been finished cooking", product);
            }

            return product;
        }
        catch (OperationCanceledException)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError("Product {@product} cooking has been canceled", product);
            }

            orderMetrics.OrderCanceled();
            DropPizza(product);
            throw;
        }
        catch (BurntPizzaException)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Product {@product} has been burnt", product);
            }

            return await DoPizza(product, cancellationToken);
        }
    }

    public int GetProductCountInOven() => _bake.Count;

    private async Task<Product> BakePizza(Product product, CancellationToken cancellationToken = default)
    {
        using var span = tracer.StartActiveSpan(nameof(BakePizza));
        PushToBake(product);
        var bakeForSeconds = new Random().Next(3, 9);
        await Task.Delay(TimeSpan.FromSeconds(bakeForSeconds), cancellationToken);
        if (bakeForSeconds > 7)
        {
            pizzeriaMetrics.ProductBurnt(product);
            DropPizza(product);
            throw new BurntPizzaException("The pizza is burnt");
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Product with id {productId} has been baked", product.Id);
        }

        return PopFromBake(product);
    }

    private async Task<Product> MakePizza(Product product, CancellationToken cancellationToken = default)
    {
        using var span = tracer.StartActiveSpan(nameof(MakePizza));
        await Task.Delay(new Random().Next(1, 3) * 1000, cancellationToken);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Product with id {productId} has been made", product.Id);
        }

        return product;
    }

    private async Task<Product> PackPizza(Product product, CancellationToken cancellationToken = default)
    {
        using var span = tracer.StartActiveSpan(nameof(PackPizza));
        await Task.Delay(new Random().Next(1, 2) * 1000, cancellationToken);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Product with id {productId} has been packed", product.Id);
        }

        return product;
    }

    private void PushToBake(Product product)
    {
        _bake[product.Id] = product;

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Product with id {productId} has been pushed to bake", product.Id);
        }
    }

    private Product PopFromBake(Product product)
    {
        _bake.Remove(product.Id, out var pizza);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Product with id {productId} has been popped from bake", product.Id);
        }

        return pizza!; //пусть у нас всегда есть пицца
    }

    private void DropPizza(Product product)
    {
        _bake.Remove(product.Id, out _);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Product with id {productId} has been dropped", product.Id);
        }
    }
}