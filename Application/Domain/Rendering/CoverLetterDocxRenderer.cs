using System.Collections.Generic;
using System.IO;
using Application.Domain.Model;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Application.Domain.Rendering
{
    public class CoverLetterDocxRenderer : ICoverLetterRenderer
    {
        private const string FontName = "Times New Roman";
        private const string BaseFontSize = "22"; // 11pt
        private const string NameFontSize = "40"; // 20pt
        private const string LinkColor = "0000FF";

        public void Render(CoverLetterDocument document, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var wordDoc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            var sectionProps = new SectionProperties(
                new PageSize { Width = 12240, Height = 15840 },
                new PageMargin
                {
                    Top = 1440, Bottom = 1440,
                    Left = 1440, Right = 1440,
                    Header = 720, Footer = 720
                }
            );
            body.AppendChild(sectionProps);

            body.AppendChild(CreateParagraph(document.Name, NameFontSize));

            if (document.Contact.LeftColumn.Count > 0 || document.Contact.RightColumn.Count > 0)
            {
                body.AppendChild(CreateContactTable(document.Contact));
                body.AppendChild(new Paragraph());
            }

            if (!string.IsNullOrWhiteSpace(document.Date))
            {
                body.AppendChild(CreateParagraph(document.Date, BaseFontSize));
                body.AppendChild(new Paragraph());
            }

            foreach (var line in document.Recipient)
                body.AppendChild(CreateInlineParagraph(line));
            if (document.Recipient.Count > 0)
                body.AppendChild(new Paragraph());

            if (document.Subject.Count > 0)
            {
                var subjectPara = new Paragraph();
                foreach (var inline in document.Subject)
                    subjectPara.AppendChild(CreateRun(inline.Text, BaseFontSize, bold: true));
                body.AppendChild(subjectPara);
                body.AppendChild(new Paragraph());
            }

            if (document.Salutation.Count > 0)
            {
                body.AppendChild(CreateInlineParagraph(document.Salutation));
                body.AppendChild(new Paragraph());
            }

            foreach (var paragraph in document.Body)
            {
                body.AppendChild(CreateInlineParagraph(paragraph));
                body.AppendChild(new Paragraph());
            }

            if (document.SignOff.Count > 0)
            {
                body.AppendChild(CreateInlineParagraph(document.SignOff));
                body.AppendChild(new Paragraph());
                body.AppendChild(new Paragraph());
                body.AppendChild(CreateParagraph(document.Name, BaseFontSize));
            }
        }

        private Table CreateContactTable(ContactInfo contact)
        {
            var table = new Table();
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
                leftCell.AppendChild(new TableCellProperties(new TableCellWidth { Width = "50", Type = TableWidthUnitValues.Pct }));
                leftCell.AppendChild(i < contact.LeftColumn.Count ? CreateInlineParagraph(contact.LeftColumn[i]) : new Paragraph());
                row.AppendChild(leftCell);

                var rightCell = new TableCell();
                rightCell.AppendChild(new TableCellProperties(new TableCellWidth { Width = "50", Type = TableWidthUnitValues.Pct }));
                rightCell.AppendChild(i < contact.RightColumn.Count ? CreateInlineParagraph(contact.RightColumn[i]) : new Paragraph());
                row.AppendChild(rightCell);

                table.AppendChild(row);
            }
            return table;
        }

        private Paragraph CreateInlineParagraph(List<InlineContent> inlines)
        {
            var para = new Paragraph();
            foreach (var inline in inlines)
                para.AppendChild(CreateInlineRun(inline));
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

        private static Paragraph CreateParagraph(string text, string fontSize, bool bold = false, bool italic = false)
        {
            var para = new Paragraph();
            para.AppendChild(CreateRun(text, fontSize, bold, italic));
            return para;
        }

        private static Run CreateRun(string text, string fontSize, bool bold = false, bool italic = false)
        {
            var run = new Run();
            var runProps = new RunProperties();
            runProps.AppendChild(new RunFonts { Ascii = FontName, HighAnsi = FontName });
            runProps.AppendChild(new FontSize { Val = fontSize });
            if (bold) runProps.AppendChild(new Bold());
            if (italic) runProps.AppendChild(new Italic());
            run.AppendChild(runProps);
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }
    }
}
