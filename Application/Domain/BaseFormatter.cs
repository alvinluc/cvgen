using System;
using System.IO;

namespace Application.Domain
{
    public abstract class BaseFormatter : IFormatter
    {
        protected readonly string currentDirectory;
        protected readonly string inPath;
        protected readonly string outPath;
        protected readonly string templatesPath;

        protected BaseFormatter()
        {
            currentDirectory = Environment.CurrentDirectory;
            var parentDirectory = Directory.GetParent(currentDirectory) ?? throw new DirectoryNotFoundException("Parent directory not found");
            currentDirectory = parentDirectory.ToString();

            inPath = $"{currentDirectory}/in";
            outPath = $"{currentDirectory}/out";
            templatesPath = $"{currentDirectory}/templates";
        }

        public abstract string Generate(string filePath);
    }
}