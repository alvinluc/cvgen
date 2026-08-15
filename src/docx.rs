use std::fs::{self, File};
use std::io::Write;
use std::path::Path;
use std::sync::atomic::{AtomicU32, Ordering};

use zip::write::SimpleFileOptions;
use zip::{CompressionMethod, ZipWriter};

use crate::model::{Document, clean_lines, join_items, join_parts};

// The monochrome palette of the double-column layout. Each colour is also
// mapped to a theme slot (text1/text2/accent1/accent2) so Word's
// Design > Colors menu can restyle the document.
const BLACK: &str = "1A1A1A";
const INK: &str = "3D3D3D";
const TITLE: &str = "2B2B2B";
const MUTED: &str = "7A7A7A";
const RULE: &str = "BFBFBF";

// A4 (11906 twips) minus the 720-twip side margins, split into a main
// column and a sidebar; the gutter lives in the main cell's right margin.
const CONTENT_WIDTH: u32 = 10466;
const MAIN_WIDTH: u32 = 6800;
const SIDE_WIDTH: u32 = 3666;
const GUTTER: u32 = 420;

// Inline icon sizes in EMUs (914400 per inch): 9.5pt beside contact text,
// 9pt beside the smaller date/location lines.
const ICON_CONTACT: u32 = 120650;
const ICON_META: u32 = 114300;

/// The monochrome glyphs embedded as image parts; regenerate with
/// assets/icons/generate.py.
const ICONS: [(&str, &[u8]); 5] = [
    ("phone", include_bytes!("../assets/icons/phone.png")),
    ("email", include_bytes!("../assets/icons/email.png")),
    ("pin", include_bytes!("../assets/icons/pin.png")),
    ("link", include_bytes!("../assets/icons/link.png")),
    ("calendar", include_bytes!("../assets/icons/calendar.png")),
];

/// DrawingML object ids must be unique within a document; a process-wide
/// counter is the simplest way to guarantee that.
static DRAWING_ID: AtomicU32 = AtomicU32::new(1);

/// Render a document to a minimal, dependency-free WordprocessingML package.
pub fn render(document: &Document, output_path: &Path) -> Result<(), String> {
    write_package(document, output_path, false)
}

/// Render the ATS variant: a single column with no table, no icons and
/// labelled contact details, maximising extraction fidelity in parsers.
pub fn render_ats(document: &Document, output_path: &Path) -> Result<(), String> {
    write_package(document, output_path, true)
}

fn write_package(document: &Document, output_path: &Path, ats: bool) -> Result<(), String> {
    let body = if document.is_cover_letter() {
        cover_letter(document)
    } else if ats {
        cv_ats(document)
    } else {
        cv(document)
    };

    if let Some(parent) = output_path.parent() {
        fs::create_dir_all(parent).map_err(|error| format!("{}: {error}", parent.display()))?;
    }
    write_docx(output_path, &body, document)
        .map_err(|error| format!("{}: {error}", output_path.display()))
}

/// A full-width header, then a two-column table: profile, experience and
/// education in the main column; skills and additional groups in the sidebar.
fn cv(document: &Document) -> String {
    let mut body = vec![
        paragraph(&document.name.to_uppercase(), "Name"),
        paragraph(&document.headline, "Headline"),
        contact_paragraph(document),
    ];

    let main = main_column(document);
    let side = side_column(document);
    if side.is_empty() {
        body.push(main);
    } else {
        body.push(two_columns(&main, &side));
        // Word expects a paragraph after a table that closes the body.
        body.push(empty_paragraph("Normal"));
    }
    body.concat()
}

