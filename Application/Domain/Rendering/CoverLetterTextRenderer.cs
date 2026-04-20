using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Application.Domain.Model;

namespace Application.Domain.Rendering
{
    public class CoverLetterTextRenderer : ICoverLetterRenderer
    {
        public void Render(CoverLetterDocument document, string outputPath)
        {
            var sb = new StringBuilder();

            sb.AppendLine(document.Name);
            sb.AppendLine(new string('=', document.Name.Length));
            sb.AppendLine();

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

            if (!string.IsNullOrWhiteSpace(document.Date))
            {
                sb.AppendLine(document.Date);
                sb.AppendLine();
            }

            foreach (var line in document.Recipient)
                sb.AppendLine(InlinesToString(line));
            if (document.Recipient.Count > 0) sb.AppendLine();

            if (document.Subject.Count > 0)
            {
                sb.AppendLine(InlinesToString(document.Subject));
                sb.AppendLine();
            }

            if (document.Salutation.Count > 0)
            {
                sb.AppendLine(InlinesToString(document.Salutation));
                sb.AppendLine();
            }

            foreach (var paragraph in document.Body)
            {
                sb.AppendLine(InlinesToString(paragraph));
                sb.AppendLine();
            }

            if (document.SignOff.Count > 0)
            {
                sb.AppendLine(InlinesToString(document.SignOff));
                sb.AppendLine();
                sb.AppendLine(document.Name);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, sb.ToString());
        }

        private static string InlinesToString(List<InlineContent> inlines)
        {
            return string.Join("", inlines.Select(i =>
            {
                var text = i.Text;
                if (i.IsBold && i.IsItalic) text = $"***{text}***";
                else if (i.IsBold) text = $"**{text}**";
                else if (i.IsItalic) text = $"*{text}*";
                return i.IsLink ? $"{text} ({i.Url})" : text;
            }));
        }
    }
}
