using System.IO;
using System.Linq;
using System.Text;
using Application.Domain.Model;

namespace Application.Domain.Rendering
{
    public class TextRenderer : IDocumentRenderer
    {
        public void Render(CvDocument document, string outputPath)
        {
            var sb = new StringBuilder();

            // Name
            sb.AppendLine(document.Name);
            sb.AppendLine(new string('=', document.Name.Length));
            sb.AppendLine();

            // Contact info
            var left = document.Contact.LeftColumn;
            var right = document.Contact.RightColumn;
            var maxRows = System.Math.Max(left.Count, right.Count);
            for (int i = 0; i < maxRows; i++)
            {
                var leftText = i < left.Count ? InlinesToString(left[i]) : string.Empty;
                var rightText = i < right.Count ? InlinesToString(right[i]) : string.Empty;

                if (!string.IsNullOrEmpty(rightText))
                    sb.AppendLine($"{leftText,-40} {rightText}");
                else
                    sb.AppendLine(leftText);
            }
            sb.AppendLine();

            // Sections
            foreach (var section in document.Sections)
            {
                sb.AppendLine(section.Title);
                sb.AppendLine(new string('-', section.Title.Length));
                sb.AppendLine();

                RenderContent(sb, section.Content, "");

                foreach (var sub in section.SubSections)
                {
                    sb.AppendLine(InlinesToString(sub.Title));
                    sb.AppendLine();
                    RenderContent(sb, sub.Content, "");
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, sb.ToString());
        }

        private static void RenderContent(StringBuilder sb, System.Collections.Generic.List<ContentBlock> blocks, string indent)
        {
            foreach (var block in blocks)
            {
                switch (block.Type)
                {
                    case ContentBlockType.Paragraph:
                        foreach (var line in block.Items)
                        {
                            sb.AppendLine($"{indent}{InlinesToString(line)}");
                        }
                        sb.AppendLine();
                        break;

                    case ContentBlockType.List:
                        foreach (var item in block.Items)
                        {
                            sb.AppendLine($"{indent}-- {InlinesToString(item)}");
                        }
                        sb.AppendLine();
                        break;

                    case ContentBlockType.DefinitionList:
                        foreach (var (term, definition) in block.Definitions)
                        {
                            sb.AppendLine($"{indent}{InlinesToString(term)} {InlinesToString(definition)}");
                        }
                        sb.AppendLine();
                        break;
                }
            }
        }

        private static string InlinesToString(System.Collections.Generic.List<InlineContent> inlines)
        {
            return string.Join("", inlines.Select(i => i.IsLink ? $"{i.Text} ({i.Url})" : i.Text));
        }
    }
}
