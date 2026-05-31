namespace Api.Idempotency;

internal static class IdempotencyKeyReader
{
    internal static Guid? Read(HttpContext ctx)
    {
        if (ctx.Request.Headers.TryGetValue("Idempotency-Key", out var values)
            && Guid.TryParse(values.FirstOrDefault(), out var key))
            return key;
        return null;
    }
}
