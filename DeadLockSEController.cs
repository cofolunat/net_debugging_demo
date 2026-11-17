using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("side-effect/deadlock")]
public sealed class DeadLockSEController : ControllerBase
{
    private readonly CounterWithLock counterWithLock;

    public DeadLockSEController(CounterWithLock counter)
    {
        counterWithLock = counter;
    }

    [HttpGet]
    public ActionResult<DeadLockSEResponse> Get()
    {
        using var curLock = counterWithLock.GetLock();
        var curCounter = counterWithLock.Counter;
        return Ok(new DeadLockSEResponse(curCounter));
    }

    [HttpGet("status")]
    public ActionResult<DeadLockSEResponse> GetStatus()
    {
        return Ok(new DeadLockSEResponse(counterWithLock.Counter));
    }
}

public sealed record DeadLockSEResponse(int counter);
