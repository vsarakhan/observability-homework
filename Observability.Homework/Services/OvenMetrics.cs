using System.Diagnostics.Metrics;

namespace Observability.Homework.Services;

public class OvenMetrics
{
    public const string MeterName = "Observability.Homework.Oven";

    private const string ProductCountInOvenMetricName = "oven.product.count";

    public OvenMetrics(IMeterFactory meterFactory, IPizzaBakeryService pizzaBakeryService)
    {
        var meter = meterFactory.Create(MeterName);
        meter.CreateObservableGauge(ProductCountInOvenMetricName, pizzaBakeryService.GetProductCountInOven);
    }
}