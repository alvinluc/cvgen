using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;

using CvGenerator.Model;

using static CvGenerator.Model.Values;

namespace CvGenerator.Docx;

/// <summary>
/// Writes a minimal, dependency-free WordprocessingML package. The markup is
/// assembled as text rather than through the Open XML SDK: the document has a
/// fixed shape, so a handful of string helpers cover it and the output stays
/// easy to diff.
/// </summary>
internal static class DocxRenderer
{
    // The monochrome palette of the double-column layout. Each colour is also
    // mapped to a theme slot (text1/text2/accent1/accent2) so Word's
    // Design > Colors menu can restyle the document.
    internal const string Black = "1A1A1A";
    internal const string Ink = "3D3D3D";
    internal const string Title = "2B2B2B";
    internal const string Muted = "7A7A7A";
    internal const string Rule = "BFBFBF";

    // A4 (11906 twips) minus the 720-twip side margins, split into a main
    // column and a sidebar; the gutter lives in the main cell's right margin.
    private const int ContentWidth = 10466;
    private const int MainWidth = 6800;
    private const int SideWidth = 3666;
    private const int Gutter = 420;

    // Inline icon sizes in EMUs (914400 per inch): 9.5pt beside contact text,
    // 9pt beside the smaller date/location lines.
    private const int IconContact = 120650;
    private const int IconMeta = 114300;

    /// <summary>The em space that separates runs on the contact and meta lines.</summary>
    private const string EmSpace = " ";

    private static readonly string[] IconNames = ["phone", "email", "pin", "link", "calendar"];

    /// <summary>
    /// DrawingML object ids must be unique within a document; a process-wide
    /// counter is the simplest way to guarantee that.
    /// </summary>
    private static int drawingId;

    /// <summary>Render a document to a styled, two-column DOCX.</summary>
    public static void Render(Document document, string outputPath) =>
        WritePackage(document, outputPath, ats: false);

    /// <summary>
    /// Render the ATS variant: a single column with no table, no icons and
    /// labelled contact details, maximising extraction fidelity in parsers.
    /// </summary>
    public static void RenderAts(Document document, string outputPath) =>
        WritePackage(document, outputPath, ats: true);

    private static void WritePackage(Document document, string outputPath, bool ats)
    {
        var body = document.IsCoverLetter
            ? CoverLetter(document)
            : ats
                ? CvAts(document)
                : Cv(document);

        var parent = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        try
        {
            WriteDocx(outputPath, body, document);
        }
        catch (IOException error)
        {
            throw new GeneratorException($"{outputPath}: {error.Message}", error);
        }
    }

    /// <summary>
    /// A full-width header, then a two-column table: profile, experience and
    /// education in the main column; skills and additional groups in the sidebar.
    /// </summary>
    internal static string Cv(Document document)
    {
        var body = new StringBuilder();
        body.Append(Paragraph(document.Name.ToUpperInvariant(), "Name"));
        body.Append(Paragraph(document.Headline, "Headline"));
        body.Append(ContactParagraph(document));

        var main = MainColumn(document);
        var side = SideColumn(document);
        if (side.Length == 0)
        {
            body.Append(main);
        }
        else
        {
            body.Append(TwoColumns(main, side));
            // Word expects a paragraph after a table that closes the body.
            body.Append(EmptyParagraph("Normal"));
        }

        return body.ToString();
    }

    private static string MainColumn(Document document)
    {
        var column = new StringBuilder();

        if (document.Summary.Length != 0)
        {
            column.Append(Heading("Summary"));
            column.Append(Paragraph(document.Summary, "Body"));
        }

        if (document.Experience.Count != 0)
        {
            column.Append(Heading("Experience"));
            var hasPreviousExperienceBlock = false;
            foreach (var role in document.Experience)
            {
                if (hasPreviousExperienceBlock)
                {
                    column.Append(EmptyParagraph("DotBreak"));
                }

                column.Append(EntryBlock(role.Role, role.Company, role.Dates, role.Location));
                column.Append(Paragraph(role.Summary, "Body"));
                foreach (var highlight in CleanLines(role.Highlights))
                {
                    column.Append(Bullet(highlight));
                }

                var tech = JoinItems(role.Technologies);
                if (tech.Length != 0)
                {
                    column.Append(Labelled("Meta", "Tools", tech));
                }

                hasPreviousExperienceBlock = true;
                foreach (var earlier in role.Progression)
                {
                    column.Append(EmptyParagraph("DotBreak"));
                    column.Append(EntryBlock(earlier.Role, role.Company, earlier.Dates, earlier.Location));
                    column.Append(Paragraph(earlier.Summary, "Body"));
                    foreach (var highlight in CleanLines(earlier.Highlights))
                    {
                        column.Append(Bullet(highlight));
                    }

                    var earlierTech = JoinItems(earlier.Technologies);
                    if (earlierTech.Length != 0)
                    {
                        column.Append(Labelled("Meta", "Tools", earlierTech));
                    }
                }
            }
        }

        if (document.Education.Count != 0)
        {
            column.Append(Heading("Education & Certifications"));
            for (var index = 0; index < document.Education.Count; index++)
            {
                var item = document.Education[index];
                if (index > 0)
                {
                    column.Append(EmptyParagraph("DotBreak"));
                }

                column.Append(EntryBlock(item.Name, item.Institution, item.Dates, ""));
                column.Append(Paragraph(item.Detail, "Body"));
            }
        }

        return column.ToString();
    }

