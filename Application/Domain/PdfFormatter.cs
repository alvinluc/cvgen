namespace Application.Domain
{
    public class PdfFormatter : BaseFormatter
    {
        public override string Generate(string fileName)
        {
            return
                $"{inPath}/{fileName}.md -f markdown+yaml_metadata_block --template {templatesPath}/cv.latex -o {outPath}/{fileName}.pdf";
        }
    }
}