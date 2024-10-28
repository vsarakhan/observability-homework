using Serilog;
using Serilog.Formatting.Elasticsearch;

namespace Observability.Homework.Extensions
{
    public static class SerilogConfig
    {
        public static void AddLogging(this WebApplicationBuilder builder)
        {
            builder.Logging.ClearProviders();
            builder.Host.UseSerilog(
                (context, configuration) =>
                {
                    if (builder.Environment.EnvironmentName == "Development")
                        configuration
                            .MinimumLevel.Information()
                            .WriteTo.Console(outputTemplate: "[{Level:u3} {Timestamp:HH:mm:ss} {ScopePath}] {ClientId} {Message:lj}{NewLine}{Exception}");
                    else
                        configuration
                            .MinimumLevel.Warning()
                            .WriteTo.Console(new ExceptionAsObjectJsonFormatter(
                                renderMessage: true,
                                inlineFields: true));
                }
            );
        }
    }
}