fn main_column(document: &Document) -> String {
    let mut column = Vec::new();

    if !document.summary.is_empty() {
        column.push(heading("Summary"));
        column.push(paragraph(&document.summary, "Body"));
    }

    if !document.experience.is_empty() {
        column.push(heading("Experience"));
        let mut has_previous_experience_block = false;
        for role in &document.experience {
            if has_previous_experience_block {
                column.push(empty_paragraph("DotBreak"));
            }
            column.push(entry_block(
                &role.role,
                &role.company,
                &role.dates,
                &role.location,
            ));
            column.push(paragraph(&role.summary, "Body"));
            column.extend(clean_lines(&role.highlights).into_iter().map(bullet));
            let tech = join_items(&role.technologies);
            if !tech.is_empty() {
                column.push(labelled("Meta", "Tools", &tech));
            }
            has_previous_experience_block = true;
            for earlier in &role.progression {
                column.push(empty_paragraph("DotBreak"));
                column.push(entry_block(
                    &earlier.role,
                    &role.company,
                    &earlier.dates,
                    &earlier.location,
                ));
                column.push(paragraph(&earlier.summary, "Body"));
                column.extend(clean_lines(&earlier.highlights).into_iter().map(bullet));
                let tech = join_items(&earlier.technologies);
                if !tech.is_empty() {
                    column.push(labelled("Meta", "Tools", &tech));
                }
            }
        }
    }

    if !document.education.is_empty() {
        column.push(heading("Education & Certifications"));
        for (index, item) in document.education.iter().enumerate() {
            if index > 0 {
                column.push(empty_paragraph("DotBreak"));
            }
            column.push(entry_block(&item.name, &item.institution, &item.dates, ""));
            column.push(paragraph(&item.detail, "Body"));
        }
    }

    column.concat()
}

fn side_column(document: &Document) -> String {
    let mut column = Vec::new();

    let mut has_skills = false;
    for group in &document.skills {
        let items = clean_lines(&group.items);
        if group.name.is_empty() || items.is_empty() {
            continue;
        }
        if !has_skills {
            column.push(heading("Skills"));
            has_skills = true;
        }
        column.push(paragraph(&group.name.to_uppercase(), "SkillGroup"));
        column.extend(items.into_iter().map(|item| paragraph(item, "Skill")));
    }

    for group in &document.additional {
        let items = clean_lines(&group.items);
        if items.is_empty() {
            continue;
        }
        column.push(heading(group.name.as_deref().unwrap_or("Additional")));
        for (index, item) in items.into_iter().enumerate() {
            if index > 0 {
                column.push(empty_paragraph("DotBreak"));
            }
            column.push(paragraph(item, "Body"));
        }
    }

    column.concat()
}

