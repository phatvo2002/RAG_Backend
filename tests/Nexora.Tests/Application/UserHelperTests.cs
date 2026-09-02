using Nexora.Application.Common.Helpers;

namespace Nexora.Tests.Application;

public sealed class UserHelperTests
{
    [Fact]
    public void NormalizeUserName_ShouldTrimAndCollapseSpaces()
    {
        var result = UserHelper.NormalizeUserName("  Võ   Đăng   Phát  ");

        Assert.Equal("Võ Đăng Phát", result);
    }

    [Theory]
    [InlineData("Võ Đăng Phát")]
    [InlineData("Nguyễn Thị Ánh")]
    [InlineData("Admin User")]
    public void IsValidUserName_ShouldAcceptUnicodeLettersAndSpaces(string userName)
    {
        Assert.True(UserHelper.IsValidUserName(userName));
    }

    [Theory]
    [InlineData("Võ Đăng Phát 123")]
    [InlineData("Vo_Dang_Phat")]
    [InlineData("Vo@Dang")]
    [InlineData("😀")]
    public void IsValidUserName_ShouldRejectNumbersAndSpecialCharacters(string userName)
    {
        Assert.False(UserHelper.IsValidUserName(userName));
    }

    [Fact]
    public void IsValidUserName_ShouldRejectEmptyUserName()
    {
        Assert.False(UserHelper.IsValidUserName("   "));
    }
}
