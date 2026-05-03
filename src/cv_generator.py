#!/usr/bin/env python3
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
import tempfile
import tomllib
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape as xml_escape


SUPPORTED_FORMATS = {"pdf", "docx", "doc"}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Generate a CV or cover letter from a TOML profile.")
    parser.add_argument("name", help="Input file name without .toml")
    parser.add_argument("format", nargs="?", default="pdf", choices=sorted(SUPPORTED_FORMATS))
    parser.add_argument("-i", "--input-dir", default="input")
    parser.add_argument("-o", "--output-dir", default="output")
    args = parser.parse_args(argv)

    output_format = "docx" if args.format == "doc" else args.format
    input_path = Path(args.input_dir) / f"{args.name}.toml"
    output_path = Path(args.output_dir) / f"{args.name}.{output_format}"

    document = load_toml(input_path)
    document_type = document.get("type", "cv")

    if output_format == "pdf":
        TypstRenderer().render(document, document_type, output_path)
    else:
        DocxRenderer().render(document, document_type, output_path)

    print(f"Generated {output_path}")
    return 0


def load_toml(path: Path) -> dict:
    if not path.exists():
        raise FileNotFoundError(f"Input file not found: {path}")
    with path.open("rb") as file:
        return tomllib.load(file)


def clean_lines(values: list[str] | None) -> list[str]:
    return [value.strip() for value in values or [] if value and value.strip()]


def contact_line(document: dict) -> str:
    parts = []
    for item in document.get("contact", []):
        label = item.get("label", "").strip()
        value = item.get("value", "").strip()
        if not value:
            continue
        parts.append(f"{label}: {value}" if label else value)
    return "  |  ".join(parts)


def join_items(values: list[str] | None) -> str:
    return ", ".join(clean_lines(values))


