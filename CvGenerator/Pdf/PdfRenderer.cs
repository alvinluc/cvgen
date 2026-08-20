using CvGenerator.Model;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using static CvGenerator.Model.Values;
using static CvGenerator.Pdf.PdfTheme;

using CvDocument = CvGenerator.Model.Document;
using QuestDocument = QuestPDF.Fluent.Document;

namespace CvGenerator.Pdf;

/// <summary>
/// Lays out a CV or cover letter with QuestPDF.
/// <para>
/// The template is expressed as composition calls rather than markup, so the
/// structure of these methods is the structure of the page: a header, then
/// ruled sections, each built from entries, paragraphs, bullets and chips. The
/// palette and every size and gap live in <see cref="PdfTheme"/>; nothing here
/// hard-codes a measurement.
/// </para>
/// </summary>
internal static class PdfRenderer
{
    public static void Render(CvDocument document, string outputPath)
    {
        var pdf = Build(document).GeneratePdf();

        var parent = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        try
        {
            File.WriteAllBytes(outputPath, pdf);
        }
        catch (IOException error)
        {
            throw new GeneratorException($"{outputPath}: {error.Message}", error);
        }
    }

    /// <summary>
    /// The composed, metadata-stamped document. Kept separate from
    /// <see cref="Render"/> so tests can rasterise pages rather than only
    /// checking that bytes were written.
    /// </summary>
    internal static IDocument Build(CvDocument document)
    {
        Configure();

        var kind = document.IsCoverLetter ? "Cover Letter" : "CV";
        var title = document.Name.Length == 0 ? kind : $"{document.Name} — {kind}";
        var now = DateTime.UtcNow;

        return QuestDocument
            .Create(container => container.Page(page => ComposePage(page, document)))
            .WithMetadata(new DocumentMetadata
            {
                Title = title,
                Author = document.Name,
                Creator = "cv-generator",
                Producer = "cv-generator",
                Language = "en",
                // Recorded in UTC so the same input stamps the same way wherever
                // it is generated.
                CreationDate = now,
                ModifiedDate = now,
            });
    }

