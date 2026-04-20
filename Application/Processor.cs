using System;
using System.IO;
using Application.Domain;
using Application.Domain.Parsing;

namespace Application
{
    internal class Processor
    {
        private readonly IDocumentFactory _documentFactory;
        private readonly IMarkdownParser _parser;
        private readonly ICoverLetterFactory _coverLetterFactory;
        private readonly ICoverLetterParser _coverLetterParser;
        private readonly ILogger _logger;

        public Processor(
            IDocumentFactory documentFactory,
            IMarkdownParser parser,
            ICoverLetterFactory coverLetterFactory,
            ICoverLetterParser coverLetterParser,
            ILogger logger)
        {
            _documentFactory = documentFactory;
            _parser = parser;
            _coverLetterFactory = coverLetterFactory;
            _coverLetterParser = coverLetterParser;
            _logger = logger;
        }

        public void Run(Options opts)
        {
            var fileName = opts.File?.ToLower();

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentNullException("Please supply a valid file name");

            var fileExtension = opts.Format?.ToLower() ?? "pdf";
            var currentDirectory = Environment.CurrentDirectory;

            var inputPath = Path.Combine(currentDirectory, "in", $"{fileName}.md");
            var outputPath = Path.Combine(currentDirectory, "out", $"{fileName}.{fileExtension}");

            if (opts.CoverLetter)
            {
                _logger.Log($"Producing covering letter for {fileName}");
                var letter = _coverLetterParser.Parse(inputPath);
                var renderer = _coverLetterFactory.Create(fileExtension);
                renderer.Render(letter, outputPath);
            }
            else
            {
                _logger.Log($"Producing CV for {fileName}");
                var cvDocument = _parser.Parse(inputPath);
                var renderer = _documentFactory.Create(fileExtension);
                renderer.Render(cvDocument, outputPath);
            }

            _logger.Log("Done! File produced");
        }
    }
}
