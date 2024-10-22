/*
 * У вас есть сервис, который имеет один Post эндпоинт /order. Через него можно для клиента оформить заказ на одну позицию.
 * Если заказ - пицца, то она делается. В коде можно посмотреть флоу. Если отменить запрос во время приготовления, то это равносильно отказу от заказа.
 * В случае отказа пицца выбрасывается. Кроме того, все пиццы на одном из этапов попадают в "печь". Будем считать, что печь вмещает бесконечное количество пицц.
 *
 *  1. Необходимо затащить в проект подключение трейсов, метрик, логов. Трейсы должны отправляться в Егерь, метрики - в Прометей, логи - с помощью Serilog в консоль.
 *  1.1 Сделать так, чтобы логер в релизной сборке писал json, а в дебаг сборке писал обычный текст в консоль.
 *  1.2* Сделать так, чтобы в дебаге в логе поменялись местами timestamp и level
 *  1.3* Сделать так, чтобы родительский спан запроса обогащался местным временем (можно взять время вашего компьютера)
 *
 *  2. Организовать разумное логирование в проекте. Не стоит упарываться в trace и debug.
 *  2.1 Настроить, чтобы в релизе минимальный уровень логов был Warning, а в дебаге - Information.
 *  2.2 Сделать так, чтобы все логи внутри PizzaBakeryService содержали информацию о клиенте, при этом клиент нельзя изменять контракт сервиса, прокидывать в него клиента.
 *  2.3** Сделать так, чтобы логи, которые ниже минимального уровня логирования, не выделяли лишнюю память.
 *
 *  3. Внедрить трейсы в процесс
 *  3.1 Проверить трейсы в Егере
 *  3.2 Сделать отдельный проект-сервис (как на уроке), который будет дергать АПИ. Сделать так, чтобы в Егере в трейсах был сквозной трейс на два сервиса.
 *
 *  4. Внедрить метрики в эндпоинт.
 *  4.1 Метрика заказов по типу. Указать в тегах клиента.
 *  4.2 Метрика времени приготовления пицц.
 *  4.3 Метрика отмен заказа.
 *  4.4 Метрика по подгоревшим пиццам.
 *  4.5 Метрика "текущее количество пицц в печи". Не использовать counter и upDownCounter
 *  4.6** Построить графики по этим метрикам в графане. Для этого вам нужен язык запросов PromQl. Советую использовать chatGPT, он очень хорошо генерирует запросы PromQL
 */

using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Observability.Homework.Extensions;
using Observability.Homework.Jobs;
using Observability.Homework.Models;
using Observability.Homework.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string serviceName = "Observability.Homework";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tpb =>
    {
        tpb
            .AddSource(serviceName)
            .SetResourceBuilder(ResourceBuilder
                .CreateDefault()
                .AddService(serviceName: serviceName))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.EnrichWithHttpRequest = (activity, _) =>
                {
                    activity.SetTag("timestamp", DateTime.Now.ToString("s", CultureInfo.InvariantCulture));
                };

                options.Filter = context => context.Request.Path.Value?.Contains("/metrics") != true;
            })
            .AddJaegerExporter();
    })
    .WithMetrics(mpb =>
    {
        mpb
            .AddMeter(OrderMetrics.MeterName)
            .AddMeter(PizzeriaMetrics.MeterName)
            .AddMeter(OvenMetrics.MeterName)
            .AddPrometheusExporter();
    });

builder.Logging.AddAppLogging(builder.Environment);

builder.Services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));
builder.Services.AddSingleton<IPizzaBakeryService, PizzaBakeryService>();
builder.Services.AddSingleton<OrderMetrics>();
builder.Services.AddSingleton<PizzeriaMetrics>();
builder.Services.AddSingleton<OvenMetrics>();

builder.Services.AddHostedService<ProductsInOvenCollector>();

var app = builder.Build();

app.MapPrometheusScrapingEndpoint();

app.MapPost("/order", async (
    [FromBody] Order order,
    IPizzaBakeryService pizzaBakeryService,
    ILogger<Program> logger,
    OrderMetrics orderMetrics,
    CancellationToken cancellationToken) =>
{
    using var _ = logger.BeginScope(new Dictionary<string, object> { { "clientId", order.Client.Id } });

    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("Order with product {@product} has been pushed", order.Product);
    }

    orderMetrics.OrderPushed(order);

    if (order.Product.Type is ProductType.Pizza)
        await pizzaBakeryService.DoPizza(order.Product, cancellationToken);

    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("Order with product {@product} has been completed", order.Product);
    }

    return Results.Ok(order.Product);
});

app.Run();
