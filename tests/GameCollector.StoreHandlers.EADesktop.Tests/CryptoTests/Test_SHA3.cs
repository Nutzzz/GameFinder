using System.Globalization;
using GameCollector.StoreHandlers.EADesktop.Crypto;

namespace GameCollector.StoreHandlers.EADesktop.Tests;

public partial class CryptoTests
{
    [Theory]
    [InlineData("allUsersGenericId", "530c11479fe252fc5aabc24935b9776d4900eb3ba58fdc271e0d6229413ad40e")]
    [InlineData("allUsersGenericIdIS", "84efc4b836119c20419398c3f3f2bcef6fc52f9d86c6e4e8756aec5a8279e492")]
    public void Test_SHA3(string input, string expectedOutput)
    {
        var actualOutput = Hashing.CalculateSHA3_256Hash(input);
        var hexOutput = Convert.ToHexString(actualOutput).ToLower(CultureInfo.InvariantCulture);
        hexOutput.Should().Be(expectedOutput);
    }
}