    private static string SideColumn(Document document)
    {
        var column = new StringBuilder();

        var hasSkills = false;
        foreach (var group in document.Skills)
        {
            var items = CleanLines(group.Items);
            if (group.Name.Length == 0 || items.Count == 0)
            {
                continue;
            }

            if (!hasSkills)
            {
                column.Append(Heading("Skills"));
                hasSkills = true;
            }

            column.Append(Paragraph(group.Name.ToUpperInvariant(), "SkillGroup"));
            foreach (var item in items)
            {
                column.Append(Paragraph(item, "Skill"));
            }
        }

        foreach (var group in document.Additional)
        {
            var items = CleanLines(group.Items);
            if (items.Count == 0)
            {
                continue;
            }

            column.Append(Heading(group.Name ?? "Additional"));
            for (var index = 0; index < items.Count; index++)
            {
                if (index > 0)
                {
                    column.Append(EmptyParagraph("DotBreak"));
                }

                column.Append(Paragraph(items[index], "Body"));
            }
        }

        return column.ToString();
    }

    /// <summary>
    /// The ATS variant: one column, plain-text everything. Contact details keep
    /// their labels, date/location lines are plain text, sections run Summary →
    /// Experience → Skills → Education so parsers see keywords in role context
    /// before credentials.
    /// </summary>
    internal static string CvAts(Document document)
    {
        var body = new StringBuilder();
        body.Append(Paragraph(document.Name.ToUpperInvariant(), "Name"));
        body.Append(Paragraph(document.Headline, "Headline"));
        body.Append(Paragraph(document.ContactLine(), "ContactLine"));

        if (document.Summary.Length != 0)
        {
            body.Append(Heading("Summary"));
            body.Append(Paragraph(document.Summary, "Body"));
        }

        if (document.Experience.Count != 0)
        {
            body.Append(Heading("Experience"));
            foreach (var role in document.Experience)
            {
                body.Append(EntryBlockAts(role.Role, role.Company, role.Dates, role.Location));
                body.Append(Paragraph(role.Summary, "Body"));
                foreach (var highlight in CleanLines(role.Highlights))
                {
                    body.Append(Bullet(highlight));
                }

                var tech = JoinItems(role.Technologies);
                if (tech.Length != 0)
                {
                    body.Append(Labelled("Meta", "Tools", tech));
                }

                foreach (var earlier in role.Progression)
                {
                    body.Append(EntryBlockAts(earlier.Role, role.Company, earlier.Dates, earlier.Location));
                    body.Append(Paragraph(earlier.Summary, "Body"));
                    foreach (var highlight in CleanLines(earlier.Highlights))
                    {
                        body.Append(Bullet(highlight));
                    }

                    var earlierTech = JoinItems(earlier.Technologies);
                    if (earlierTech.Length != 0)
                    {
                        body.Append(Labelled("Meta", "Tools", earlierTech));
                    }
                }
            }
        }

        if (document.Skills.Count != 0)
        {
            body.Append(Heading("Skills"));
            foreach (var group in document.Skills)
            {
                var items = JoinItems(group.Items);
                if (group.Name.Length != 0 && items.Length != 0)
                {
                    body.Append(Labelled("Body", group.Name, items));
                }
            }
        }

        if (document.Education.Count != 0)
        {
            body.Append(Heading("Education & Certifications"));
            foreach (var item in document.Education)
            {
                body.Append(EntryBlockAts(item.Name, item.Institution, item.Dates, ""));
                body.Append(Paragraph(item.Detail, "Body"));
            }
        }

        foreach (var group in document.Additional)
        {
            var items = CleanLines(group.Items);
            if (items.Count == 0)
            {
                continue;
            }

            body.Append(Heading(group.Name ?? "Additional"));
            foreach (var item in items)
            {
                body.Append(Paragraph(item, "Body"));
            }
        }

        return body.ToString();
    }

