using Application.Domain;
using Autofac;
using CommandLine;

namespace Application
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions);
        }

        private static void RunOptions(Options opts)
        {
            var container = ContainerResolver.RegisterContainer();

            using (var scope = container.BeginLifetimeScope())
            {
                if (!string.IsNullOrWhiteSpace(opts.FileName))
                {
                    var command = scope.Resolve<ICommand>();
                    var documentFactory = scope.Resolve<IDocumentFactory>();
                    var formatter = documentFactory.Create(opts);
                    var commandToExecute = formatter.Generate(opts.FileName);
                    command.Execute(commandToExecute);
                }
                else
                {
                    var logger = scope.Resolve<ILogger>();
                    logger.Log("Please supply a valid file name");
                }
            }
        }
    }
}