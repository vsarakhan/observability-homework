using System.Diagnostics.Metrics;
using Observability.Homework.Models;

namespace Observability.Homework.Services;

public class PizzeriaMetricsService
{
    public static readonly string MeterName = "Observability.Metrics.Pizzeria";

    private const string ProductTypeMetricName = "pizzeria.product.type";
    private const string ProductBurntMetricName = "pizzeria.product.burnt";
    private const string ProductCancelMetricName = "pizzeria.product.cancel";
    private const string ProductCookingTimeMetricName = "pizzeria.product.cooking.time";
    private const string ProductCookingCountMetricName = "pizzeria.product.cooking.count";
    
    private readonly Counter<int> _productTypeCounter;
    private readonly Counter<int> _productBurntCounter;
    private readonly Counter<int> _productCancelCounter; 
    private readonly Histogram<double> _productCookingTime; 
    private readonly Histogram<int> _productBakingCounter; 
    
    public PizzeriaMetricsService(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _productTypeCounter = meter.CreateCounter<int>(ProductTypeMetricName);
        _productBurntCounter = meter.CreateCounter<int>(ProductBurntMetricName);
        _productCancelCounter = meter.CreateCounter<int>(ProductCancelMetricName);
        _productCookingTime = meter.CreateHistogram<double>(ProductCookingTimeMetricName);
        _productBakingCounter = meter.CreateHistogram<int>(ProductCookingCountMetricName);
    }

    public void ProductType(Product product)
    {
        _productTypeCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("product.type", product.Type.ToString()),
        });
    }
    
    public void ProductBurnt()
    {
        _productBurntCounter.Add(1);
    }
    
    public void ProductCancel()
    {
        _productCancelCounter.Add(1);
    }
    
    public void RecordProductCooking(double cookingTime)
    {
        _productCookingTime.Record(cookingTime);
    }
    
    public void ProductBakingCount(int bakingCount)
    {
        _productBakingCounter.Record(bakingCount);
    }
}