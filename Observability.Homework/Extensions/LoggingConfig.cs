using System.Globalization;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;

namespace Observability.Homework.Extensions;

public static class LoggingConfig
{
    public static void AppLogging(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Host.UseSerilog(
            (context, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration);


                if (builder.Environment.EnvironmentName == "Development")
                    configuration
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                        .WriteTo.Console(formatProvider: CultureInfo.DefaultThreadCurrentCulture,
                            outputTemplate:
                            "[{Level:u3} {Timestamp:HH:mm:ss} {ScopePath}] {ClientId} {Message:lj}{NewLine}{Exception}");

                else
                    configuration
                        .MinimumLevel.Warning()
                        .WriteTo.Async(
                            to => to.Console(
                                new ExceptionAsObjectJsonFormatter(
                                    renderMessage: true,
                                    inlineFields: true),
                                standardErrorFromLevel: LogEventLevel.Warning));
            }
        );
    }
}