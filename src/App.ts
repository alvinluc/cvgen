import { DocumentFactory } from "./DocumentFactory";

parseArguments(process.argv)


function parseArguments(args : string[]) {
  if (args.length < 4) return;

  const fileName = args[2].toLowerCase();
  const fileType = args[3].toLowerCase();
  
  const factory = new DocumentFactory();
  const document = factory.createDocument(fileType);
  document.generate(fileName);
}