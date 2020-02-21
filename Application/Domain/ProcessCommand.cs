using System;
using System.Diagnostics;

namespace Application.Domain
{
    public class ProcessCommand : ICommand
    {
        private readonly ILogger _logger;

        public ProcessCommand(ILogger logger)
        {
            _logger = logger;
        }

        public void Execute(string command)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/pandoc",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };


                var process = new Process();
                process.StartInfo = processStartInfo;
                process.Start();
                process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                _logger.Log(e.Message);
            }
        }
    }
}