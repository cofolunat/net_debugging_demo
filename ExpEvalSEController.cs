using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("side-effect/getter")]
public sealed class ExpEvalSEController : ControllerBase
{
    private int i;

    public int I
    {
        get
        {
            Interlocked.Increment(ref i);
            return Volatile.Read(ref i);
        }
    }

    [HttpGet]
    public ActionResult<DeadLockSEResponse> Get()
    {
        var iBeforeExpEval = Volatile.Read(ref i);

        return Ok(new ExpEvalSEResponse( // здесь breakpoint и watch I и watch I,nse
            iBeforeExpEval,
            Volatile.Read(ref i)));
    }
}

public sealed record ExpEvalSEResponse(
    int iBeforeExpEval,
    int iAfterExpEval);
