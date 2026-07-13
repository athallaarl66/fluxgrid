using FluxGrid.Api.Modules.HR.Application;

namespace FluxGrid.Api.Tests.HR;

public class GroqApiServiceTests
{
    [Fact]
    public void RedactPii_ReplacesEmail()
    {
        var result = GroqApiService.RedactPii("Contact me at john@example.com");
        Assert.Contains("[EMAIL]", result);
        Assert.DoesNotContain("john@example.com", result);
    }

    [Fact]
    public void RedactPii_ReplacesPhone()
    {
        var result = GroqApiService.RedactPii("Call 0812-3456-7890 for info");
        Assert.Contains("[PHONE]", result);
        Assert.DoesNotContain("0812-3456-7890", result);
    }

    [Fact]
    public void RedactPii_ReplacesInternationalPhone()
    {
        var result = GroqApiService.RedactPii("Phone: +62-812-3456-7890");
        Assert.Contains("[PHONE]", result);
    }

    [Fact]
    public void RedactPii_ReplacesAddress()
    {
        var result = GroqApiService.RedactPii("Jl. Sudirman No. 123, Jakarta");
        Assert.Contains("[ADDRESS]", result);
    }

    [Fact]
    public void RedactPii_HandlesMultiplePii()
    {
        var text = "Email: user@test.com, Phone: 08123456789, Jl. Merdeka";
        var result = GroqApiService.RedactPii(text);
        Assert.Contains("[EMAIL]", result);
        Assert.Contains("[PHONE]", result);
        Assert.Contains("[ADDRESS]", result);
    }

    [Fact]
    public void RedactPii_DoesNotModifyCleanText()
    {
        var text = "This is a normal CV text without any PII";
        var result = GroqApiService.RedactPii(text);
        Assert.Equal(text, result);
    }

    [Fact]
    public void TruncateToTokens_DoesNotTruncate_WhenWithinLimit()
    {
        var text = new string('x', 100);
        var result = GroqApiService.TruncateToTokens(text, 4000);
        Assert.Equal(text, result);
    }

    [Fact]
    public void TruncateToTokens_Truncates_WhenExceedsLimit()
    {
        var text = new string('x', 20000);
        var result = GroqApiService.TruncateToTokens(text, 4000);
        Assert.Equal(16000, result.Length);
    }

    [Fact]
    public void TruncateToTokens_ReturnsEmpty_WhenInputIsEmpty()
    {
        var result = GroqApiService.TruncateToTokens("", 4000);
        Assert.Empty(result);
    }

    [Fact]
    public void TruncateToTokens_ReturnsNull_WhenInputIsNull()
    {
        var result = GroqApiService.TruncateToTokens(null!, 4000);
        Assert.Null(result);
    }
}
