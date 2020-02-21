namespace Application.Domain
{
    public class DocXFormatter : BaseFormatter
    {
        public override string Generate(string fileName)
        {
            return
                $"{inPath}/{fileName}.md -f markdown+yaml_metadata_block --template {templatesPath}/cv.latex -s -V geometry:margin=1in -o {outPath}/{fileName}.docx";
        }
    }
}