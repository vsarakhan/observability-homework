using System.Diagnostics.Metrics;
using Observability.Homework.Models;

namespace Observability.Homework.Services;

public class MetricsService
{
    public const string MetricsServiceName = "Observability.Homework.Metrics";
    private readonly Counter<int> _productTypeCounter;
    private readonly Histogram<double> _productCookingTimeCounter;
    private readonly Counter<int> _orderCanceledCounter;
    private readonly Counter<int> _burntPizzaCounter;
    private int _totalCategories = 0;
    public void IncreaseCookingProductsCount() => _totalCategories++;
    public void DecreaseCookingProductsCount() => _totalCategories--;

    public MetricsService(IMeterFactory meterFactory)
    {

        var meter = meterFactory.Create(MetricsServiceName);
        _productTypeCounter = meter.CreateCounter<int>("product.type");
        _orderCanceledCounter = meter.CreateCounter<int>("order.canceled");
        _burntPizzaCounter = meter.CreateCounter<int>("pizza.burnt");
        meter.CreateObservableGauge("product.currently.cooking", () => _totalCategories);
        _productCookingTimeCounter =  meter.CreateHistogram<double>("product.cookingTime", "s");
    }
    public void ProductType(Order order)
    {
        _productTypeCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("product.type", order.Product.Type.ToString()),
            new("client.id", order.Client.Id),
        });
    }
    public void CookingTime(Product product, double cookingTime)
    {
        _productCookingTimeCounter.Record(cookingTime, new KeyValuePair<string, object?>[]
        {
            new("product.type", product.Type.ToString()),
        });
    }
    public void OrderCanceled()
    {
        _orderCanceledCounter.Add(1);
    }
    public void PizzaBurnt()
    {
        _burntPizzaCounter.Add(1);
    }
}