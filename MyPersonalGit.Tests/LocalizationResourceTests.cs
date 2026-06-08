using System.Globalization;
using System.Resources;
using MyPersonalGit.Resources;

namespace MyPersonalGit.Tests;

/// <summary>
/// Verifies the translated .resx satellite assemblies actually build and resolve
/// per-culture (the data side of the Setup-wizard live language switcher).
/// </summary>
public class LocalizationResourceTests
{
    private static readonly ResourceManager Rm =
        new("MyPersonalGit.Resources.SharedResource", typeof(SharedResource).Assembly);

    [Theory]
    // Setup wizard heading across a Latin, an accented, and a CJK culture.
    [InlineData("Setup_Welcome", "en", "Welcome to MyPersonalGit")]
    [InlineData("Setup_Welcome", "es", "Bienvenido a MyPersonalGit")]
    [InlineData("Setup_Welcome", "de", "Willkommen bei MyPersonalGit")]
    [InlineData("Setup_Welcome", "ja", "MyPersonalGit へようこそ")]
    [InlineData("Setup_Welcome", "ru", "Добро пожаловать в MyPersonalGit")]
    // A validation message (used from C# code, not just markup).
    [InlineData("Setup_ErrPasswordMatch", "fr", "Les mots de passe ne correspondent pas.")]
    [InlineData("Setup_ErrPasswordMatch", "zh", "两次输入的密码不一致。")]
    // An a11y key from the earlier pass, to cover that round too.
    [InlineData("A11y_ToggleNav", "ko", "탐색 메뉴 전환")]
    public void Key_ResolvesToTranslation(string key, string culture, string expected)
        => Assert.Equal(expected, Rm.GetString(key, new CultureInfo(culture)));

    [Fact]
    public void MissingCultureTranslation_FallsBackToEnglishNeutral()
    {
        // An unsupported culture should fall back to the neutral (English) resource.
        var value = Rm.GetString("Setup_SiteName", new CultureInfo("nl"));
        Assert.Equal("Site name", value);
    }
}