    /// <summary>
    /// A letter carries no header block — no name, headline or contact line — so
    /// the correspondence itself is the whole document.
    /// </summary>
    internal static string CoverLetter(Document document)
    {
        var body = new StringBuilder();
        body.Append(Paragraph(document.Date, "Body"));
        foreach (var line in CleanLines(document.Recipient))
        {
            body.Append(Paragraph(line, "NoGap"));
        }

        body.Append(Paragraph(document.Subject, "Subject"));
        body.Append(Paragraph(document.Salutation, "Body"));
        foreach (var text in CleanLines(document.Body))
        {
            body.Append(Paragraph(text, "Body"));
        }

        body.Append(Paragraph(document.SignOff, "SignOff"));
        body.Append(Paragraph(document.Name, "Body"));
        return body.ToString();
    }

    /// <summary>
    /// The contact row: an icon and value per entry where the label maps to a
    /// known icon, "Label: value" text otherwise.
    /// </summary>
    private static string ContactParagraph(Document document)
    {
        var runs = new StringBuilder();
        foreach (var item in document.Contact)
        {
            var value = item.Value.Trim();
            if (value.Length == 0)
            {
                continue;
            }

            if (runs.Length != 0)
            {
                runs.Append(Run(EmSpace, ""));
            }

            var icon = ContactIcon(item.Label, item.Url);
            if (icon is not null)
            {
                runs.Append(IconRun(icon, IconContact));
                runs.Append(Run(" ", ""));
                runs.Append(Run(value, ""));
            }
            else
            {
                var label = item.Label.Trim();
                runs.Append(Run(label.Length == 0 ? value : $"{label}: {value}", ""));
            }
        }

        return runs.Length == 0 ? "" : RunsParagraph("ContactLine", runs.ToString());
    }

    internal static string? ContactIcon(string label, string url)
    {
        var lower = label.ToLowerInvariant();
        bool Matches(params string[] keys) => keys.Any(key => lower.Contains(key, StringComparison.Ordinal));

        if (Matches("mobile", "phone", "tel"))
        {
            return "phone";
        }

        if (Matches("mail"))
        {
            return "email";
        }

        if (Matches("location", "address", "city"))
        {
            return "pin";
        }

        if (url.Trim().Length != 0 || Matches("linkedin", "github", "web", "site", "portfolio", "url"))
        {
            return "link";
        }

        return null;
    }

    /// <summary>The role, company and date/location lines that open an entry.</summary>
    internal static string EntryBlock(string title, string company, string dates, string location) =>
        Paragraph(title, "EntryTitle") + Paragraph(company, "Company") + MetaParagraph(dates, location);

    /// <summary>The icon-free entry opener used by the ATS variant.</summary>
    private static string EntryBlockAts(string title, string company, string dates, string location) =>
        Paragraph(title, "EntryTitle")
        + Paragraph(company, "Company")
        + Paragraph(JoinParts(", ", dates, location), "Meta");

    /// <summary>A calendar-marked date and a pin-marked location on one muted line.</summary>
    private static string MetaParagraph(string dates, string location)
    {
        dates = dates.Trim();
        location = location.Trim();
        if (dates.Length == 0 && location.Length == 0)
        {
            return "";
        }

        var runs = new StringBuilder();
        if (dates.Length != 0)
        {
            runs.Append(IconRun("calendar", IconMeta));
            runs.Append(Run(" ", ""));
            runs.Append(Run(dates, ""));
        }

        if (location.Length != 0)
        {
            if (runs.Length != 0)
            {
                runs.Append(Run(EmSpace, ""));
            }

            runs.Append(IconRun("pin", IconMeta));
            runs.Append(Run(" ", ""));
            runs.Append(Run(location, ""));
        }

        return RunsParagraph("Meta", runs.ToString());
    }

    private static string TwoColumns(string main, string side) =>
        $"""<w:tbl><w:tblPr><w:tblW w:w="{ContentWidth}" w:type="dxa"/><w:tblLayout w:type="fixed"/><w:tblCellMar><w:left w:w="0" w:type="dxa"/><w:right w:w="0" w:type="dxa"/></w:tblCellMar></w:tblPr><w:tblGrid><w:gridCol w:w="{MainWidth}"/><w:gridCol w:w="{SideWidth}"/></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w="{MainWidth}" w:type="dxa"/><w:tcMar><w:right w:w="{Gutter}" w:type="dxa"/></w:tcMar></w:tcPr>{main}</w:tc><w:tc><w:tcPr><w:tcW w:w="{SideWidth}" w:type="dxa"/></w:tcPr>{side}</w:tc></w:tr></w:tbl>""";

