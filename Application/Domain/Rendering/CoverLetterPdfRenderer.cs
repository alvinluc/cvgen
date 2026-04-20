using System.Collections.Generic;
using System.IO;
using Application.Domain.Model;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Application.Domain.Rendering
{
    public class CoverLetterPdfRenderer : ICoverLetterRenderer
    {
        private const string FontFamily = "Times New Roman";
        private const float BaseFontSize = 11f;
        private const float NameFontSize = 20f;
        private const string LinkColor = "#0000FF";

        public void Render(CoverLetterDocument document, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.Letter);
                    page.MarginTop(1, Unit.Inch);
                    page.MarginBottom(1, Unit.Inch);
                    page.MarginLeft(1, Unit.Inch);
                    page.MarginRight(1, Unit.Inch);
                    page.DefaultTextStyle(x => x.FontSize(BaseFontSize).FontFamily(FontFamily));

                    page.Content().Column(column =>
                    {
                        column.Item().Text(document.Name).FontSize(NameFontSize);
                        column.Item().PaddingVertical(6);

                        if (document.Contact.LeftColumn.Count > 0 || document.Contact.RightColumn.Count > 0)
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(left =>
                                {
                                    foreach (var item in document.Contact.LeftColumn)
                                        left.Item().Text(text => RenderInlines(text, item));
                                });
                                row.RelativeItem().Column(right =>
                                {
                                    foreach (var item in document.Contact.RightColumn)
                                        right.Item().Text(text => RenderInlines(text, item));
                                });
                            });
                            column.Item().PaddingVertical(10);
                        }

                        if (!string.IsNullOrWhiteSpace(document.Date))
                        {
                            column.Item().Text(document.Date);
                            column.Item().PaddingVertical(6);
                        }

                        foreach (var line in document.Recipient)
                            column.Item().Text(text => RenderInlines(text, line));

                        if (document.Recipient.Count > 0)
                            column.Item().PaddingVertical(10);

                        if (document.Subject.Count > 0)
                        {
                            column.Item().Text(text =>
                            {
                                foreach (var inline in document.Subject)
                                    text.Span(inline.Text).Bold();
                            });
                            column.Item().PaddingVertical(8);
                        }

                        if (document.Salutation.Count > 0)
                        {
                            column.Item().Text(text => RenderInlines(text, document.Salutation));
                            column.Item().PaddingVertical(6);
                        }

                        foreach (var paragraph in document.Body)
                        {
                            column.Item().Text(text => RenderInlines(text, paragraph));
                            column.Item().PaddingVertical(4);
                        }

                        if (document.SignOff.Count > 0)
                        {
                            column.Item().PaddingVertical(4);
                            column.Item().Text(text => RenderInlines(text, document.SignOff));
                            column.Item().PaddingVertical(20);
                            column.Item().Text(document.Name);
                        }
                    });
                });
            })
            .GeneratePdf(outputPath);
        }

        private static void RenderInlines(TextDescriptor text, List<InlineContent> inlines)
        {
            foreach (var inline in inlines)
            {
                var span = inline.IsLink
                    ? text.Hyperlink(inline.Text, inline.Url!).FontColor(LinkColor)
                    : text.Span(inline.Text);

                if (inline.IsBold) span.Bold();
                if (inline.IsItalic) span.Italic();
            }
        }
    }
}
