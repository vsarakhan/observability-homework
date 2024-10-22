using Serilog;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.SystemConsole.Themes;

namespace Observability.Homework.Extensions;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddAppLogging(this ILoggingBuilder loggingBuilder, IHostEnvironment environment)
    {
        loggingBuilder.ClearProviders();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(environment.IsProduction() ? LogEventLevel.Debug : LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Async(config =>
            {
                if (environment.IsProduction())
                {
                    config.Console(new ExceptionAsObjectJsonFormatter(inlineFields: true));
                }
                else
                {
                    config.Console(
                        outputTemplate: "[{Level:u3} {Timestamp:HH:mm:ss}] {Message:lj}{NewLine}{Exception}",
                        theme: AnsiConsoleTheme.Code);
                }
            })
            .CreateLogger();

        loggingBuilder.AddSerilog();

        return loggingBuilder;
    }
}