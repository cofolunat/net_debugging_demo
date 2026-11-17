using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("fibonacci")]
public sealed class FibonacciController : ControllerBase
{
    private readonly ILogger<FibonacciController> logger;

    public FibonacciController(ILogger<FibonacciController> logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public ActionResult<FibonacciResponse> Get(int? i)
    {
        if (i is null)
        {
            return BadRequest(new { error = "Требуется параметр 'i'." });
        }

        if (i is < 0 or > 92)
        {
            return BadRequest(new { error = "Параметр 'i' должен быть в диапазоне от 0 до 92 включительно." });
        }

        var result = Fibonacci(i.Value);
        logger.LogInformation("Число Фибоначчи для i={Input}: {Result}", i.Value, result);

        return Ok(new FibonacciResponse(i.Value, result));
    }

    private static long Fibonacci(int n)
    {
        long prev = 0;
        long cur = 1;

        for (var index = 0; index < n; index++)
        {
            (prev, cur) = (cur, prev + cur);
        }

        return prev;
    }
}

public sealed record FibonacciResponse(int i, long result);