class TypstRenderer:
    def render(self, document: dict, document_type: str, output_path: Path) -> None:
        if shutil.which("typst") is None:
            raise RuntimeError("Typst is required for PDF output. Install the `typst` CLI and try again.")

        source = self.cover_letter(document) if document_type == "cover-letter" else self.cv(document)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        with tempfile.TemporaryDirectory(prefix="cv-generator-") as temp_dir:
            typst_path = Path(temp_dir) / f"{output_path.stem}.typ"
            typst_path.write_text(source, encoding="utf-8")
            subprocess.run(["typst", "compile", str(typst_path), str(output_path)], check=True)

    def cv(self, document: dict) -> str:
        lines = [self.preamble(document, document_kind="cv")]

        self.section(lines, "Profile")
        self.paragraph(lines, document.get("summary", ""))

        self.section(lines, "Experience")
        for role in document.get("experience", []):
            self.entry(lines, role.get("role", ""), role.get("company", ""), role.get("dates", ""), role.get("location", ""))
            self.paragraph(lines, role.get("summary", ""))
            self.bullets(lines, role.get("highlights", []))
            tech = join_items(role.get("technologies", []))
            if tech:
                lines.append(f'#block(above: 0.15em, below: 0.35em)[#label-text("Tools", "{t(tech)}")]')
            for earlier in role.get("progression", []):
                self.entry(lines, earlier.get("role", ""), role.get("company", ""), earlier.get("dates", ""), earlier.get("location", ""), compact=True)
                self.paragraph(lines, earlier.get("summary", ""))
                self.bullets(lines, earlier.get("highlights", []))

        self.section(lines, "Skills")
        for group in document.get("skills", []):
            self.skill_group(lines, group.get("name", ""), group.get("items", []))

        self.section(lines, "Education & Certifications")
        for item in document.get("education", []):
            self.entry(lines, item.get("name", ""), item.get("institution", ""), item.get("dates", ""), "", compact=True)
            self.paragraph(lines, item.get("detail", ""))

        for group in document.get("additional", []):
            self.section(lines, group.get("name", "Additional"))
            self.bullets(lines, group.get("items", []))

        return "\n".join(lines)

    def cover_letter(self, document: dict) -> str:
        lines = [self.preamble(document, document_kind="letter")]
        self.paragraph(lines, document.get("date", ""))
        for recipient in clean_lines(document.get("recipient", [])):
            lines.append(f'#block(below: 0.08em)[#text(size: 9.6pt, "{t(recipient)}")]')
        if document.get("subject"):
            lines.append(f'#block(above: 0.9em, below: 0.8em)[#text(weight: 700, fill: accent, "{t(document["subject"])}")]')
        self.paragraph(lines, document.get("salutation", ""))
        for paragraph in clean_lines(document.get("body", [])):
            self.paragraph(lines, paragraph)
        self.paragraph(lines, document.get("sign_off", ""))
        self.paragraph(lines, document.get("name", ""))
        return "\n".join(lines)

    def preamble(self, document: dict, document_kind: str) -> str:
        header_gap = "1.0em" if document_kind == "letter" else "0.55em"
        title_size = "24pt" if document_kind == "cv" else "22pt"
        pieces = [
            '#let accent = rgb("#165a72")',
            '#let ink = rgb("#17212b")',
            '#let muted = rgb("#5c6875")',
            '#let soft = rgb("#eef5f7")',
            '#let rule = rgb("#d8e4e8")',
            '#let chip(label) = box(inset: (x: 0.46em, y: 0.16em), radius: 0.75em, fill: soft, stroke: rule + 0.35pt)[#text(size: 8.2pt, fill: accent, label)]',
            '#let label-text(label, value) = text(size: 8.5pt, fill: muted)[#text(weight: 700, fill: accent, label + ": ") + value]',
            '#set page(paper: "a4", margin: (x: 1.32cm, y: 1.24cm))',
            '#set text(font: "Libertinus Serif", size: 9.55pt, fill: ink, lang: "en")',
            '#set par(justify: true, leading: 0.5em)',
            '#show heading.where(level: 1): it => block(above: 0.78em, below: 0.42em)[#grid(columns: (auto, 1fr), gutter: 0.7em, align: horizon)[#text(size: 9.2pt, weight: 700, fill: accent, upper(it.body))][#line(length: 100%, stroke: rule + 0.55pt)]]',
            '#show list: set block(spacing: 0.28em)',
            f'#align(center)[#text(size: {title_size}, weight: 700, fill: ink, "{t(document.get("name", ""))}")]',
        ]
        if document.get("headline"):
            pieces.append(f'#align(center)[#text(size: 9.4pt, fill: accent, "{t(document["headline"])}")]')
        if contact_line(document):
            pieces.append(f'#align(center)[#text(size: 8pt, fill: muted, "{t(contact_line(document))}")]')
        pieces.append('#align(center)[#line(length: 38%, stroke: accent + 0.7pt)]')
        pieces.append(f"#v({header_gap})")
        return "\n".join(pieces)

    @staticmethod
    def section(lines: list[str], title: str) -> None:
        if title:
            lines.append(f"\n= {title}")

    @staticmethod
    def paragraph(lines: list[str], value: str) -> None:
        if value and value.strip():
            lines.append(f'#block(below: 0.48em)[#text("{t(value.strip())}")]')

    @staticmethod
    def entry(lines: list[str], role: str, company: str, dates: str, location: str, compact: bool = False) -> None:
        when = " · ".join(clean_lines([dates, location]))
        left = f"{role} — {company}" if company else role
        gap = "0.18em" if compact else "0.34em"
        lines.append(f"#v({gap})")
        lines.append(f'#grid(columns: (1fr, auto), gutter: 1em)[#text(weight: 700, fill: ink, "{t(left)}")][#text(size: 8.2pt, fill: muted, "{t(when)}")]')

    @staticmethod
    def key_value(lines: list[str], key: str, value: str) -> None:
        if key and value:
            lines.append(f'#block(below: 0.25em)[#text(weight: 700, "{t(key)}: ")#text("{t(value)}")]')

    @staticmethod
    def skill_group(lines: list[str], key: str, values: list[str]) -> None:
        items = clean_lines(values)
        if not key or not items:
            return
        chips = " ".join(f'#chip("{t(item)}")' for item in items)
        lines.append(f'#block(below: 0.5em)[#text(weight: 700, fill: accent, "{t(key)}") #h(0.5em) {chips}]')

    @staticmethod
    def bullets(lines: list[str], values: list[str]) -> None:
        items = clean_lines(values)
        if not items:
            return
        lines.append("#list(")
        for item in items:
            lines.append(f'  [#text("{t(item)}")],')
        lines.append(")")


class DocxRenderer:
    def render(self, document: dict, document_type: str, output_path: Path) -> None:
        body = self.cover_letter(document) if document_type == "cover-letter" else self.cv(document)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        write_docx(output_path, body)

    def cv(self, document: dict) -> str:
        body = [
            paragraph(document.get("name", ""), "Title"),
            paragraph(document.get("headline", ""), "Subtitle"),
            paragraph(contact_line(document), "Contact"),
            paragraph("", "Spacer"),
            heading("Profile"),
            paragraph(document.get("summary", "")),
            heading("Experience"),
        ]
        for role in document.get("experience", []):
            body.append(paragraph(f'{role.get("role", "")} — {role.get("company", "")}', "EntryTitle"))
            body.append(paragraph(" · ".join(clean_lines([role.get("dates", ""), role.get("location", "")])), "Meta"))
            body.append(paragraph(role.get("summary", "")))
            body.extend(bullet(item) for item in clean_lines(role.get("highlights", [])))
            tech = join_items(role.get("technologies", []))
            if tech:
                body.append(paragraph(f"Technologies: {tech}", "Meta"))
            for earlier in role.get("progression", []):
                body.append(paragraph(f'{earlier.get("role", "")} — {role.get("company", "")}', "EntryTitle"))
                body.append(paragraph(" · ".join(clean_lines([earlier.get("dates", ""), earlier.get("location", "")])), "Meta"))
                body.append(paragraph(earlier.get("summary", "")))
                body.extend(bullet(item) for item in clean_lines(earlier.get("highlights", [])))

        body.append(heading("Skills"))
        for group in document.get("skills", []):
            body.append(paragraph(f'{group.get("name", "")}: {join_items(group.get("items", []))}'))

        body.append(heading("Education & Certifications"))
        for item in document.get("education", []):
            body.append(paragraph(f'{item.get("name", "")} — {item.get("institution", "")}', "EntryTitle"))
            body.append(paragraph(item.get("dates", ""), "Meta"))
            body.append(paragraph(item.get("detail", "")))

        for group in document.get("additional", []):
            body.append(heading(group.get("name", "Additional")))
            body.extend(bullet(item) for item in clean_lines(group.get("items", [])))

        return "".join(body)

    def cover_letter(self, document: dict) -> str:
        body = [
            paragraph(document.get("name", ""), "Title"),
            paragraph(document.get("headline", ""), "Subtitle"),
            paragraph(contact_line(document), "Contact"),
            paragraph("", "Spacer"),
            paragraph(document.get("date", "")),
        ]
        body.extend(paragraph(line, "NoGap") for line in clean_lines(document.get("recipient", [])))
        body.append(paragraph(document.get("subject", ""), "EntryTitle"))
        body.append(paragraph(document.get("salutation", "")))
        body.extend(paragraph(item) for item in clean_lines(document.get("body", [])))
        body.append(paragraph(document.get("sign_off", "")))
        body.append(paragraph(document.get("name", "")))
        return "".join(body)


