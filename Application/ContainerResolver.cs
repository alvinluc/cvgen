using Application.Domain;
using Autofac;

namespace Application 
{
    internal static class ContainerResolver 
    {
        internal static IContainer RegisterContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ConsoleLogger>().As<ILogger>();
            builder.RegisterType<ProcessCommand>().As<ICommand>();
            builder.RegisterType<DocumentFactory>().As<IDocumentFactory>();

            return builder.Build();
        }
    }
}