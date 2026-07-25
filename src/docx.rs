use std::fs::{self, File};
use std::io::Write;
use std::path::Path;

use zip::write::SimpleFileOptions;
use zip::{CompressionMethod, ZipWriter};

use crate::model::{Document, clean_lines, join_items, join_parts};

/// Render a document to a minimal, dependency-free WordprocessingML package.
pub fn render(document: &Document, output_path: &Path) -> Result<(), String> {
    let body = if document.is_cover_letter() {
        cover_letter(document)
    } else {
        cv(document)
    };

    if let Some(parent) = output_path.parent() {
        fs::create_dir_all(parent).map_err(|error| format!("{}: {error}", parent.display()))?;
    }
    write_docx(output_path, &body).map_err(|error| format!("{}: {error}", output_path.display()))
}

fn cv(document: &Document) -> String {
    let mut body = vec![
        paragraph(&document.name, "Title"),
        paragraph(&document.headline, "Subtitle"),
        paragraph(&document.contact_line(), "Contact"),
        paragraph("", "Spacer"),
        heading("Profile"),
        paragraph(&document.summary, "Body"),
        heading("Experience"),
    ];

    let mut has_previous_experience_block = false;
    for role in &document.experience {
        if has_previous_experience_block {
            body.push(paragraph("", "ExperienceBreak"));
        }
        body.push(paragraph(
            &format!("{} — {}", role.role, role.company),
            "EntryTitle",
        ));
        body.push(paragraph(
            &join_parts(&[&role.dates, &role.location], " · "),
            "Meta",
        ));
        body.push(paragraph(&role.summary, "Body"));
        body.extend(clean_lines(&role.highlights).into_iter().map(bullet));
        let tech = join_items(&role.technologies);
        if !tech.is_empty() {
            body.push(paragraph(&format!("Technologies: {tech}"), "Meta"));
        }
        has_previous_experience_block = true;
        for earlier in &role.progression {
            body.push(paragraph("", "ExperienceBreak"));
            body.push(paragraph(
                &format!("{} — {}", earlier.role, role.company),
                "EntryTitle",
            ));
            body.push(paragraph(
                &join_parts(&[&earlier.dates, &earlier.location], " · "),
                "Meta",
            ));
            body.push(paragraph(&earlier.summary, "Body"));
            body.extend(clean_lines(&earlier.highlights).into_iter().map(bullet));
        }
    }

    body.push(heading("Skills"));
    for group in &document.skills {
        body.push(paragraph(
            &format!("{}: {}", group.name, join_items(&group.items)),
            "Body",
        ));
    }

    body.push(heading("Education & Certifications"));
    for item in &document.education {
        body.push(paragraph(
            &format!("{} — {}", item.name, item.institution),
            "EntryTitle",
        ));
        body.push(paragraph(&item.dates, "Meta"));
        body.push(paragraph(&item.detail, "Body"));
    }

    for group in &document.additional {
        body.push(heading(group.name.as_deref().unwrap_or("Additional")));
        body.extend(clean_lines(&group.items).into_iter().map(bullet));
    }

    body.concat()
}

/// A letter carries no header block — no name, headline or contact line — so
/// the correspondence itself is the whole document.
fn cover_letter(document: &Document) -> String {
    let mut body = vec![paragraph(&document.date, "Body")];
    body.extend(
        clean_lines(&document.recipient)
            .into_iter()
            .map(|line| paragraph(line, "NoGap")),
    );
    body.push(paragraph(&document.subject, "EntryTitle"));
    body.push(paragraph(&document.salutation, "Body"));
    body.extend(
        clean_lines(&document.body)
            .into_iter()
            .map(|text| paragraph(text, "Body")),
    );
    body.push(paragraph(&document.sign_off, "SignOff"));
    body.push(paragraph(&document.name, "Body"));
    body.concat()
}

