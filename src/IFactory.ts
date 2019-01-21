import { IDocument } from "./IDocument";

export interface IFactory {
  createDocument(fileType: string): IDocument
}


