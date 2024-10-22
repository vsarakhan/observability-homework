using System.Diagnostics.Metrics;
using Observability.Homework.Models;

namespace Observability.Homework.Services;

public class PizzeriaMetrics
{
    public const string MeterName = "Observability.Homework.Pizzeria";

    private const string ProductBurntMetricName = "pizzeria.product.burnt";
    private const string ProductCookingTimeMetricName = "pizzeria.product.cooking.time";

    private readonly Counter<int> _productBurntCounter;
    private readonly Histogram<double> _productCookingTimeHistogram;

    public PizzeriaMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _productBurntCounter = meter.CreateCounter<int>(ProductBurntMetricName);
        _productCookingTimeHistogram = meter.CreateHistogram<double>(ProductCookingTimeMetricName, "ms");
    }

    public void ProductBurnt(Product product)
    {
        _productBurntCounter.Add(1, GetTags(product));
    }

    public void RecordProductCooking(Product product, double cookingTime)
    {
        _productCookingTimeHistogram.Record(cookingTime, GetTags(product));
    }

    private KeyValuePair<string, object?>[] GetTags(Product product)
    {
        return
        [
            new("product.type", product.Type.ToString())
        ];
    }
}