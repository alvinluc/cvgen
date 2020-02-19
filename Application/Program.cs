using System;
using Application.Domain;

namespace Application
{
    internal class Program
    {
       
        
        static void Main(string[] args)
        {
            ILogger  _logger = new ConsoleLogger();
            ICommand  _command = new ProcessCommand(_logger);
            IFormatter _formatter;      

            if (args.Length < 1)
            {
                _logger.Log("No arguments supplied...exiting!");
                Environment.Exit(-1);
            }
            else if (args.Length == 1)
            {               
                _formatter = new PdfFormatter(); 
                string commandToExecute = _formatter.Generate(args[0]);
                _command.Execute(commandToExecute);
            }
            else if (args.Length == 2)
            {
              
                IFormatFactory factory = new DocumentFactory();
                _formatter = factory.Create(args[1]);          
                string commandToExecute = _formatter.Generate(args[0]);    
                _command.Execute(commandToExecute);
            }

        }
    }
}
