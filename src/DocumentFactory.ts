import { IDocument } from "./IDocument";
import { TextDocument } from "./TextDocument";
import { PdfDocument } from "./PdfDocument";
import { DocXDocument } from "./DocXDocument";
import { IFactory } from "./IFactory";


export class DocumentFactory implements IFactory {
  createDocument(fileType: string): IDocument {
    switch (fileType) {
      case "pdf":
        return new PdfDocument;
      case "doc":
        return new DocXDocument;
      case "txt":
        return new TextDocument;
      default:
        return new PdfDocument;
    }
  }
}
