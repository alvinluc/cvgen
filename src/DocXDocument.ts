import { IDocument } from "./IDocument";
import { exec } from "child_process";

export class DocXDocument implements IDocument {
  public generate(fileName: string): void {
    const command = `pandoc docs/${fileName}.md -f markdown+yaml_metadata_block --template templates/cv.latex -s -V geometry:margin=1in -o out/${fileName}.docx`;
    exec(command, (err) => {if (err) throw new Error("error converting to docx format") });
  }
}