fn paragraph(text: &str, style: &str) -> String {
    if !matches!(style, "Spacer" | "ExperienceBreak") && text.is_empty() {
        return String::new();
    }
    format!(
        "<w:p><w:pPr><w:pStyle w:val=\"{style}\"/></w:pPr><w:r><w:t xml:space=\"preserve\">{}</w:t></w:r></w:p>",
        xml_escape(text)
    )
}

fn heading(text: &str) -> String {
    paragraph(&text.to_uppercase(), "Heading")
}

fn bullet(text: &str) -> String {
    format!(
        "<w:p><w:pPr><w:pStyle w:val=\"Bullet\"/><w:ind w:left=\"360\" w:hanging=\"180\"/></w:pPr><w:r><w:t>• </w:t></w:r><w:r><w:t xml:space=\"preserve\">{}</w:t></w:r></w:p>",
        xml_escape(text)
    )
}

fn xml_escape(value: &str) -> String {
    value
        .replace('&', "&amp;")
        .replace('<', "&lt;")
        .replace('>', "&gt;")
}

fn write_docx(path: &Path, body: &str) -> std::io::Result<()> {
    let document_xml = format!(
        r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:body>
    {body}
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="850" w:right="900" w:bottom="850" w:left="900" w:header="720" w:footer="720" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>"#
    );

    let mut docx = ZipWriter::new(File::create(path)?);
    let options = SimpleFileOptions::default().compression_method(CompressionMethod::Deflated);
    for (name, contents) in [
        ("[Content_Types].xml", CONTENT_TYPES),
        ("_rels/.rels", ROOT_RELS),
        ("word/_rels/document.xml.rels", DOCUMENT_RELS),
        ("word/document.xml", document_xml.as_str()),
        ("word/styles.xml", STYLES),
    ] {
        docx.start_file(name, options)?;
        docx.write_all(contents.as_bytes())?;
    }
    docx.finish()?;
    Ok(())
}

const CONTENT_TYPES: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
</Types>"#;

const ROOT_RELS: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>"#;

const DOCUMENT_RELS: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>"#;

const STYLES: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:styleId="Normal"><w:name w:val="Normal"/><w:rPr><w:rFonts w:ascii="Libertinus Serif" w:hAnsi="Libertinus Serif"/><w:sz w:val="20"/></w:rPr><w:pPr><w:spacing w:after="90"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Body"><w:name w:val="Body"/><w:basedOn w:val="Normal"/><w:pPr><w:spacing w:after="100"/><w:jc w:val="both"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:rPr><w:rFonts w:ascii="Libertinus Serif" w:hAnsi="Libertinus Serif"/><w:b/><w:sz w:val="46"/></w:rPr><w:pPr><w:jc w:val="center"/><w:spacing w:after="20"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Subtitle"><w:name w:val="Subtitle"/><w:rPr><w:color w:val="56657A"/><w:sz w:val="19"/></w:rPr><w:pPr><w:jc w:val="center"/><w:spacing w:after="20"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Contact"><w:name w:val="Contact"/><w:rPr><w:color w:val="56657A"/><w:sz w:val="17"/></w:rPr><w:pPr><w:jc w:val="center"/><w:spacing w:after="180"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading"><w:name w:val="Heading"/><w:rPr><w:b/><w:color w:val="23395B"/><w:sz w:val="22"/></w:rPr><w:pPr><w:spacing w:before="180" w:after="80"/><w:pBdr><w:bottom w:val="single" w:sz="5" w:space="2" w:color="D7DEE8"/></w:pBdr></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="EntryTitle"><w:name w:val="EntryTitle"/><w:rPr><w:b/><w:sz w:val="20"/></w:rPr><w:pPr><w:spacing w:before="70" w:after="10"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Meta"><w:name w:val="Meta"/><w:rPr><w:color w:val="56657A"/><w:sz w:val="18"/></w:rPr><w:pPr><w:spacing w:after="50"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Bullet"><w:name w:val="Bullet"/><w:basedOn w:val="Body"/><w:pPr><w:spacing w:after="55"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="SignOff"><w:name w:val="SignOff"/><w:basedOn w:val="Body"/><w:pPr><w:spacing w:after="900"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="NoGap"><w:name w:val="NoGap"/><w:basedOn w:val="Body"/><w:pPr><w:spacing w:after="0"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Spacer"><w:name w:val="Spacer"/><w:pPr><w:spacing w:after="80"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="ExperienceBreak"><w:name w:val="ExperienceBreak"/><w:pPr><w:spacing w:after="150"/></w:pPr></w:style>
</w:styles>"#;

