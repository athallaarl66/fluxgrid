using System.Security.Cryptography;
using System.Text;

namespace FluxGrid.Api.Modules.HR.API;

public class QStashSignatureFilter : IEndpointFilter
{
    private readonly string _signingKey;

    public QStashSignatureFilter(IConfiguration config)
    {
        _signingKey = config["QStash:SigningKey"] ?? "";
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        if (string.IsNullOrEmpty(_signingKey))
            return Results.Problem("QStash signing key not configured", statusCode: 500);

        if (!ctx.HttpContext.Request.Headers.TryGetValue("Upstash-Signature", out var signature))
            return Results.Problem("Missing Upstash-Signature header", statusCode: 401);

        ctx.HttpContext.Request.EnableBuffering();
        var body = await new StreamReader(ctx.HttpContext.Request.Body).ReadToEndAsync();
        ctx.HttpContext.Request.Body.Position = 0;

        var expected = Convert.ToBase64String(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_signingKey),
            Encoding.UTF8.GetBytes(body)));

        if (!string.Equals(signature, expected, StringComparison.OrdinalIgnoreCase))
            return Results.Problem("Invalid QStash signature", statusCode: 401);

        return await next(ctx);
    }
}