/// The ATS variant: one column, plain-text everything. Contact details keep
/// their labels, date/location lines are plain text, sections run Summary →
/// Experience → Skills → Education so parsers see keywords in role context
/// before credentials.
fn cv_ats(document: &Document) -> String {
    let mut body = vec![
        paragraph(&document.name.to_uppercase(), "Name"),
        paragraph(&document.headline, "Headline"),
        paragraph(&document.contact_line(), "ContactLine"),
    ];

    if !document.summary.is_empty() {
        body.push(heading("Summary"));
        body.push(paragraph(&document.summary, "Body"));
    }

    if !document.experience.is_empty() {
        body.push(heading("Experience"));
        for role in &document.experience {
            body.push(entry_block_ats(
                &role.role,
                &role.company,
                &role.dates,
                &role.location,
            ));
            body.push(paragraph(&role.summary, "Body"));
            body.extend(clean_lines(&role.highlights).into_iter().map(bullet));
            let tech = join_items(&role.technologies);
            if !tech.is_empty() {
                body.push(labelled("Meta", "Tools", &tech));
            }
            for earlier in &role.progression {
                body.push(entry_block_ats(
                    &earlier.role,
                    &role.company,
                    &earlier.dates,
                    &earlier.location,
                ));
                body.push(paragraph(&earlier.summary, "Body"));
                body.extend(clean_lines(&earlier.highlights).into_iter().map(bullet));
                let tech = join_items(&earlier.technologies);
                if !tech.is_empty() {
                    body.push(labelled("Meta", "Tools", &tech));
                }
            }
        }
    }

    if !document.skills.is_empty() {
        body.push(heading("Skills"));
        for group in &document.skills {
            let items = join_items(&group.items);
            if !group.name.is_empty() && !items.is_empty() {
                body.push(labelled("Body", &group.name, &items));
            }
        }
    }

    if !document.education.is_empty() {
        body.push(heading("Education & Certifications"));
        for item in &document.education {
            body.push(entry_block_ats(
                &item.name,
                &item.institution,
                &item.dates,
                "",
            ));
            body.push(paragraph(&item.detail, "Body"));
        }
    }

    for group in &document.additional {
        let items = clean_lines(&group.items);
        if items.is_empty() {
            continue;
        }
        body.push(heading(group.name.as_deref().unwrap_or("Additional")));
        body.extend(items.into_iter().map(|item| paragraph(item, "Body")));
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
    body.push(paragraph(&document.subject, "Subject"));
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

/// The contact row: an icon and value per entry where the label maps to a
/// known icon, "Label: value" text otherwise.
fn contact_paragraph(document: &Document) -> String {
    let mut runs = String::new();
    for item in &document.contact {
        let value = item.value.trim();
        if value.is_empty() {
            continue;
        }
        if !runs.is_empty() {
            runs.push_str(&run("\u{2003}", ""));
        }
        match contact_icon(&item.label, &item.url) {
            Some(icon) => {
                runs.push_str(&icon_run(icon, ICON_CONTACT));
                runs.push_str(&run(" ", ""));
                runs.push_str(&run(value, ""));
            }
            None => {
                let label = item.label.trim();
                let text = if label.is_empty() {
                    value.to_string()
                } else {
                    format!("{label}: {value}")
                };
                runs.push_str(&run(&text, ""));
            }
        }
    }
    if runs.is_empty() {
        return String::new();
    }
    runs_paragraph("ContactLine", &runs)
}

fn contact_icon(label: &str, url: &str) -> Option<&'static str> {
    let label = label.to_lowercase();
    let matches = |keys: &[&str]| keys.iter().any(|key| label.contains(key));
    if matches(&["mobile", "phone", "tel"]) {
        Some("phone")
    } else if matches(&["mail"]) {
        Some("email")
    } else if matches(&["location", "address", "city"]) {
        Some("pin")
    } else if !url.trim().is_empty()
        || matches(&["linkedin", "github", "web", "site", "portfolio", "url"])
    {
        Some("link")
    } else {
        None
    }
}

/// The role, company and date/location lines that open an entry.
fn entry_block(title: &str, company: &str, dates: &str, location: &str) -> String {
    let mut block = paragraph(title, "EntryTitle");
    block.push_str(&paragraph(company, "Company"));
    block.push_str(&meta_paragraph(dates, location));
    block
}

/// The icon-free entry opener used by the ATS variant.
fn entry_block_ats(title: &str, company: &str, dates: &str, location: &str) -> String {
    let mut block = paragraph(title, "EntryTitle");
    block.push_str(&paragraph(company, "Company"));
    block.push_str(&paragraph(&join_parts(&[dates, location], ", "), "Meta"));
    block
}

/// A calendar-marked date and a pin-marked location on one muted line.
fn meta_paragraph(dates: &str, location: &str) -> String {
    let dates = dates.trim();
    let location = location.trim();
    if dates.is_empty() && location.is_empty() {
        return String::new();
    }
    let mut runs = String::new();
    if !dates.is_empty() {
        runs.push_str(&icon_run("calendar", ICON_META));
        runs.push_str(&run(" ", ""));
        runs.push_str(&run(dates, ""));
    }
    if !location.is_empty() {
        if !runs.is_empty() {
            runs.push_str(&run("\u{2003}", ""));
        }
        runs.push_str(&icon_run("pin", ICON_META));
        runs.push_str(&run(" ", ""));
        runs.push_str(&run(location, ""));
    }
    runs_paragraph("Meta", &runs)
}

fn two_columns(main: &str, side: &str) -> String {
    format!(
        "<w:tbl><w:tblPr><w:tblW w:w=\"{CONTENT_WIDTH}\" w:type=\"dxa\"/><w:tblLayout w:type=\"fixed\"/><w:tblCellMar><w:left w:w=\"0\" w:type=\"dxa\"/><w:right w:w=\"0\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr><w:tblGrid><w:gridCol w:w=\"{MAIN_WIDTH}\"/><w:gridCol w:w=\"{SIDE_WIDTH}\"/></w:tblGrid><w:tr><w:tc><w:tcPr><w:tcW w:w=\"{MAIN_WIDTH}\" w:type=\"dxa\"/><w:tcMar><w:right w:w=\"{GUTTER}\" w:type=\"dxa\"/></w:tcMar></w:tcPr>{main}</w:tc><w:tc><w:tcPr><w:tcW w:w=\"{SIDE_WIDTH}\" w:type=\"dxa\"/></w:tcPr>{side}</w:tc></w:tr></w:tbl>"
    )
}

fn paragraph(text: &str, style: &str) -> String {
    if text.is_empty() {
        return String::new();
    }
    runs_paragraph(style, &run(text, ""))
}

/// A paragraph with no text, used for separators and structural spacing.
fn empty_paragraph(style: &str) -> String {
    runs_paragraph(style, &run("", ""))
}

fn heading(text: &str) -> String {
    paragraph(&text.to_uppercase(), "Heading")
}

/// A bold label followed by a value in the style's own formatting.
fn labelled(style: &str, label: &str, value: &str) -> String {
    let runs = format!(
        "{}{}",
        run(
            &format!("{label}: "),
            &format!("<w:b/><w:color w:val=\"{BLACK}\" w:themeColor=\"text1\"/>")
        ),
        run(value, "")
    );
    runs_paragraph(style, &runs)
}

fn bullet(text: &str) -> String {
    format!(
        "<w:p><w:pPr><w:pStyle w:val=\"Bullet\"/><w:ind w:left=\"360\" w:hanging=\"180\"/></w:pPr>{}{}</w:p>",
        run(
            "• ",
            &format!("<w:color w:val=\"{MUTED}\" w:themeColor=\"accent1\"/>")
        ),
        run(text, "")
    )
}

fn runs_paragraph(style: &str, runs: &str) -> String {
    format!("<w:p><w:pPr><w:pStyle w:val=\"{style}\"/></w:pPr>{runs}</w:p>")
}

fn run(text: &str, properties: &str) -> String {
    let properties = if properties.is_empty() {
        String::new()
    } else {
        format!("<w:rPr>{properties}</w:rPr>")
    };
    format!(
        "<w:r>{properties}<w:t xml:space=\"preserve\">{}</w:t></w:r>",
        xml_escape(text)
    )
}

/// An inline picture run referencing one of the embedded icon parts, sized
/// in EMUs and nudged down slightly to sit optically centred beside text.
fn icon_run(icon: &str, size: u32) -> String {
    let id = DRAWING_ID.fetch_add(1, Ordering::Relaxed);
    format!(
        "<w:r><w:rPr><w:noProof/><w:position w:val=\"-2\"/></w:rPr><w:drawing><wp:inline distT=\"0\" distB=\"0\" distL=\"0\" distR=\"0\"><wp:extent cx=\"{size}\" cy=\"{size}\"/><wp:docPr id=\"{id}\" name=\"{icon}\"/><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/picture\"><pic:pic><pic:nvPicPr><pic:cNvPr id=\"{id}\" name=\"{icon}\"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed=\"rIdIcon-{icon}\"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"{size}\" cy=\"{size}\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r>"
    )
}

fn xml_escape(value: &str) -> String {
    value
        .replace('&', "&amp;")
        .replace('<', "&lt;")
        .replace('>', "&gt;")
}

fn write_docx(path: &Path, body: &str, document: &Document) -> std::io::Result<()> {
    let document_xml = format!(
        r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture">
  <w:body>
    {body}
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="680" w:right="720" w:bottom="680" w:left="720" w:header="708" w:footer="708" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>"#
    );
    let styles_xml = styles();
    let core_xml = core_properties(document);

    let mut docx = ZipWriter::new(File::create(path)?);
    let options = SimpleFileOptions::default().compression_method(CompressionMethod::Deflated);
    for (name, contents) in [
        ("[Content_Types].xml", CONTENT_TYPES),
        ("_rels/.rels", ROOT_RELS),
        ("word/_rels/document.xml.rels", DOCUMENT_RELS),
        ("word/document.xml", document_xml.as_str()),
        ("word/styles.xml", styles_xml.as_str()),
        ("word/settings.xml", SETTINGS),
        ("word/fontTable.xml", FONT_TABLE),
        ("word/theme/theme1.xml", THEME),
        ("docProps/core.xml", core_xml.as_str()),
        ("docProps/app.xml", APP_PROPERTIES),
    ] {
        docx.start_file(name, options)?;
        docx.write_all(contents.as_bytes())?;
    }
    for (icon, bytes) in ICONS {
        docx.start_file(format!("word/media/icon-{icon}.png"), options)?;
        docx.write_all(bytes)?;
    }
    docx.finish()?;
    Ok(())
}

fn core_properties(document: &Document) -> String {
    let kind = if document.is_cover_letter() {
        "Cover Letter"
    } else {
        "CV"
    };
    let title = if document.name.is_empty() {
        kind.to_string()
    } else {
        format!("{} — {kind}", document.name)
    };
    format!(
        r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/">
  <dc:title>{}</dc:title>
  <dc:creator>{}</dc:creator>
</cp:coreProperties>"#,
        xml_escape(&title),
        xml_escape(&document.name)
    )
}

const CONTENT_TYPES: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Default Extension="png" ContentType="image/png"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
  <Override PartName="/word/settings.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.settings+xml"/>
  <Override PartName="/word/fontTable.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.fontTable+xml"/>
  <Override PartName="/word/theme/theme1.xml" ContentType="application/vnd.openxmlformats-officedocument.theme+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>"#;

const ROOT_RELS: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>"#;

const DOCUMENT_RELS: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId0" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/fontTable" Target="fontTable.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/settings" Target="settings.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme" Target="theme/theme1.xml"/>
  <Relationship Id="rIdIcon-phone" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-phone.png"/>
  <Relationship Id="rIdIcon-email" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-email.png"/>
  <Relationship Id="rIdIcon-pin" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-pin.png"/>
  <Relationship Id="rIdIcon-link" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-link.png"/>
  <Relationship Id="rIdIcon-calendar" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/icon-calendar.png"/>
</Relationships>"#;

const SETTINGS: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:settings xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:defaultTabStop w:val="708"/>
  <w:characterSpacingControl w:val="doNotCompress"/>
  <w:clrSchemeMapping w:bg1="light1" w:t1="dark1" w:bg2="light2" w:t2="dark2" w:accent1="accent1" w:accent2="accent2" w:accent3="accent3" w:accent4="accent4" w:accent5="accent5" w:accent6="accent6" w:hyperlink="hyperlink" w:followedHyperlink="followedHyperlink"/>
</w:settings>"#;

/// Declares Carlito — metric-compatible with Calibri — as the substitute for
/// machines without Calibri installed (typically LibreOffice on Linux).
const FONT_TABLE: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:fonts xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:font w:name="Calibri"><w:altName w:val="Carlito"/><w:family w:val="swiss"/><w:pitch w:val="variable"/></w:font>
</w:fonts>"#;

/// A monochrome Office theme. The styles reference these slots, so Word's
/// Design > Colors and Design > Fonts menus restyle the whole document.
const THEME: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="CV Monochrome">
  <a:themeElements>
    <a:clrScheme name="CV Monochrome">
      <a:dk1><a:srgbClr val="1A1A1A"/></a:dk1>
      <a:lt1><a:srgbClr val="FFFFFF"/></a:lt1>
      <a:dk2><a:srgbClr val="3D3D3D"/></a:dk2>
      <a:lt2><a:srgbClr val="F2F2F2"/></a:lt2>
      <a:accent1><a:srgbClr val="7A7A7A"/></a:accent1>
      <a:accent2><a:srgbClr val="BFBFBF"/></a:accent2>
      <a:accent3><a:srgbClr val="595959"/></a:accent3>
      <a:accent4><a:srgbClr val="404040"/></a:accent4>
      <a:accent5><a:srgbClr val="262626"/></a:accent5>
      <a:accent6><a:srgbClr val="A6A6A6"/></a:accent6>
      <a:hlink><a:srgbClr val="2B579A"/></a:hlink>
      <a:folHlink><a:srgbClr val="954F72"/></a:folHlink>
    </a:clrScheme>
    <a:fontScheme name="CV Monochrome">
      <a:majorFont><a:latin typeface="Calibri"/><a:ea typeface=""/><a:cs typeface=""/></a:majorFont>
      <a:minorFont><a:latin typeface="Calibri"/><a:ea typeface=""/><a:cs typeface=""/></a:minorFont>
    </a:fontScheme>
    <a:fmtScheme name="Office">
      <a:fillStyleLst>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
      </a:fillStyleLst>
      <a:lnStyleLst>
        <a:ln w="6350"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
        <a:ln w="12700"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
        <a:ln w="19050"><a:solidFill><a:schemeClr val="phClr"/></a:solidFill></a:ln>
      </a:lnStyleLst>
      <a:effectStyleLst>
        <a:effectStyle><a:effectLst/></a:effectStyle>
        <a:effectStyle><a:effectLst/></a:effectStyle>
        <a:effectStyle><a:effectLst/></a:effectStyle>
      </a:effectStyleLst>
      <a:bgFillStyleLst>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
        <a:solidFill><a:schemeClr val="phClr"/></a:solidFill>
      </a:bgFillStyleLst>
    </a:fmtScheme>
  </a:themeElements>
</a:theme>"#;

const APP_PROPERTIES: &str = r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties">
  <Application>cv-generator</Application>
</Properties>"#;

fn styles() -> String {
    format!(
        r#"<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:docDefaults>
    <w:rPrDefault><w:rPr><w:rFonts w:ascii="Calibri" w:hAnsi="Calibri" w:asciiTheme="minorHAnsi" w:hAnsiTheme="minorHAnsi"/><w:color w:val="{ink}" w:themeColor="text2"/><w:sz w:val="20"/><w:szCs w:val="20"/></w:rPr></w:rPrDefault>
    <w:pPrDefault><w:pPr><w:spacing w:after="80"/></w:pPr></w:pPrDefault>
  </w:docDefaults>
  <w:style w:type="paragraph" w:styleId="Normal" w:default="1"><w:name w:val="Normal"/><w:uiPriority w:val="0"/><w:qFormat/></w:style>
  <w:style w:type="paragraph" w:styleId="Name"><w:name w:val="CV Name"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="1"/><w:qFormat/><w:pPr><w:spacing w:after="40"/></w:pPr><w:rPr><w:b/><w:color w:val="{black}" w:themeColor="text1"/><w:sz w:val="54"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Headline"><w:name w:val="Headline"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="2"/><w:qFormat/><w:pPr><w:spacing w:after="60"/></w:pPr><w:rPr><w:color w:val="{muted}" w:themeColor="accent1"/><w:sz w:val="26"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="ContactLine"><w:name w:val="Contact Line"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="3"/><w:qFormat/><w:pPr><w:spacing w:after="200"/></w:pPr><w:rPr><w:b/><w:sz w:val="20"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading"><w:name w:val="Section Heading"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="4"/><w:qFormat/><w:pPr><w:spacing w:before="200" w:after="120"/><w:pBdr><w:bottom w:val="single" w:sz="18" w:space="4" w:color="{black}" w:themeColor="text1"/></w:pBdr></w:pPr><w:rPr><w:b/><w:color w:val="{black}" w:themeColor="text1"/><w:sz w:val="28"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="EntryTitle"><w:name w:val="Entry Title"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="5"/><w:qFormat/><w:pPr><w:spacing w:before="40" w:after="20"/></w:pPr><w:rPr><w:color w:val="{title}"/><w:sz w:val="26"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Company"><w:name w:val="Company"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="6"/><w:qFormat/><w:pPr><w:spacing w:after="20"/></w:pPr><w:rPr><w:b/><w:color w:val="{muted}" w:themeColor="accent1"/><w:sz w:val="22"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Meta"><w:name w:val="Meta"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="7"/><w:pPr><w:spacing w:after="80"/></w:pPr><w:rPr><w:color w:val="{muted}" w:themeColor="accent1"/><w:sz w:val="20"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Body"><w:name w:val="Body"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="8"/><w:qFormat/><w:pPr><w:spacing w:after="100"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="Bullet"><w:name w:val="Bullet"/><w:basedOn w:val="Body"/><w:uiPriority w:val="9"/><w:pPr><w:spacing w:after="40"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="SkillGroup"><w:name w:val="Skill Group"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="10"/><w:pPr><w:spacing w:before="120" w:after="60"/></w:pPr><w:rPr><w:b/><w:color w:val="{muted}" w:themeColor="accent1"/><w:sz w:val="18"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Skill"><w:name w:val="Skill"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="11"/><w:qFormat/><w:pPr><w:spacing w:after="100"/><w:pBdr><w:bottom w:val="single" w:sz="4" w:space="6" w:color="{rule}" w:themeColor="accent2"/></w:pBdr></w:pPr><w:rPr><w:b/><w:color w:val="{title}"/><w:sz w:val="20"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="DotBreak"><w:name w:val="Dotted Break"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="12"/><w:semiHidden/><w:pPr><w:spacing w:before="40" w:after="120"/><w:pBdr><w:bottom w:val="dotted" w:sz="4" w:space="2" w:color="{rule}" w:themeColor="accent2"/></w:pBdr></w:pPr><w:rPr><w:sz w:val="8"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Subject"><w:name w:val="Subject"/><w:basedOn w:val="Normal"/><w:uiPriority w:val="13"/><w:qFormat/><w:pPr><w:spacing w:before="90" w:after="80"/></w:pPr><w:rPr><w:b/><w:color w:val="{black}" w:themeColor="text1"/><w:sz w:val="22"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="SignOff"><w:name w:val="Sign Off"/><w:basedOn w:val="Body"/><w:uiPriority w:val="14"/><w:pPr><w:spacing w:after="900"/></w:pPr></w:style>
  <w:style w:type="paragraph" w:styleId="NoGap"><w:name w:val="No Gap"/><w:basedOn w:val="Body"/><w:uiPriority w:val="15"/><w:pPr><w:spacing w:after="0"/></w:pPr></w:style>
</w:styles>"#,
        black = BLACK,
        ink = INK,
        title = TITLE,
        muted = MUTED,
        rule = RULE,
    )
}

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
    fn empty_paragraphs_are_dropped_unless_explicitly_structural() {
        assert_eq!(paragraph("", "Body"), "");
        assert!(empty_paragraph("DotBreak").contains("w:val=\"DotBreak\""));
    }

    #[test]
    fn contact_labels_map_to_icons() {
        assert_eq!(contact_icon("Mobile", ""), Some("phone"));
        assert_eq!(contact_icon("Email", ""), Some("email"));
        assert_eq!(contact_icon("Location", ""), Some("pin"));
        assert_eq!(contact_icon("LinkedIn", ""), Some("link"));
        assert_eq!(contact_icon("Blog", "https://example.com"), Some("link"));
        assert_eq!(contact_icon("Pronouns", ""), None);
    }

    #[test]
    fn entry_blocks_stack_title_company_and_iconed_dates() {
        let block = entry_block("Lead", "Analytical Engine Co", "1843", "London");
        assert!(block.contains("w:val=\"EntryTitle\""));
        assert!(block.contains("w:val=\"Company\""));
        assert!(block.contains("w:val=\"Meta\""));
        assert!(block.contains("rIdIcon-calendar"));
        assert!(block.contains("rIdIcon-pin"));
        // Missing pieces simply produce no line or icon.
        let sparse = entry_block("Lead", "", "", "");
        assert!(sparse.contains("w:val=\"EntryTitle\""));
        assert!(!sparse.contains("w:val=\"Company\""));
        assert!(!sparse.contains("w:val=\"Meta\""));
    }

    #[test]
    fn cv_splits_content_into_two_columns() {
        let body = cv(&document(
            r#"
            name = "Ada Lovelace"
            summary = "Analytical engines."

            [[contact]]
            label = "Email"
            value = "ada@example.com"

            [[experience]]
            role = "Lead"
            company = "Analytical Engine Co"
            dates = "1843"
            location = "London"
            highlights = ["Wrote the first algorithm"]
            technologies = ["Punch cards"]

            [[skills]]
            name = "Mathematics"
            items = ["Analysis"]

            [[education]]
            name = "BSc"
            institution = "Somewhere"
            dates = "1840"

            [[additional]]
            items = ["Fluent in French"]
            "#,
        ));

        assert!(body.contains(">ADA LOVELACE<"));
        assert!(body.contains("<w:tbl>"));
        assert!(body.contains(">SUMMARY<"));
        assert!(body.contains(">EDUCATION &amp; CERTIFICATIONS<"));
        assert!(body.contains(">SKILLS<"));
        assert!(body.contains(">MATHEMATICS<"));
        assert!(body.contains("w:val=\"Skill\""));
        assert!(body.contains(">Tools: <"));
        assert!(body.contains("w:val=\"Bullet\""));
        assert!(body.contains("rIdIcon-email"));
        assert!(body.contains("<w:drawing>"));
        // The sidebar cell comes after the main content in document order.
        let skills = body.find(">SKILLS<").expect("skills heading");
        let education = body.find(">EDUCATION").expect("education heading");
        assert!(education < skills);
    }

    #[test]
    fn ats_variant_is_single_column_with_labelled_plain_text() {
        let body = cv_ats(&document(
            r#"
            name = "Ada Lovelace"
            summary = "Analytical engines."

            [[contact]]
            label = "Email"
            value = "ada@example.com"

            [[experience]]
            role = "Lead"
            company = "Analytical Engine Co"
            dates = "1843"
            location = "London"
            technologies = ["Punch cards"]

            [[experience.progression]]
            role = "Collaborator"
            dates = "1842"
            technologies = ["Notes"]

            [[skills]]
            name = "Mathematics"
            items = ["Analysis"]

            [[education]]
            name = "BSc"
            institution = "Somewhere"
            dates = "1840"
            "#,
        ));

        assert!(!body.contains("<w:tbl>"));
        assert!(!body.contains("<w:drawing>"));
        assert!(body.contains("Email: ada@example.com"));
        assert!(body.contains(">1843, London<"));
        assert!(body.contains(">Notes<"));
        // Skills precede education so keywords sit above credentials.
        let skills = body.find(">SKILLS<").expect("skills heading");
        let education = body.find(">EDUCATION").expect("education heading");
        assert!(skills < education);
    }

    #[test]
    fn cv_without_sidebar_content_stays_single_column() {
        let body = cv(&document(
            "name = \"Ada Lovelace\"\nsummary = \"Analytical engines.\"",
        ));
        assert!(!body.contains("<w:tbl>"));
        assert!(body.contains(">SUMMARY<"));
    }

    #[test]
    fn cover_letter_drops_the_header_and_spaces_the_sign_off() {
        let body = cover_letter(&document(
            r#"
            type = "cover-letter"
            name = "Ada Lovelace"
            headline = "Mathematician"
            date = "10 May 2026"
            subject = "Application"
            sign_off = "Yours sincerely,"

            [[contact]]
            label = "Email"
            value = "ada@example.com"
            "#,
        ));

        assert!(!body.contains("w:val=\"Name\""));
        assert!(!body.contains("Mathematician"));
        assert!(!body.contains("ada@example.com"));
        assert!(body.starts_with(&paragraph("10 May 2026", "Body")));
        assert!(body.contains("w:val=\"Subject\""));
        // Letters carry no icons.
        assert!(!body.contains("<w:drawing>"));
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
            "word/settings.xml",
            "word/fontTable.xml",
            "word/theme/theme1.xml",
            "word/media/icon-phone.png",
            "word/media/icon-calendar.png",
            "docProps/core.xml",
            "docProps/app.xml",
        ] {
            assert!(names.iter().any(|name| name == part), "missing {part}");
        }

        let mut xml = String::new();
        archive
            .by_name("docProps/core.xml")
            .expect("core properties")
            .read_to_string(&mut xml)
            .expect("readable");
        assert!(xml.contains("Ada Lovelace — Cover Letter"));

        // Word only loads parts reachable through explicit relationships, so
        // every part next to document.xml must be declared in its rels.
        let mut rels = String::new();
        archive
            .by_name("word/_rels/document.xml.rels")
            .expect("document rels")
            .read_to_string(&mut rels)
            .expect("readable");
        for target in [
            "styles.xml",
            "settings.xml",
            "fontTable.xml",
            "theme/theme1.xml",
        ] {
            assert!(
                rels.contains(&format!("Target=\"{target}\"")),
                "missing relationship to {target}"
            );
        }

        fs::remove_file(&path).expect("cleanup");
    }
}
