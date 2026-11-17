var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

var app = builder.Build();

app.MapGet("/test", (int? i, ILogger<Program> logger) =>
{
    if (i is null)
    {
        return Results.BadRequest(new { error = "Query parameter 'i' is required." });
    }

    if (i is < 0 or > 92)
    {
        return Results.BadRequest(new { error = "Query parameter 'i' must be between 0 and 92." });
    }

    var result = Fibonacci(i.Value);
    logger.LogInformation("Fibonacci calculated: i={Input}, result={Result}", i.Value, result);

    return Results.Ok(new FibonacciResponse(i.Value, result));
});

app.Run();

static long Fibonacci(int n)
{
    long previous = 0;
    long current = 1;

    for (var index = 0; index < n; index++)
    {
        (previous, current) = (current, previous + current);
    }

    return previous;
}

public sealed record FibonacciResponse(int Input, long Fibonacci);

public partial class Program;
