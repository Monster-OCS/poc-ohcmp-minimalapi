using poc_ohcmp_minimalapi.Services;
using Xunit;

namespace poc_ohcmp_minimalapi.Tests;

public class WeatherComposerTest
{
    //for test with NUnit see https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-nunit
    [Fact]
    public void GetForecast_NoInput_Returns()
    {
        var watherCmp = new WeatherComposer();

        var result = watherCmp.GetForecast();

        Assert.NotEmpty(result);
    }
}