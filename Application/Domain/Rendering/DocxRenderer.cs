using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application.Domain.Model;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Application.Domain.Rendering
{
    public class DocxRenderer : IDocumentRenderer
    {
        private const string FontName = "Times New Roman";
        private const string BaseFontSize = "20"; // half-points: 20 = 10pt
        private const string NameFontSize = "50"; // 25pt
        private const string H1FontSize = "34";   // 17pt
        private const string H2FontSize = "28";   // 14pt
        private const string H3FontSize = "24";   // 12pt
        private const string H4FontSize = "22";   // 11pt
        private const string LinkColor = "0000FF";

        public void Render(CvDocument document, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var wordDoc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Page settings: Letter, 1" margins (1440 twips = 1 inch)
            var sectionProps = new SectionProperties(
                new PageSize { Width = 12240, Height = 15840 }, // Letter: 8.5" x 11"
                new PageMargin
                {
                    Top = 1440, Bottom = 1440,
                    Left = 1440, Right = 1440,
                    Header = 720, Footer = 720
                }
            );
            body.AppendChild(sectionProps);

            // Name
            body.AppendChild(CreateParagraph(document.Name, NameFontSize, bold: false));

            // Contact info - two columns using a table
            if (document.Contact.LeftColumn.Count > 0 || document.Contact.RightColumn.Count > 0)
            {
                var table = CreateContactTable(document.Contact);
                body.AppendChild(table);
                body.AppendChild(new Paragraph()); // spacer
            }

            // Sections
            foreach (var section in document.Sections)
            {
                RenderSection(body, section);
            }
        }

        private Table CreateContactTable(ContactInfo contact)
        {
            var table = new Table();

            // Table properties: no borders, full width
            var tblProps = new TableProperties(
                new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
                new TableBorders(
                    new TopBorder { Val = BorderValues.None },
                    new BottomBorder { Val = BorderValues.None },
                    new LeftBorder { Val = BorderValues.None },
                    new RightBorder { Val = BorderValues.None },
                    new InsideHorizontalBorder { Val = BorderValues.None },
                    new InsideVerticalBorder { Val = BorderValues.None }
                )
            );
            table.AppendChild(tblProps);

            var maxRows = System.Math.Max(contact.LeftColumn.Count, contact.RightColumn.Count);
            for (int i = 0; i < maxRows; i++)
            {
                var row = new TableRow();

                var leftCell = new TableCell();
                leftCell.AppendChild(new TableCellProperties(
                    new TableCellWidth { Width = "50", Type = TableWidthUnitValues.Pct }));
                if (i < contact.LeftColumn.Count)
                    leftCell.AppendChild(CreateInlineParagraph(contact.LeftColumn[i]));
                else
                    leftCell.AppendChild(new Paragraph());
                row.AppendChild(leftCell);

                var rightCell = new TableCell();
                rightCell.AppendChild(new TableCellProperties(
                    new TableCellWidth { Width = "50", Type = TableWidthUnitValues.Pct }));
                if (i < contact.RightColumn.Count)
                    rightCell.AppendChild(CreateInlineParagraph(contact.RightColumn[i]));
                else
                    rightCell.AppendChild(new Paragraph());
                row.AppendChild(rightCell);

                table.AppendChild(row);
            }

            return table;
        }

        private void RenderSection(Body body, CvSection section)
        {
            // Section heading with bottom border
            var heading = CreateParagraph(section.Title, H1FontSize, bold: false, medium: true);
            var pProps = heading.GetFirstChild<ParagraphProperties>() ?? heading.PrependChild(new ParagraphProperties());
            pProps.AppendChild(new ParagraphBorders(
                new BottomBorder
                {
                    Val = BorderValues.Single,
                    Size = 4,
                    Space = 1,
                    Color = "808080"
                }
            ));
            pProps.AppendChild(new SpacingBetweenLines { Before = "200", After = "80" });
            body.AppendChild(heading);

            RenderContentBlocks(body, section.Content);

            foreach (var sub in section.SubSections)
            {
                RenderSubSection(body, sub);
            }
        }

        private static string GetSubSectionFontSize(int level) => level switch
        {
            3 => H3FontSize,
            4 => H4FontSize,
            _ => H2FontSize
        };

        private void RenderSubSection(Body body, CvSubSection sub)
        {
            var fontSize = GetSubSectionFontSize(sub.Level);

            // Subsection heading - italic
            var para = new Paragraph();
            var pProps = new ParagraphProperties(
                new SpacingBetweenLines { Before = "100", After = "40" });
            para.AppendChild(pProps);

            foreach (var inline in sub.Title)
            {
                var run = CreateRun(inline.Text, fontSize, italic: true);
                if (inline.IsLink)
                {
                    // For subsection titles, just render the text (links in headings)
                    var rProps = run.GetFirstChild<RunProperties>() ?? run.PrependChild(new RunProperties());
                    rProps.AppendChild(new Color { Val = LinkColor });
                }
                para.AppendChild(run);
            }
            body.AppendChild(para);

            RenderContentBlocks(body, sub.Content);
        }

        private void RenderContentBlocks(Body body, List<ContentBlock> blocks)
        {
            foreach (var block in blocks)
            {
                switch (block.Type)
                {
                    case ContentBlockType.Paragraph:
                        foreach (var line in block.Items)
                        {
                            body.AppendChild(CreateInlineParagraph(line));
                        }
                        break;

                    case ContentBlockType.List:
                        foreach (var item in block.Items)
                        {
                            var para = new Paragraph();
                            var pProps = new ParagraphProperties(
                                new Indentation { Left = "360" });
                            para.AppendChild(pProps);
                            para.AppendChild(CreateRun("-- ", BaseFontSize));
                            foreach (var inline in item)
                            {
                                para.AppendChild(CreateInlineRun(inline));
                            }
                            body.AppendChild(para);
                        }
                        break;

                    case ContentBlockType.DefinitionList:
                        foreach (var (term, definition) in block.Definitions)
                        {
                            var para = new Paragraph();
                            foreach (var inline in term)
                            {
                                para.AppendChild(CreateInlineRun(inline));
                            }
                            para.AppendChild(CreateRun(" ", BaseFontSize));
                            foreach (var inline in definition)
                            {
                                para.AppendChild(CreateInlineRun(inline));
                            }
                            body.AppendChild(para);
                        }
                        break;
                }
            }
        }

        private Paragraph CreateInlineParagraph(List<InlineContent> inlines)
        {
            var para = new Paragraph();
            foreach (var inline in inlines)
            {
                para.AppendChild(CreateInlineRun(inline));
            }
            return para;
        }

        private Run CreateInlineRun(InlineContent inline)
        {
            var run = CreateRun(inline.Text, BaseFontSize, bold: inline.IsBold, italic: inline.IsItalic);
            if (inline.IsLink)
            {
                var rProps = run.GetFirstChild<RunProperties>() ?? run.PrependChild(new RunProperties());
                rProps.AppendChild(new Color { Val = LinkColor });
                rProps.AppendChild(new Underline { Val = UnderlineValues.Single });
            }
            return run;
        }

        private static Paragraph CreateParagraph(string text, string fontSize, bool bold = false, bool italic = false, bool medium = false)
        {
            var para = new Paragraph();
            var run = CreateRun(text, fontSize, bold, italic, medium);
            para.AppendChild(run);
            return para;
        }

        private static Run CreateRun(string text, string fontSize, bool bold = false, bool italic = false, bool medium = false)
        {
            var run = new Run();
            var runProps = new RunProperties();
            runProps.AppendChild(new RunFonts { Ascii = FontName, HighAnsi = FontName });
            runProps.AppendChild(new FontSize { Val = fontSize });

            if (bold)
                runProps.AppendChild(new Bold());
            if (medium)
                runProps.AppendChild(new Bold { Val = OnOffValue.FromBoolean(false) }); // medium weight approximation
            if (italic)
                runProps.AppendChild(new Italic());

            run.AppendChild(runProps);
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            return run;
        }
    }
}
