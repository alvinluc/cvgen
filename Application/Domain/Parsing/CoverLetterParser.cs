using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application.Domain.Model;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Application.Domain.Parsing
{
    public class CoverLetterParser : ICoverLetterParser
    {
        private readonly MarkdownPipeline _pipeline;
        private readonly MarkdownPipeline _inlinePipeline;

        public CoverLetterParser()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .Build();

            _inlinePipeline = new MarkdownPipelineBuilder().Build();
        }

        public CoverLetterDocument Parse(string filePath)
        {
            var markdown = File.ReadAllText(filePath);
            var document = Markdown.Parse(markdown, _pipeline);

            var letter = new CoverLetterDocument();

            var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
            if (yamlBlock != null)
            {
                var yamlText = markdown.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
                yamlText = yamlText.TrimStart('-').TrimEnd('-', '.').Trim();
                ParseYamlMetadata(yamlText, letter);
            }

            foreach (var block in document)
            {
                if (block is YamlFrontMatterBlock)
                    continue;

                if (block is ParagraphBlock paragraph)
                {
                    letter.Body.Add(ParseInlines(paragraph.Inline));
                }
            }

            return letter;
        }

        private void ParseYamlMetadata(string yamlText, CoverLetterDocument letter)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .Build();

            var metadata = deserializer.Deserialize<Dictionary<string, object>>(yamlText);
            if (metadata == null) return;

            if (metadata.TryGetValue("name", out var name))
                letter.Name = name?.ToString() ?? string.Empty;

            if (metadata.TryGetValue("left-column", out var leftCol) && leftCol is List<object> leftItems)
            {
                foreach (var item in leftItems)
                    letter.Contact.LeftColumn.Add(ParseInlineMarkdown(item?.ToString() ?? string.Empty));
            }

            if (metadata.TryGetValue("right-column", out var rightCol) && rightCol is List<object> rightItems)
            {
                foreach (var item in rightItems)
                    letter.Contact.RightColumn.Add(ParseInlineMarkdown(item?.ToString() ?? string.Empty));
            }

            if (metadata.TryGetValue("date", out var date))
                letter.Date = date?.ToString() ?? string.Empty;

            if (metadata.TryGetValue("recipient", out var recipient) && recipient is List<object> recipientItems)
            {
                foreach (var item in recipientItems)
                    letter.Recipient.Add(ParseInlineMarkdown(item?.ToString() ?? string.Empty));
            }

            if (metadata.TryGetValue("subject", out var subject))
                letter.Subject = ParseInlineMarkdown(subject?.ToString() ?? string.Empty);

            if (metadata.TryGetValue("salutation", out var salutation))
                letter.Salutation = ParseInlineMarkdown(salutation?.ToString() ?? string.Empty);

            if (metadata.TryGetValue("sign-off", out var signOff))
                letter.SignOff = ParseInlineMarkdown(signOff?.ToString() ?? string.Empty);
        }

        private List<InlineContent> ParseInlineMarkdown(string text)
        {
            var doc = Markdown.Parse(text, _inlinePipeline);
            var paragraph = doc.Descendants<ParagraphBlock>().FirstOrDefault();
            if (paragraph != null)
                return ParseInlines(paragraph.Inline);

            return new List<InlineContent> { new InlineContent { Text = text } };
        }

        private static List<InlineContent> ParseInlines(ContainerInline? container, bool parentBold = false, bool parentItalic = false)
        {
            var result = new List<InlineContent>();
            if (container == null) return result;

            foreach (var inline in container)
            {
                if (inline is LinkInline link)
                {
                    var linkText = string.Empty;
                    foreach (var child in link)
                    {
                        if (child is LiteralInline lit)
                            linkText += lit.Content.ToString();
                    }
                    result.Add(new InlineContent
                    {
                        Text = linkText,
                        Url = link.Url,
                        IsBold = parentBold,
                        IsItalic = parentItalic
                    });
                }
                else if (inline is LiteralInline literal)
                {
                    result.Add(new InlineContent
                    {
                        Text = literal.Content.ToString(),
                        IsBold = parentBold,
                        IsItalic = parentItalic
                    });
                }
                else if (inline is EmphasisInline emphasis)
                {
                    var isBold = parentBold || emphasis.DelimiterCount >= 2;
                    var isItalic = parentItalic || emphasis.DelimiterCount == 1 || emphasis.DelimiterCount == 3;
                    result.AddRange(ParseInlines(emphasis, isBold, isItalic));
                }
            }

            return result;
        }
    }
}
