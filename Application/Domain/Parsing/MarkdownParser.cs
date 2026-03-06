using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Application.Domain.Model;
using Markdig;
using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Application.Domain.Parsing
{
    public class MarkdownParser : IMarkdownParser
    {
        private readonly MarkdownPipeline _pipeline;
        private readonly MarkdownPipeline _inlinePipeline;

        public MarkdownParser()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDefinitionLists()
                .Build();

            _inlinePipeline = new MarkdownPipelineBuilder().Build();
        }

        public CvDocument Parse(string filePath)
        {
            var markdown = File.ReadAllText(filePath);
            // Markdig requires `:   ` (3+ spaces) for definition lists; normalize `: ` to `:   `
            markdown = Regex.Replace(markdown, @"^: (\S)", ":   $1", RegexOptions.Multiline);
            var document = Markdown.Parse(markdown, _pipeline);

            var cv = new CvDocument();

            // Extract YAML front matter
            var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
            if (yamlBlock != null)
            {
                var yamlText = markdown.Substring(yamlBlock.Span.Start, yamlBlock.Span.Length);
                // Remove the --- delimiters
                yamlText = yamlText.TrimStart('-').TrimEnd('-', '.').Trim();
                ParseYamlMetadata(yamlText, cv);
            }

            // Walk the AST to build sections
            CvSection? currentSection = null;
            CvSubSection? currentSubSection = null;

            foreach (var block in document)
            {
                if (block is YamlFrontMatterBlock)
                    continue;

                if (block is HeadingBlock heading)
                {
                    if (heading.Level == 1)
                    {
                        currentSection = new CvSection
                        {
                            Title = heading.Inline?.FirstChild?.ToString() ?? string.Empty
                        };
                        cv.Sections.Add(currentSection);
                        currentSubSection = null;
                    }
                    else if (heading.Level == 2 && currentSection != null)
                    {
                        currentSubSection = new CvSubSection
                        {
                            Title = ParseInlines(heading.Inline)
                        };
                        currentSection.SubSections.Add(currentSubSection);
                    }
                }
                else if (block is ParagraphBlock paragraph)
                {
                    var contentBlock = new ContentBlock
                    {
                        Type = ContentBlockType.Paragraph,
                        Items = { ParseInlines(paragraph.Inline) }
                    };
                    AddContentToCurrentContext(contentBlock, currentSection, currentSubSection);
                }
                else if (block is ListBlock list)
                {
                    var contentBlock = new ContentBlock
                    {
                        Type = ContentBlockType.List
                    };
                    foreach (var item in list)
                    {
                        if (item is ListItemBlock listItem)
                        {
                            var inlines = new List<InlineContent>();
                            foreach (var subBlock in listItem)
                            {
                                if (subBlock is ParagraphBlock p)
                                {
                                    inlines.AddRange(ParseInlines(p.Inline));
                                }
                            }
                            contentBlock.Items.Add(inlines);
                        }
                    }
                    AddContentToCurrentContext(contentBlock, currentSection, currentSubSection);
                }
                else if (block is DefinitionList defList)
                {
                    var contentBlock = new ContentBlock
                    {
                        Type = ContentBlockType.DefinitionList
                    };

                    foreach (var item in defList)
                    {
                        if (item is DefinitionItem defItem)
                        {
                            var termInlines = new List<InlineContent>();
                            var defInlines = new List<InlineContent>();

                            foreach (var subBlock in defItem)
                            {
                                if (subBlock is DefinitionTerm term)
                                {
                                    termInlines.AddRange(ParseInlines(term.Inline));
                                }
                                else if (subBlock is ParagraphBlock p)
                                {
                                    defInlines.AddRange(ParseInlines(p.Inline));
                                }
                            }

                            contentBlock.Definitions.Add((termInlines, defInlines));
                        }
                    }
                    AddContentToCurrentContext(contentBlock, currentSection, currentSubSection);
                }
            }

            return cv;
        }

        private void ParseYamlMetadata(string yamlText, CvDocument cv)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(HyphenatedNamingConvention.Instance)
                .Build();

            var metadata = deserializer.Deserialize<Dictionary<string, object>>(yamlText);
            if (metadata == null) return;

            if (metadata.TryGetValue("name", out var name))
                cv.Name = name?.ToString() ?? string.Empty;

            if (metadata.TryGetValue("left-column", out var leftCol) && leftCol is List<object> leftItems)
            {
                foreach (var item in leftItems)
                {
                    cv.Contact.LeftColumn.Add(ParseInlineMarkdown(item?.ToString() ?? string.Empty));
                }
            }

            if (metadata.TryGetValue("right-column", out var rightCol) && rightCol is List<object> rightItems)
            {
                foreach (var item in rightItems)
                {
                    cv.Contact.RightColumn.Add(ParseInlineMarkdown(item?.ToString() ?? string.Empty));
                }
            }
        }

        private List<InlineContent> ParseInlineMarkdown(string text)
        {
            // Parse a string through Markdig to extract inline elements (links, etc.)
            var doc = Markdown.Parse(text, _inlinePipeline);
            var paragraph = doc.Descendants<ParagraphBlock>().FirstOrDefault();
            if (paragraph != null)
                return ParseInlines(paragraph.Inline);

            return new List<InlineContent> { new InlineContent { Text = text } };
        }

        private static List<InlineContent> ParseInlines(ContainerInline? container)
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
                        Url = link.Url
                    });
                }
                else if (inline is LiteralInline literal)
                {
                    result.Add(new InlineContent
                    {
                        Text = literal.Content.ToString()
                    });
                }
                else if (inline is EmphasisInline emphasis)
                {
                    // Flatten emphasis content
                    foreach (var child in emphasis)
                    {
                        if (child is LiteralInline lit)
                        {
                            result.Add(new InlineContent
                            {
                                Text = lit.Content.ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }

        private static void AddContentToCurrentContext(ContentBlock content, CvSection? section, CvSubSection? subSection)
        {
            if (subSection != null)
                subSection.Content.Add(content);
            else if (section != null)
                section.Content.Add(content);
            // Content before any section is ignored
        }
    }
}
