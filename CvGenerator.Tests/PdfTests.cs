using System.Text;

using CvGenerator.Model;
using CvGenerator.Pdf;

namespace CvGenerator.Tests;

public class PdfTests
{
    [Fact]
    public void RendersACvWithoutExternalTools()
    {
        var bytes = RenderToBytes(
            """
            name = "Ada Lovelace"
            headline = "Mathematician"
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

            [[skills]]
            name = "Mathematics"
            items = ["Analysis", "Notation"]

            [[education]]
            name = "BSc"
            institution = "Somewhere"
            dates = "1840"

            [[additional]]
            items = ["Fluent in French"]
            """);

        Assert.Equal("%PDF-"u8.ToArray(), bytes[..5]);
        Assert.True(bytes.Length > 1000);
    }

    [Fact]
    public void RendersACoverLetter()
    {
        var bytes = RenderToBytes(
            """
            type = "cover-letter"
            name = "Ada Lovelace"
            date = "10 May 2026"
            subject = "Application"
            recipient = ["Hiring Manager", "  "]
            body = ["First paragraph.", "Second paragraph."]
            salutation = "Dear Hiring Manager,"
            sign_off = "Yours sincerely,"
            """);

        Assert.Equal("%PDF-"u8.ToArray(), bytes[..5]);
        Assert.True(bytes.Length > 1000);
    }

    [Fact]
    public void RendersAnAlmostEmptyProfile()
    {
        // Every field is optional, so a bare profile must still produce a page
        // rather than throwing on a missing section.
        var bytes = RenderToBytes("name = \"Ada Lovelace\"");
        Assert.Equal("%PDF-"u8.ToArray(), bytes[..5]);
    }

    [Fact]
    public void EmbedsTheBundledSerifRatherThanAHostFont()
    {
        // The point of CvGenerator/Fonts/ is that one input lays out identically
        // anywhere. Assert on the subset font actually written into the PDF, not
        // on the files being present: a face that fails to register would
        // otherwise fall back to a host serif with nothing to show for it.
        var bytes = RenderToBytes(
            """
            name = "Ada Lovelace"
            summary = "Analytical engines."
            """);

        Assert.True(PdfFonts.HasEmbeddedFonts);

        // /BaseFont entries are plain ASCII in the page's font dictionary.
        var pdf = Encoding.Latin1.GetString(bytes);
        Assert.Contains("LibertinusSerif-Regular", pdf, StringComparison.Ordinal);
        Assert.Contains("LibertinusSerif-Bold", pdf, StringComparison.Ordinal);
    }

    private static byte[] RenderToBytes(string toml)
    {
        var path = Path.Combine(Path.GetTempPath(), $"cv-generator-{Guid.NewGuid():N}.pdf");
        try
        {
            PdfRenderer.Render(TomlLoader.Parse(toml), path);
            return File.ReadAllBytes(path);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
