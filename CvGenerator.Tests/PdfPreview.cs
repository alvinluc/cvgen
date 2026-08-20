using CvGenerator.Model;
using CvGenerator.Pdf;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CvGenerator.Tests;

/// <summary>
/// Rasterises the sample inputs into PNGs so a layout change can be eyeballed.
/// Off by default; set CVGEN_PREVIEW_DIR to a writable directory to run it.
/// </summary>
public class PdfPreview
{
    [Fact]
    public void RenderSampleInputsToImages()
    {
        var directory = Environment.GetEnvironmentVariable("CVGEN_PREVIEW_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);

        foreach (var path in Directory.GetFiles(InputDirectory(), "*.toml"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            PdfRenderer.Build(TomlLoader.Load(path)).GenerateImages(
                index => Path.Combine(directory, $"{name}-{index + 1}.png"),
                new ImageGenerationSettings { RasterDpi = 120 });
        }
    }

    /// <summary>
    /// The repository's <c>input/</c> directory. Tests run from their output
    /// directory, so walk up to find it rather than making the caller pass a
    /// path relative to somewhere non-obvious. CVGEN_INPUT_DIR overrides.
    /// </summary>
    private static string InputDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("CVGEN_INPUT_DIR");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "input");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not find an 'input' directory above the test output; set CVGEN_INPUT_DIR.");
    }
}
