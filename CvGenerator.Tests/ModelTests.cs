using CvGenerator.Model;

namespace CvGenerator.Tests;

public class ModelTests
{
    [Fact]
    public void DefaultsToCvWhenTypeIsMissing()
    {
        Assert.False(TomlLoader.Parse("name = \"Ada\"").IsCoverLetter);
        Assert.True(TomlLoader.Parse("type = \"cover-letter\"").IsCoverLetter);
    }

    [Fact]
    public void UnknownKeysAreIgnored()
    {
        var parsed = TomlLoader.Parse("name = \"Ada\"\nnickname = \"Countess\"");
        Assert.Equal("Ada", parsed.Name);
    }

    [Fact]
    public void InvalidTomlIsReportedWithTheOrigin()
    {
        var error = Assert.Throws<GeneratorException>(() => TomlLoader.Parse("name = ", "ada.toml"));
        Assert.Contains("ada.toml", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ContactLineSkipsEntriesWithoutAValue()
    {
        var parsed = TomlLoader.Parse(
            """
            [[contact]]
            label = "Email"
            value = "ada@example.com"

            [[contact]]
            label = "Fax"
            value = "  "

            [[contact]]
            value = "London"
            """);

        Assert.Equal("Email: ada@example.com  |  London", parsed.ContactLine());
    }

    [Fact]
    public void NestedTablesAndArraysBind()
    {
        var parsed = TomlLoader.Parse(
            """
            [[experience]]
            role = "Lead"
            company = "Analytical Engine Co"
            highlights = ["First", "Second"]

            [[experience.progression]]
            role = "Collaborator"
            dates = "1842"
            """);

        var role = Assert.Single(parsed.Experience);
        Assert.Equal("Lead", role.Role);
        Assert.Equal(["First", "Second"], role.Highlights);
        Assert.Equal("Collaborator", Assert.Single(role.Progression).Role);
    }

    [Fact]
    public void HelpersTrimAndDropEmptyValues()
    {
        List<string> values = [" one ", "", "two"];
        Assert.Equal(["one", "two"], Values.CleanLines(values));
        Assert.Equal("one, two", Values.JoinItems(values));
        Assert.Equal("2019", Values.JoinParts(" · ", "2019", "  "));
        Assert.Equal("2019 · Remote", Values.JoinParts(" · ", "2019", "Remote"));
    }
}
