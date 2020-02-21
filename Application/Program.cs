using Application.Domain;
using Autofac;
using CommandLine;

namespace Application
{
    internal class Program
    {              
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions);     
           
        }

        private static void RunOptions(Options opts) 
        {
            var container = ContainerResolver.RegisterContainer();

            using (var scope = container.BeginLifetimeScope())
            {
                var command = scope.Resolve<ICommand>();
                var logger = scope.Resolve<ILogger>();
                var documentFactory = scope.Resolve<IDocumentFactory>();

                if (!string.IsNullOrWhiteSpace(opts.FileName))
                {
                    var formatter = documentFactory.Create(opts);
                    var  commandToExecute = formatter.Generate(opts.FileName);
                    command.Execute(commandToExecute);
                }
                else 
                {
                    logger.Log("Please supply a valid file name");
                }             
            }  


        }
    }
}
