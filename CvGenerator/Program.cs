using CvGenerator;
using CvGenerator.Docx;
using CvGenerator.Model;
using CvGenerator.Pdf;

try
{
    var cli = Cli.Parse(args);
    if (cli is null)
    {
        return 0;
    }

    var document = TomlLoader.Load(cli.InputPath);

    switch (cli.Format)
    {
        case Format.Pdf:
            PdfRenderer.Render(document, cli.OutputPath);
            break;
        case Format.Ats:
            DocxRenderer.RenderAts(document, cli.OutputPath);
            break;
        case Format.Doc:
        case Format.Docx:
            DocxRenderer.Render(document, cli.OutputPath);
            break;
        default:
            throw new GeneratorException($"Unsupported format: {cli.Format}");
    }

    Console.WriteLine($"Generated {cli.OutputPath}");
    return 0;
}
catch (GeneratorException error)
{
    Console.Error.WriteLine($"error: {error.Message}");
    return 1;
}
