using Observability.Homework.Services;

namespace Observability.Homework.Jobs;

public class ProductsInOvenCollector(OvenMetrics ovenMetrics) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) => await Task.CompletedTask;
    public async Task StopAsync(CancellationToken cancellationToken) => await Task.CompletedTask;
}