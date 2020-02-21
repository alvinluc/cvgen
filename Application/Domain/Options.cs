using CommandLine;

namespace Application.Domain
{
    public sealed class Options
    {
        [Option("pdf", Required = false, HelpText = "Convert to PDF format")]
        public bool Pdf { get; set; }


        [Option("docx", Required = false, HelpText = "Convert to DocX format")]
        public bool Docx { get; set; }


        [Option("txt", Required = false, HelpText = "Convert to Txt format")]
        public bool Txt { get; set; }

        [Value(0)] public string FileName { get; set; }
    }
}