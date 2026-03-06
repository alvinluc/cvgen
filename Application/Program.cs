using System;
using Application.Domain;
using Application.Domain.Parsing;
using Microsoft.Extensions.DependencyInjection;
using CommandLine;
using QuestPDF.Infrastructure;

namespace Application
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Console.WriteLine("Starting..");
            var services = CreateServiceProvider();
            var processor = services.GetRequiredService<Processor>();
            Parser.Default.ParseArguments<Options>(args).WithParsed(processor.Run);
        }

        private static ServiceProvider CreateServiceProvider()
        {
            var serviceProvider = new ServiceCollection()
                .AddSingleton<ILogger, ConsoleLogger>()
                .AddSingleton<IMarkdownParser, MarkdownParser>()
                .AddSingleton<IDocumentFactory, DocumentFactory>()
                .AddSingleton<Processor>()
                .BuildServiceProvider();

            return serviceProvider;
        }
    }
}
