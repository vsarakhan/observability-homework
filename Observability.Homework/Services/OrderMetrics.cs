using System.Diagnostics.Metrics;
using Observability.Homework.Models;

namespace Observability.Homework.Services;

public class OrderMetrics
{
    public const string MeterName = "Observability.Homework.Order";

    private const string OrderPushedMetricName = "order.pushed";
    private const string OrderCanceledMetricName = "order.canceled";

    private readonly Counter<int> _orderPushedCounter;
    private readonly Counter<int> _orderCanceledCounter;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _orderPushedCounter = meter.CreateCounter<int>(OrderPushedMetricName);
        _orderCanceledCounter = meter.CreateCounter<int>(OrderCanceledMetricName);
    }

    public void OrderPushed(Order order)
    {
        _orderPushedCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("product.type", order.Product.Type.ToString()),
            new("client.id", order.Client.Id),
        });
    }

    public void OrderCanceled()
    {
        _orderCanceledCounter.Add(1);
    }
}