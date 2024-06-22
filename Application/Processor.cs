using Application.Domain;

namespace Application
{
    internal class Processor
    {
        private readonly ICommand _command;
        private readonly IDocumentFactory _documentFactory;
        private readonly ILogger _logger;

        public Processor(ICommand command, IDocumentFactory documentFactory, ILogger logger)
        {
            _command = command;
            _documentFactory = documentFactory;
            _logger = logger;
        }

        public void Run(Options opts)
        {            
            if (!string.IsNullOrWhiteSpace(opts.FileName))
            {
                _logger.Log($"Producing CV for {opts.FileName}");
                var formatter = _documentFactory.Create(opts.FileFormat);
                var commandToExecute = formatter.Generate(opts.FileName);
                _command.Execute(commandToExecute);
                _logger.Log($"Done! File produced");
            }
            else
            {
                _logger.Log("Please supply a valid file name");
            }
        }
    }
}