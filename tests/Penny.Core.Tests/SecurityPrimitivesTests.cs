using Penny.Security;
using Xunit;

namespace Penny.Core.Tests;

public class DeviceIdGeneratorTests
{
    [Fact]
    public void Generate_ProducesWellFormedId()
    {
        var id = DeviceIdGenerator.Generate();
        Assert.True(DeviceIdGenerator.IsWellFormed(id));
        Assert.Equal(11, id.Length); // "###-###-###"
    }

    [Fact]
    public void Generate_IsNotConstant()
    {
        var ids = Enumerable.Range(0, 50).Select(_ => DeviceIdGenerator.Generate()).ToHashSet();
        // With ~30 bits of entropy, 50 draws colliding would be astronomically unlikely.
        Assert.True(ids.Count > 45);
    }

    [Theory]
    [InlineData("583-921-447", true)]
    [InlineData("583921447", false)]
    [InlineData("58-921-447", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsWellFormed_ValidatesFormat(string? candidate, bool expected)
    {
        Assert.Equal(expected, DeviceIdGenerator.IsWellFormed(candidate!));
    }
}

public class PinGeneratorTests
{
    [Fact]
    public void GenerateNew_Produces6DigitPin()
    {
        var generator = new PinGenerator();
        var pin = generator.GenerateNew();
        Assert.Equal(6, pin.Value.Length);
        Assert.True(pin.Value.All(char.IsDigit));
    }

    [Fact]
    public void Matches_ReturnsTrueForCorrectPin_FalseForWrongPin()
    {
        var generator = new PinGenerator();
        var pin = generator.GenerateNew();

        Assert.True(pin.Matches(pin.Value));
        Assert.False(pin.Matches("000000" == pin.Value ? "111111" : "000000"));
    }

    [Fact]
    public void IsExpired_TrueAfterValidityWindow()
    {
        var generator = new PinGenerator(TimeSpan.FromMilliseconds(1));
        var pin = generator.GenerateNew();
        Thread.Sleep(10);
        Assert.True(pin.IsExpired());
    }

    [Fact]
    public void IsExpired_FalseWithinValidityWindow()
    {
        var generator = new PinGenerator(TimeSpan.FromMinutes(5));
        var pin = generator.GenerateNew();
        Assert.False(pin.IsExpired());
    }
}

public class ConnectionAttemptLimiterTests
{
    [Fact]
    public void IsAllowed_FalseAfterMaxFailures()
    {
        var limiter = new ConnectionAttemptLimiter { MaxAttempts = 3, Window = TimeSpan.FromMinutes(1) };
        const string key = "10.0.0.5:1234";

        Assert.True(limiter.IsAllowed(key));
        limiter.RecordFailure(key);
        limiter.RecordFailure(key);
        limiter.RecordFailure(key);

        Assert.False(limiter.IsAllowed(key));
    }

    [Fact]
    public void Reset_ClearsFailureHistory()
    {
        var limiter = new ConnectionAttemptLimiter { MaxAttempts = 1, Window = TimeSpan.FromMinutes(1) };
        const string key = "10.0.0.6:1234";

        limiter.RecordFailure(key);
        Assert.False(limiter.IsAllowed(key));

        limiter.Reset(key);
        Assert.True(limiter.IsAllowed(key));
    }
}
