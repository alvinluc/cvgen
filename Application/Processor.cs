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

            var fileName = opts.File?.ToLower();

            if (string.IsNullOrWhiteSpace(fileName))
                 throw new ArgumentNullException("Please supply a valid file name");


            var fileExtension = opts.Format?.ToLower() ?? "pdf";

            var currentDirectory = Environment.CurrentDirectory;

            _logger.Log($"Producing CV for {fileName}");
            var inputPath = Path.Combine(currentDirectory, "in", $"{fileName}.md");
            var outputPath = Path.Combine(currentDirectory, "out", $"{fileName}.{fileExtension}");

            var cvDocument = _parser.Parse(inputPath);
            var renderer = _documentFactory.Create(fileExtension);
            renderer.Render(cvDocument, outputPath);

            _logger.Log("Done! File produced");




        }
    }
}