#[cfg(test)]
mod tests {
    use std::io::Read;

    use super::*;

    fn document(toml: &str) -> Document {
        toml::from_str(toml).expect("valid TOML")
    }

    #[test]
    fn escapes_xml_special_characters() {
        assert_eq!(xml_escape("a & b < c > d"), "a &amp; b &lt; c &gt; d");
    }

    #[test]
    fn empty_paragraphs_are_dropped_unless_they_are_spacers() {
        assert_eq!(paragraph("", "Body"), "");
        assert!(paragraph("", "Spacer").contains("w:val=\"Spacer\""));
        assert!(paragraph("", "ExperienceBreak").contains("w:val=\"ExperienceBreak\""));
    }

    #[test]
    fn cv_body_uses_headings_bullets_and_meta_lines() {
        let body = cv(&document(
            r#"
            name = "Ada Lovelace"
            summary = "Analytical engines."

            [[experience]]
            role = "Lead"
            company = "Analytical Engine Co"
            dates = "1843"
            location = "London"
            highlights = ["Wrote the first algorithm"]
            technologies = ["Punch cards"]

            [[education]]
            name = "BSc"
            institution = "Somewhere"
            dates = "1840"
            "#,
        ));

        assert!(body.contains(">PROFILE<"));
        assert!(body.contains(">EDUCATION &amp; CERTIFICATIONS<"));
        assert!(body.contains("Lead — Analytical Engine Co"));
        assert!(body.contains("1843 · London"));
        assert!(body.contains("Technologies: Punch cards"));
        assert!(body.contains("w:val=\"Bullet\""));
    }

    #[test]
    fn cover_letter_drops_the_header_and_spaces_the_sign_off() {
        let body = cover_letter(&document(
            r#"
            type = "cover-letter"
            name = "Ada Lovelace"
            headline = "Mathematician"
            date = "10 May 2026"
            sign_off = "Yours sincerely,"

            [[contact]]
            label = "Email"
            value = "ada@example.com"
            "#,
        ));

        assert!(!body.contains("w:val=\"Title\""));
        assert!(!body.contains("Mathematician"));
        assert!(!body.contains("ada@example.com"));
        assert!(body.starts_with(&paragraph("10 May 2026", "Body")));
        // Room for a signature between the sign-off and the name.
        assert!(body.contains("w:val=\"SignOff\""));
    }

    #[test]
    fn package_contains_the_expected_parts() {
        let path = std::env::temp_dir().join(format!("cv-generator-{}.docx", std::process::id()));
        render(
            &document("type = \"cover-letter\"\nname = \"Ada Lovelace\""),
            &path,
        )
        .expect("render succeeds");

        let mut archive = zip::ZipArchive::new(File::open(&path).expect("open")).expect("zip");
        let names: Vec<String> = archive.file_names().map(str::to_string).collect();
        for part in [
            "[Content_Types].xml",
            "_rels/.rels",
            "word/_rels/document.xml.rels",
            "word/document.xml",
            "word/styles.xml",
        ] {
            assert!(names.iter().any(|name| name == part), "missing {part}");
        }

        let mut xml = String::new();
        archive
            .by_name("word/document.xml")
            .expect("document part")
            .read_to_string(&mut xml)
            .expect("readable");
        assert!(xml.contains("Ada Lovelace"));
        assert!(xml.contains("<w:sectPr>"));

        fs::remove_file(&path).expect("cleanup");
    }
}
