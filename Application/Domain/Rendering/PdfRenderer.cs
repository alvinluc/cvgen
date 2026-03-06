using System.Collections.Generic;
using System.IO;
using Application.Domain.Model;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Application.Domain.Rendering
{
    public class PdfRenderer : IDocumentRenderer
    {
        private const string FontFamily = "Times New Roman";
        private const float BaseFontSize = 10f;
        private const float NameFontSize = 25f;
        private const float H1FontSize = 17f;
        private const float H2FontSize = 14f;
        private const string LinkColor = "#0000FF";

        public void Render(CvDocument document, string outputPath)
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
                        // Name
                        column.Item().Text(document.Name)
                            .FontSize(NameFontSize);

                        column.Item().PaddingVertical(8);

                        // Contact info - two columns side by side
                        if (document.Contact.LeftColumn.Count > 0 || document.Contact.RightColumn.Count > 0)
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(left =>
                                {
                                    foreach (var item in document.Contact.LeftColumn)
                                    {
                                        left.Item().Text(text => RenderInlines(text, item));
                                    }
                                });
                                row.RelativeItem().Column(right =>
                                {
                                    foreach (var item in document.Contact.RightColumn)
                                    {
                                        right.Item().Text(text => RenderInlines(text, item));
                                    }
                                });
                            });

                            column.Item().PaddingVertical(4);
                        }

                        // Sections
                        foreach (var section in document.Sections)
                        {
                            RenderSection(column, section);
                        }
                    });
                });
            })
            .GeneratePdf(outputPath);
        }

        private static void RenderSection(ColumnDescriptor column, CvSection section)
        {
            column.Item().PaddingTop(8).Column(sectionCol =>
            {
                // Section heading
                sectionCol.Item().Text(section.Title)
                    .FontSize(H1FontSize)
                    .Medium();

                // Bottom border separator
                sectionCol.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

                sectionCol.Item().PaddingTop(4);

                // Section content
                RenderContentBlocks(sectionCol, section.Content);

                // Subsections
                foreach (var sub in section.SubSections)
                {
                    RenderSubSection(sectionCol, sub);
                }
            });
        }

        private static void RenderSubSection(ColumnDescriptor column, CvSubSection sub)
        {
            column.Item().PaddingTop(4).Column(subCol =>
            {
                // Subsection heading
                subCol.Item().Text(text =>
                {
                    foreach (var inline in sub.Title)
                    {
                        if (inline.IsLink)
                            text.Span(inline.Text).FontSize(H2FontSize).Italic().FontColor(LinkColor);
                        else
                            text.Span(inline.Text).FontSize(H2FontSize).Italic();
                    }
                });

                subCol.Item().PaddingTop(2);

                RenderContentBlocks(subCol, sub.Content);
            });
        }

        private static void RenderContentBlocks(ColumnDescriptor column, List<ContentBlock> blocks)
        {
            foreach (var block in blocks)
            {
                switch (block.Type)
                {
                    case ContentBlockType.Paragraph:
                        foreach (var line in block.Items)
                        {
                            column.Item().Text(text => RenderInlines(text, line));
                        }
                        column.Item().PaddingVertical(2);
                        break;

                    case ContentBlockType.List:
                        column.Item().PaddingLeft(15).Column(listCol =>
                        {
                            foreach (var item in block.Items)
                            {
                                listCol.Item().Row(row =>
                                {
                                    row.ConstantItem(15).Text("--").FontSize(BaseFontSize);
                                    row.RelativeItem().Text(text => RenderInlines(text, item));
                                });
                            }
                        });
                        column.Item().PaddingVertical(2);
                        break;

                    case ContentBlockType.DefinitionList:
                        foreach (var (term, definition) in block.Definitions)
                        {
                            column.Item().Text(text =>
                            {
                                RenderInlines(text, term);
                                text.Span(" ");
                                RenderInlines(text, definition);
                            });
                        }
                        column.Item().PaddingVertical(2);
                        break;
                }
            }
        }

        private static void RenderInlines(TextDescriptor text, List<InlineContent> inlines)
        {
            foreach (var inline in inlines)
            {
                if (inline.IsLink)
                {
                    text.Hyperlink(inline.Text, inline.Url!).FontColor(LinkColor);
                }
                else
                {
                    text.Span(inline.Text);
                }
            }
        }
    }
}
