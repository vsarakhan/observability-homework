using System.Diagnostics.Metrics;
using Observability.Homework.Models;

namespace Observability.Homework.Services;

public class PizzeriaMetrics
{
    public static readonly string MeterName = "Observability.Metrics.Pizzeria";

    private const string ProductSoldMetricName = "pizzeria.product.sold";
    private const string ProductCookingTimeMetricName = "pizzeria.product.cooking.time";
    private const string ProductsInOvenMetricName = "pizzeria.product.oven.current.quantity";
    private const string OrderCancellationMetricName = "pizzeria.cancelled.order";
    private const string BurntProductMetricName = "pizzeria.burnt.product";

    private readonly Counter<int> _productSoldCounter;
    private readonly Gauge<int> _productsInOven;
    private readonly Histogram<double> _productCookingTime;
    private readonly Counter<int> _orderCancelledCounter;
    private readonly Counter<int> _burntProductCounter;

    public PizzeriaMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _productSoldCounter = meter.CreateCounter<int>(ProductSoldMetricName);
        _orderCancelledCounter = meter.CreateCounter<int>(OrderCancellationMetricName);
        _burntProductCounter = meter.CreateCounter<int>(BurntProductMetricName);
        _productCookingTime = meter.CreateHistogram<double>(ProductCookingTimeMetricName, "ms");
        _productsInOven = meter.CreateGauge<int>(ProductsInOvenMetricName);
    }

    public void ProductSoldByType(Product product, string clientId)
    {
        _productSoldCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("client.Id", clientId),
            new("product.type", product.Type.ToString()),
        });
    }

    public void RecordProductCookingTime(Product product, double cookingTime)
    {
        _productCookingTime.Record(cookingTime, new KeyValuePair<string, object?>[]
        {
            new("product.name", product.Id),
            new("product.type", product.Type.ToString()),
        });
    }

    public void RecordOrderCancellation(Product product)
    {
        _orderCancelledCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("product.name", product.Id),
            new("product.type", product.Type.ToString())
        });
    }

    public void RecordBurntProduct(Product product)
    {
        _burntProductCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("product.name", product.Id),
        });
    }

    public void RecordProductsCountFromOven(int bakeCount)
    {
       _productsInOven.Record(bakeCount);
    }
}
