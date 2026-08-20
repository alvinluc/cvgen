using System.IO.Compression;
using System.Text;

using CvGenerator.Docx;
using CvGenerator.Model;

namespace CvGenerator.Tests;

public class DocxTests
{
    [Fact]
    public void EscapesXmlSpecialCharacters() =>
        Assert.Equal("a &amp; b &lt; c &gt; d", DocxRenderer.XmlEscape("a & b < c > d"));

    [Fact]
    public void EmptyParagraphsAreDroppedUnlessExplicitlyStructural()
    {
        Assert.Equal("", DocxRenderer.Paragraph("", "Body"));
        Assert.Contains("w:val=\"DotBreak\"", DocxRenderer.EmptyParagraph("DotBreak"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Mobile", "", "phone")]
    [InlineData("Email", "", "email")]
    [InlineData("Location", "", "pin")]
    [InlineData("LinkedIn", "", "link")]
    [InlineData("Blog", "https://example.com", "link")]
    [InlineData("Pronouns", "", null)]
    public void ContactLabelsMapToIcons(string label, string url, string? expected) =>
        Assert.Equal(expected, DocxRenderer.ContactIcon(label, url));

    [Fact]
    public void EntryBlocksStackTitleCompanyAndIconedDates()
    {
        var block = DocxRenderer.EntryBlock("Lead", "Analytical Engine Co", "1843", "London");
        Assert.Contains("w:val=\"EntryTitle\"", block, StringComparison.Ordinal);
        Assert.Contains("w:val=\"Company\"", block, StringComparison.Ordinal);
        Assert.Contains("w:val=\"Meta\"", block, StringComparison.Ordinal);
        Assert.Contains("rIdIcon-calendar", block, StringComparison.Ordinal);
        Assert.Contains("rIdIcon-pin", block, StringComparison.Ordinal);

        // Missing pieces simply produce no line or icon.
        var sparse = DocxRenderer.EntryBlock("Lead", "", "", "");
        Assert.Contains("w:val=\"EntryTitle\"", sparse, StringComparison.Ordinal);
        Assert.DoesNotContain("w:val=\"Company\"", sparse, StringComparison.Ordinal);
        Assert.DoesNotContain("w:val=\"Meta\"", sparse, StringComparison.Ordinal);
    }

    [Fact]
    public void CvSplitsContentIntoTwoColumns()
    {
        var body = DocxRenderer.Cv(TomlLoader.Parse(Sample));

        Assert.Contains(">ADA LOVELACE<", body, StringComparison.Ordinal);
        Assert.Contains("<w:tbl>", body, StringComparison.Ordinal);
        Assert.Contains(">SUMMARY<", body, StringComparison.Ordinal);
        Assert.Contains(">EDUCATION &amp; CERTIFICATIONS<", body, StringComparison.Ordinal);
        Assert.Contains(">SKILLS<", body, StringComparison.Ordinal);
        Assert.Contains(">MATHEMATICS<", body, StringComparison.Ordinal);
        Assert.Contains("w:val=\"Skill\"", body, StringComparison.Ordinal);
        Assert.Contains(">Tools: <", body, StringComparison.Ordinal);
        Assert.Contains("w:val=\"Bullet\"", body, StringComparison.Ordinal);
        Assert.Contains("rIdIcon-email", body, StringComparison.Ordinal);
        Assert.Contains("<w:drawing>", body, StringComparison.Ordinal);

        // The sidebar cell comes after the main content in document order.
        Assert.True(body.IndexOf(">EDUCATION", StringComparison.Ordinal)
            < body.IndexOf(">SKILLS<", StringComparison.Ordinal));
    }

    [Fact]
    public void AtsVariantIsSingleColumnWithLabelledPlainText()
    {
        var body = DocxRenderer.CvAts(TomlLoader.Parse(Sample));

        Assert.DoesNotContain("<w:tbl>", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<w:drawing>", body, StringComparison.Ordinal);
        Assert.Contains("Email: ada@example.com", body, StringComparison.Ordinal);
        Assert.Contains(">Mathematics: <", body, StringComparison.Ordinal);
        // Skills come before education so parsers see keywords in role context first.
        Assert.True(body.IndexOf(">SKILLS<", StringComparison.Ordinal)
            < body.IndexOf(">EDUCATION", StringComparison.Ordinal));
        // Progression entries inherit the parent company.
        Assert.Contains(">Collaborator<", body, StringComparison.Ordinal);
    }

    [Fact]
    public void CoverLetterCarriesNoHeaderBlock()
    {
        var body = DocxRenderer.CoverLetter(TomlLoader.Parse(
            """
            type = "cover-letter"
            name = "Ada Lovelace"
            headline = "Mathematician"
            date = "10 May 2026"
            subject = "Application"
            sign_off = "Yours sincerely,"

            [[contact]]
            label = "Email"
            value = "ada@example.com"
            """));

        Assert.DoesNotContain("w:val=\"Name\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Mathematician", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ada@example.com", body, StringComparison.Ordinal);
        Assert.StartsWith(DocxRenderer.Paragraph("10 May 2026", "Body"), body, StringComparison.Ordinal);
        Assert.Contains("w:val=\"Subject\"", body, StringComparison.Ordinal);
        // Letters carry no icons.
        Assert.DoesNotContain("<w:drawing>", body, StringComparison.Ordinal);
        // Room for a signature between the sign-off and the name.
        Assert.Contains("w:val=\"SignOff\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public void PackageContainsTheExpectedParts()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cv-generator-{Guid.NewGuid():N}.docx");
        try
        {
            DocxRenderer.Render(
                TomlLoader.Parse("type = \"cover-letter\"\nname = \"Ada Lovelace\""),
                path);

            using var archive = ZipFile.OpenRead(path);
            var names = archive.Entries.Select(entry => entry.FullName).ToHashSet(StringComparer.Ordinal);

            foreach (var part in new[]
            {
                "[Content_Types].xml",
                "_rels/.rels",
                "word/_rels/document.xml.rels",
                "word/document.xml",
                "word/styles.xml",
                "word/settings.xml",
                "word/fontTable.xml",
                "word/theme/theme1.xml",
                "word/media/icon-phone.png",
                "word/media/icon-calendar.png",
                "docProps/core.xml",
                "docProps/app.xml",
            })
            {
                Assert.Contains(part, names);
            }

            Assert.Contains("Ada Lovelace — Cover Letter", Read(archive, "docProps/core.xml"), StringComparison.Ordinal);

            // Word only loads parts reachable through explicit relationships, so
            // every part next to document.xml must be declared in its rels.
            var rels = Read(archive, "word/_rels/document.xml.rels");
            foreach (var target in new[] { "styles.xml", "settings.xml", "fontTable.xml", "theme/theme1.xml" })
            {
                Assert.Contains($"Target=\"{target}\"", rels, StringComparison.Ordinal);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string Read(ZipArchive archive, string name)
    {
        using var stream = archive.GetEntry(name)!.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private const string Sample = """
        name = "Ada Lovelace"
        summary = "Analytical engines."

        [[contact]]
        label = "Email"
        value = "ada@example.com"

        [[experience]]
        role = "Lead"
        company = "Analytical Engine Co"
        dates = "1843"
        location = "London"
        highlights = ["Wrote the first algorithm"]
        technologies = ["Punch cards"]

        [[experience.progression]]
        role = "Collaborator"
        dates = "1842"
        technologies = ["Notes"]

        [[skills]]
        name = "Mathematics"
        items = ["Analysis"]

        [[education]]
        name = "BSc"
        institution = "Somewhere"
        dates = "1840"

        [[additional]]
        items = ["Fluent in French"]
        """;
}
