using Tomlyn.Serialization;

namespace CvGenerator.Model;

/// <summary>
/// A parsed TOML profile. Every field is optional so a partially filled
/// profile still renders; missing values simply produce no output.
/// <para>
/// TOML keys are named explicitly rather than left to a naming policy, so the
/// accepted input format is readable straight from the type.
/// </para>
/// </summary>
internal sealed class Document
{
    [TomlPropertyName("type")]
    public string? DocType { get; set; }

    [TomlPropertyName("name")]
    public string Name { get; set; } = "";

    [TomlPropertyName("headline")]
    public string Headline { get; set; } = "";

    [TomlPropertyName("summary")]
    public string Summary { get; set; } = "";

    [TomlPropertyName("date")]
    public string Date { get; set; } = "";

    [TomlPropertyName("subject")]
    public string Subject { get; set; } = "";

    [TomlPropertyName("salutation")]
    public string Salutation { get; set; } = "";

    [TomlPropertyName("sign_off")]
    public string SignOff { get; set; } = "";

    [TomlPropertyName("recipient")]
    public List<string> Recipient { get; set; } = [];

    [TomlPropertyName("body")]
    public List<string> Body { get; set; } = [];

    [TomlPropertyName("contact")]
    public List<Contact> Contact { get; set; } = [];

    [TomlPropertyName("experience")]
    public List<Experience> Experience { get; set; } = [];

    [TomlPropertyName("skills")]
    public List<SkillGroup> Skills { get; set; } = [];

    [TomlPropertyName("education")]
    public List<Education> Education { get; set; } = [];

    [TomlPropertyName("additional")]
    public List<Group> Additional { get; set; } = [];

    public bool IsCoverLetter => (DocType ?? "cv") == "cover-letter";

    public string ContactLine()
    {
        var parts = new List<string>();
        foreach (var item in Contact)
        {
            var label = item.Label.Trim();
            var value = item.Value.Trim();
            if (value.Length == 0)
            {
                continue;
            }

            parts.Add(label.Length == 0 ? value : $"{label}: {value}");
        }

        return string.Join("  |  ", parts);
    }
}

internal sealed class Contact
{
    [TomlPropertyName("label")]
    public string Label { get; set; } = "";

    [TomlPropertyName("value")]
    public string Value { get; set; } = "";

    [TomlPropertyName("url")]
    public string Url { get; set; } = "";
}

internal sealed class Experience
{
    [TomlPropertyName("role")]
    public string Role { get; set; } = "";

    [TomlPropertyName("company")]
    public string Company { get; set; } = "";

    [TomlPropertyName("dates")]
    public string Dates { get; set; } = "";

    [TomlPropertyName("location")]
    public string Location { get; set; } = "";

    [TomlPropertyName("summary")]
    public string Summary { get; set; } = "";

    [TomlPropertyName("highlights")]
    public List<string> Highlights { get; set; } = [];

    [TomlPropertyName("technologies")]
    public List<string> Technologies { get; set; } = [];

    [TomlPropertyName("progression")]
    public List<Progression> Progression { get; set; } = [];
}

/// <summary>An earlier role at the same company, rendered under its parent entry.</summary>
internal sealed class Progression
{
    [TomlPropertyName("role")]
    public string Role { get; set; } = "";

    [TomlPropertyName("dates")]
    public string Dates { get; set; } = "";

    [TomlPropertyName("location")]
    public string Location { get; set; } = "";

    [TomlPropertyName("summary")]
    public string Summary { get; set; } = "";

    [TomlPropertyName("highlights")]
    public List<string> Highlights { get; set; } = [];

    [TomlPropertyName("technologies")]
    public List<string> Technologies { get; set; } = [];
}

internal sealed class SkillGroup
{
    [TomlPropertyName("name")]
    public string Name { get; set; } = "";

    [TomlPropertyName("items")]
    public List<string> Items { get; set; } = [];
}

internal sealed class Education
{
    [TomlPropertyName("name")]
    public string Name { get; set; } = "";

    [TomlPropertyName("institution")]
    public string Institution { get; set; } = "";

    [TomlPropertyName("dates")]
    public string Dates { get; set; } = "";

    [TomlPropertyName("detail")]
    public string Detail { get; set; } = "";
}

internal sealed class Group
{
    [TomlPropertyName("name")]
    public string? Name { get; set; }

    [TomlPropertyName("items")]
    public List<string> Items { get; set; } = [];
}

/// <summary>Shared text helpers used by both renderers.</summary>
internal static class Values
{
    /// <summary>Trim each value and drop the ones left empty.</summary>
    public static List<string> CleanLines(IReadOnlyList<string> values)
    {
        var cleaned = new List<string>(values.Count);
        foreach (var value in values)
        {
            var trimmed = value.Trim();
            if (trimmed.Length != 0)
            {
                cleaned.Add(trimmed);
            }
        }

        return cleaned;
    }

    public static string JoinItems(IReadOnlyList<string> values) =>
        string.Join(", ", CleanLines(values));

    /// <summary>Join non-empty parts with a separator, e.g. dates and location.</summary>
    public static string JoinParts(string separator, params string[] parts) =>
        string.Join(separator, CleanLines(parts));
}
