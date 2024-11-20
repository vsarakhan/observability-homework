using Serilog;
using Serilog.Formatting.Display;
using Serilog.Formatting.Elasticsearch;


namespace Observability.Homework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOurCustomLogging(this IServiceCollection services,
        ILoggingBuilder loggingBuilder, IHostEnvironment environment)
    {
        AddCustomLogging(loggingBuilder, environment);
        return services;
    }

    private static ILoggingBuilder AddCustomLogging(ILoggingBuilder loggingBuilder, IHostEnvironment environment)
    {
        loggingBuilder.ClearProviders();

        if (environment.IsProduction())
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Async(config => config.Console(new ExceptionAsObjectJsonFormatter(inlineFields: true)))
                .CreateLogger();

            loggingBuilder.AddSerilog();
            return loggingBuilder;
        }

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(new MessageTemplateTextFormatter(
                "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}(ClientId:{ClientId}){NewLine}{Exception}"))
            .CreateLogger();

        loggingBuilder.AddSerilog();
        return loggingBuilder;
    }
}
