import { DocumentFactory } from "./DocumentFactory";

const { argv } = process;
const argumentLength = argv.length;

if (argumentLength > 3) {
  const fileName = argv[3].toLowerCase();
  const fileType = argv[2].toLowerCase();

  const factory = new DocumentFactory();
  const document = factory.createDocument(fileType);

  document.generate(fileName);


} else {
  console.log("Invalid arguments. Please check again");
}

