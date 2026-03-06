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
        private readonly ILogger _logger;

        public Processor(IDocumentFactory documentFactory, IMarkdownParser parser, ILogger logger)
        {
            _documentFactory = documentFactory;
            _parser = parser;
            _logger = logger;
        }

        public void Run(Options opts)
        {
            if (!string.IsNullOrWhiteSpace(opts.FileName))
            {
                _logger.Log($"Producing CV for {opts.FileName}");

                var currentDirectory = Environment.CurrentDirectory;
                var inputPath = Path.Combine(currentDirectory, "in", $"{opts.FileName}.md");
                var format = opts.FileFormat?.ToLower() ?? "pdf";
                var extension = format == "doc" ? "docx" : format == "text" ? "txt" : format;
                var outputPath = Path.Combine(currentDirectory, "out", $"{opts.FileName}.{extension}");

                var cvDocument = _parser.Parse(inputPath);
                var renderer = _documentFactory.Create(opts.FileFormat);
                renderer.Render(cvDocument, outputPath);

                _logger.Log("Done! File produced");
            }
            else
            {
                _logger.Log("Please supply a valid file name");
            }
        }
    }
}