    private static void Configure()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // A CV carries names and punctuation from anywhere; a missing glyph in a
        // fallback face should degrade quietly rather than abort the render.
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
        PdfFonts.Register();
    }

    private static void ComposePage(PageDescriptor page, CvDocument document)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(PageMarginHorizontalCm, Unit.Centimetre);
        page.MarginVertical(PageMarginVerticalCm, Unit.Centimetre);
        page.DefaultTextStyle(style => style
            .FontFamily(PdfFonts.Families)
            .FontSize(BaseSize)
            .FontColor(Ink)
            .LineHeight(LineHeight));

        page.Content().Column(column =>
        {
            if (document.IsCoverLetter)
            {
                ComposeCoverLetter(column, document);
            }
            else
            {
                ComposeCv(column, document);
            }
        });
    }

    private static void ComposeCv(ColumnDescriptor column, CvDocument document)
    {
        Header(column, document);

        if (document.Summary.Trim().Length != 0)
        {
            Section(column, "Profile");
            Paragraph(column, document.Summary, ParagraphGap);
        }

        if (document.Experience.Count != 0)
        {
            Section(column, "Experience");
            var hasPreviousExperienceBlock = false;
            foreach (var role in document.Experience)
            {
                if (hasPreviousExperienceBlock)
                {
                    ExperienceBreak(column);
                }

                Entry(column, role.Role, role.Company, role.Dates, role.Location, compact: false);
                Paragraph(column, role.Summary, ParagraphGap);
                Bullets(column, role.Highlights);
                Tools(column, JoinItems(role.Technologies));

                hasPreviousExperienceBlock = true;
                foreach (var earlier in role.Progression)
                {
                    ExperienceBreak(column);
                    Entry(column, earlier.Role, role.Company, earlier.Dates, earlier.Location, compact: true);
                    Paragraph(column, earlier.Summary, ParagraphGap);
                    Bullets(column, earlier.Highlights);
                    Tools(column, JoinItems(earlier.Technologies));
                }
            }
        }

        var skills = document.Skills
            .Where(group => group.Name.Trim().Length != 0 && CleanLines(group.Items).Count != 0)
            .ToList();
        if (skills.Count != 0)
        {
            Section(column, "Skills");
            foreach (var group in skills)
            {
                SkillGroup(column, group.Name, group.Items);
            }
        }

        if (document.Education.Count != 0)
        {
            Section(column, "Education & Certifications");
            foreach (var item in document.Education)
            {
                Entry(column, item.Name, item.Institution, item.Dates, "", compact: true);
                Paragraph(column, item.Detail, ParagraphGap);
            }
        }

        foreach (var group in document.Additional)
        {
            if (CleanLines(group.Items).Count == 0)
            {
                continue;
            }

            Section(column, group.Name ?? "Additional");
            Bullets(column, group.Items);
        }
    }

    /// <summary>
    /// A letter carries no header block — no name, headline or contact line — so
    /// the correspondence itself is the whole document.
    /// </summary>
    private static void ComposeCoverLetter(ColumnDescriptor column, CvDocument document)
    {
        Paragraph(column, document.Date, DateGap);

        foreach (var recipient in CleanLines(document.Recipient))
        {
            column.Item().PaddingBottom(RecipientGap).Text(recipient).FontSize(9.6f);
        }

        if (document.Subject.Trim().Length != 0)
        {
            column.Item()
                .PaddingTop(Em(0.9f))
                .PaddingBottom(Em(0.8f))
                .Text(document.Subject.Trim())
                .Bold()
                .FontColor(Accent);
        }

        Paragraph(column, document.Salutation, BodyGap);
        foreach (var text in CleanLines(document.Body))
        {
            Paragraph(column, text, BodyGap);
        }

        Paragraph(column, document.SignOff, SignOffGap);
        Paragraph(column, document.Name, ParagraphGap);
    }

    /// <summary>The centred name, headline and contact block that opens a CV.</summary>
    private static void Header(ColumnDescriptor column, CvDocument document)
    {
        column.Item().AlignCenter().Text(document.Name).FontSize(24f).Bold().FontColor(Ink);

        if (document.Headline.Trim().Length != 0)
        {
            column.Item().AlignCenter().Text(document.Headline.Trim()).FontSize(9.4f).FontColor(Accent);
        }

        var contact = document.ContactLine();
        if (contact.Length != 0)
        {
            column.Item().AlignCenter().Text(contact).FontSize(8f).FontColor(Muted);
        }

        column.Item()
            .PaddingTop(Em(0.35f))
            .AlignCenter()
            .Width(HeaderRuleWidth)
            .LineHorizontal(0.7f)
            .LineColor(Accent);

        column.Item().Height(Em(0.55f));
    }

    /// <summary>An uppercase accent label, then a rule running to the margin.</summary>
    private static void Section(ColumnDescriptor column, string title)
    {
        if (title.Trim().Length == 0)
        {
            return;
        }

        column.Item().PaddingTop(Em(0.78f)).PaddingBottom(Em(0.42f)).Row(row =>
        {
            row.AutoItem()
                .Text(title.Trim().ToUpperInvariant())
                .FontSize(9.2f)
                .Bold()
                .FontColor(Accent);

            row.RelativeItem()
                .PaddingLeft(Em(0.7f))
                .AlignMiddle()
                .LineHorizontal(0.55f)
                .LineColor(Rule);
        });
    }

    private static void Paragraph(ColumnDescriptor column, string value, float gap)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        column.Item().PaddingBottom(gap).Text(text =>
        {
            text.Justify();
            text.Span(trimmed);
        });
    }

    private static void ExperienceBreak(ColumnDescriptor column) => column.Item().Height(Em(0.62f));

    /// <summary>The role and company on the left, dates and location on the right.</summary>
    private static void Entry(
        ColumnDescriptor column,
        string role,
        string company,
        string dates,
        string location,
        bool compact)
    {
        role = role.Trim();
        company = company.Trim();
        var when = JoinParts(" · ", dates, location);
        var left = company.Length == 0 ? role : $"{role} — {company}";
        if (left.Length == 0 && when.Length == 0)
        {
            return;
        }

        column.Item().PaddingTop(Em(compact ? 0.18f : 0.34f)).Row(row =>
        {
            row.RelativeItem().Text(left).Bold().FontColor(Ink);

            if (when.Length != 0)
            {
                // Bottom alignment gets the two sizes close; the nudge lifts the
                // smaller line the rest of the way onto the title's baseline,
                // which QuestPDF has no direct way to share across row items.
                row.AutoItem()
                    .PaddingLeft(Em(1f))
                    .PaddingBottom(Em(0.16f))
                    .AlignBottom()
                    .Text(when)
                    .FontSize(8.2f)
                    .FontColor(Muted);
            }
        });
    }

    private static void Bullets(ColumnDescriptor column, IReadOnlyList<string> values)
    {
        foreach (var item in CleanLines(values))
        {
            column.Item().PaddingLeft(Em(0.3f)).PaddingBottom(Em(0.28f)).Row(row =>
            {
                row.ConstantItem(Em(0.85f)).Text("•").FontColor(Muted);
                row.RelativeItem().Text(text =>
                {
                    text.Justify();
                    text.Span(item);
                });
            });
        }
    }

    /// <summary>A bold accent group name followed by soft-filled skill chips.</summary>
    private static void SkillGroup(ColumnDescriptor column, string key, IReadOnlyList<string> values)
    {
        var items = CleanLines(values);
        key = key.Trim();
        if (key.Length == 0 || items.Count == 0)
        {
            return;
        }

        column.Item().PaddingBottom(Em(0.5f)).Inlined(inlined =>
        {
            inlined.HorizontalSpacing(Em(0.36f));
            inlined.VerticalSpacing(Em(0.3f));
            inlined.BaselineMiddle();

            inlined.Item().Text(key).Bold().FontColor(Accent);

            foreach (var item in items)
            {
                inlined.Item()
                    .Background(Soft)
                    .Border(0.35f)
                    .BorderColor(Rule)
                    .CornerRadius(Em(0.6f))
                    .PaddingHorizontal(Em(0.46f))
                    .PaddingVertical(Em(0.16f))
                    .Text(item)
                    .FontSize(8.2f)
                    .FontColor(Accent);
            }
        });
    }

    /// <summary>The "Tools: …" line closing an experience entry.</summary>
    private static void Tools(ColumnDescriptor column, string technologies)
    {
        if (technologies.Length == 0)
        {
            return;
        }

        column.Item().PaddingTop(Em(0.15f)).PaddingBottom(Em(0.35f)).Text(text =>
        {
            text.DefaultTextStyle(style => style.FontSize(8.5f).FontColor(Muted));
            text.Span("Tools: ").Bold().FontColor(Accent);
            text.Span(technologies);
        });
    }
}