    internal static string Paragraph(string text, string style) =>
        text.Length == 0 ? "" : RunsParagraph(style, Run(text, ""));

    /// <summary>A paragraph with no text, used for separators and structural spacing.</summary>
    internal static string EmptyParagraph(string style) => RunsParagraph(style, Run("", ""));

    private static string Heading(string text) => Paragraph(text.ToUpperInvariant(), "Heading");

    /// <summary>A bold label followed by a value in the style's own formatting.</summary>
    private static string Labelled(string style, string label, string value)
    {
        var runs = Run($"{label}: ", $"""<w:b/><w:color w:val="{Black}" w:themeColor="text1"/>""")
            + Run(value, "");
        return RunsParagraph(style, runs);
    }

    private static string Bullet(string text)
    {
        var marker = Run("• ", $"""<w:color w:val="{Muted}" w:themeColor="accent1"/>""");
        return $"""<w:p><w:pPr><w:pStyle w:val="Bullet"/><w:ind w:left="360" w:hanging="180"/></w:pPr>{marker}{Run(text, "")}</w:p>""";
    }

    private static string RunsParagraph(string style, string runs) =>
        $"""<w:p><w:pPr><w:pStyle w:val="{style}"/></w:pPr>{runs}</w:p>""";

    private static string Run(string text, string properties)
    {
        var wrapped = properties.Length == 0 ? "" : $"<w:rPr>{properties}</w:rPr>";
        return $"""<w:r>{wrapped}<w:t xml:space="preserve">{XmlEscape(text)}</w:t></w:r>""";
    }

    /// <summary>
    /// An inline picture run referencing one of the embedded icon parts, sized
    /// in EMUs and nudged down slightly to sit optically centred beside text.
    /// </summary>
    private static string IconRun(string icon, int size)
    {
        var id = Interlocked.Increment(ref drawingId);
        return $"""<w:r><w:rPr><w:noProof/><w:position w:val="-2"/></w:rPr><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0"><wp:extent cx="{size}" cy="{size}"/><wp:docPr id="{id}" name="{icon}"/><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic><pic:nvPicPr><pic:cNvPr id="{id}" name="{icon}"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="rIdIcon-{icon}"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="{size}" cy="{size}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r>""";
    }

    internal static string XmlEscape(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);

    private static void WriteDocx(string path, string body, Document document)
    {
        var documentXml = $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture">
              <w:body>
                {body}
                <w:sectPr>
                  <w:pgSz w:w="11906" w:h="16838"/>
                  <w:pgMar w:top="680" w:right="720" w:bottom="680" w:left="720" w:header="708" w:footer="708" w:gutter="0"/>
                </w:sectPr>
              </w:body>
            </w:document>
            """;

        using var file = File.Create(path);
        using var archive = new ZipArchive(file, ZipArchiveMode.Create);

        void Add(string name, byte[] contents)
        {
            var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
            // A fixed stamp keeps the package byte-reproducible for a given input.
            entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
            using var stream = entry.Open();
            stream.Write(contents);
        }

        void AddText(string name, string contents) => Add(name, Encoding.UTF8.GetBytes(contents));

        AddText("[Content_Types].xml", DocxParts.ContentTypes);
        AddText("_rels/.rels", DocxParts.RootRels);
        AddText("word/_rels/document.xml.rels", DocxParts.DocumentRels);
        AddText("word/document.xml", documentXml);
        AddText("word/styles.xml", DocxParts.Styles);
        AddText("word/settings.xml", DocxParts.Settings);
        AddText("word/fontTable.xml", DocxParts.FontTable);
        AddText("word/theme/theme1.xml", DocxParts.Theme);
        AddText("docProps/core.xml", CoreProperties(document));
        AddText("docProps/app.xml", DocxParts.AppProperties);

        foreach (var icon in IconNames)
        {
            Add($"word/media/icon-{icon}.png", Icon(icon));
        }
    }

    private static byte[] Icon(string name)
    {
        var resource = $"icon-{name}.png";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new GeneratorException($"Missing embedded icon: {resource}");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string CoreProperties(Document document)
    {
        var kind = document.IsCoverLetter ? "Cover Letter" : "CV";
        var title = document.Name.Length == 0
            ? kind
            : string.Create(CultureInfo.InvariantCulture, $"{document.Name} — {kind}");

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/">
              <dc:title>{XmlEscape(title)}</dc:title>
              <dc:creator>{XmlEscape(document.Name)}</dc:creator>
            </cp:coreProperties>
            """;
    }
}