def t(value: str) -> str:
    return (
        value.replace("\\", "\\\\")
        .replace('"', '\\"')
        .replace("\r", "")
        .replace("\n", " ")
    )


def paragraph(text: str, style: str = "Body") -> str:
    if style != "Spacer" and not text:
        return ""
    return f'<w:p><w:pPr><w:pStyle w:val="{style}"/></w:pPr><w:r><w:t xml:space="preserve">{xml_escape(text)}</w:t></w:r></w:p>'


def heading(text: str) -> str:
    return paragraph(text.upper(), "Heading")


def bullet(text: str) -> str:
    return f'<w:p><w:pPr><w:pStyle w:val="Bullet"/><w:ind w:left="360" w:hanging="180"/></w:pPr><w:r><w:t>• </w:t></w:r><w:r><w:t xml:space="preserve">{xml_escape(text)}</w:t></w:r></w:p>'


def write_docx(path: Path, body: str) -> None:
    document_xml = f'''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    {body}
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="850" w:right="900" w:bottom="850" w:left="900" w:header="720" w:footer="720" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>'''
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as docx:
        docx.writestr("[Content_Types].xml", CONTENT_TYPES)
        docx.writestr("_rels/.rels", ROOT_RELS)
        docx.writestr("word/_rels/document.xml.rels", DOCUMENT_RELS)
        docx.writestr("word/document.xml", document_xml)
        docx.writestr("word/styles.xml", STYLES)


CONTENT_TYPES = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
</Types>'''

ROOT_RELS = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>'''

DOCUMENT_RELS = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>'''

STYLES = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:styleId="Normal"><w:name w:val="Normal"/><w:rPr><w:rFonts w:ascii="Aptos" w:hAnsi="Aptos"/><w:sz w:val="20"/></w:rPr><w:pPr><w:spacing w:after="90"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Body"><w:name w:val="Body"/><w:basedOn w:val="Normal"/><w:pPr><w:spacing w:after="100"/><w:jc w:val="both"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:rPr><w:rFonts w:ascii="Aptos Display" w:hAnsi="Aptos Display"/><w:b/><w:sz w:val="46"/></w:rPr><w:pPr><w:jc w:val="center"/><w:spacing w:after="20"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Subtitle"><w:name w:val="Subtitle"/><w:rPr><w:color w:val="56657A"/><w:sz w:val="19"/></w:rPr><w:pPr><w:jc w:val="center"/><w:spacing w:after="20"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Contact"><w:name w:val="Contact"/><w:rPr><w:color w:val="56657A"/><w:sz w:val="17"/></w:rPr><w:pPr><w:jc w:val="center"/><w:spacing w:after="180"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading"><w:name w:val="Heading"/><w:rPr><w:b/><w:color w:val="23395B"/><w:sz w:val="22"/></w:rPr><w:pPr><w:spacing w:before="180" w:after="80"/><w:pBdr><w:bottom w:val="single" w:sz="5" w:space="2" w:color="D7DEE8"/></w:pBdr></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="EntryTitle"><w:name w:val="EntryTitle"/><w:rPr><w:b/><w:sz w:val="20"/></w:rPr><w:pPr><w:spacing w:before="70" w:after="10"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Meta"><w:name w:val="Meta"/><w:rPr><w:color w:val="56657A"/><w:sz w:val="18"/></w:rPr><w:pPr><w:spacing w:after="50"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Bullet"><w:name w:val="Bullet"/><w:basedOn w:val="Body"/><w:pPr><w:spacing w:after="55"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="NoGap"><w:name w:val="NoGap"/><w:basedOn w:val="Body"/><w:pPr><w:spacing w:after="0"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Spacer"><w:name w:val="Spacer"/><w:pPr><w:spacing w:after="80"/></w:pPr></w:style>
</w:styles>'''


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"error: {error}", file=sys.stderr)
        raise SystemExit(1)
