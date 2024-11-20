namespace Observability.Homework.Middlewares;

public class RequestHeadersLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        Console.WriteLine("Request Headers:");
        foreach (var header in context.Request.Headers)
        {
            Console.WriteLine($"{header.Key}: {header.Value}");
        }

        await next(context);
    }
}
