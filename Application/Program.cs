using System;
using Application.Domain;
using Microsoft.Extensions.DependencyInjection;
using CommandLine;

namespace Application
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting..");
            var services = CreateServiceProvider();
            var processor = services.GetRequiredService<Processor>();
            Parser.Default.ParseArguments<Options>(args).WithParsed(processor.Run);
        }


        private static ServiceProvider CreateServiceProvider()
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<ILogger, ConsoleLogger>()
                .AddSingleton<ICommand, ProcessCommand>()
                .AddSingleton<IDocumentFactory, DocumentFactory>()
                .AddSingleton<Processor>()
                .BuildServiceProvider();

            return serviceProvider;
        }

    }
}