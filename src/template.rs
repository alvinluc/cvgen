//! Generation of the Typst markup for a CV or cover letter.

use crate::model::{Document, clean_lines, join_items, join_parts};

/// Gap below each paragraph of a cover letter body, so the sections read as
/// separate blocks rather than one running column of text.
const BODY_GAP: &str = "1.05em";

/// Room between the sign-off and the name, left for a signature.
const SIGN_OFF_GAP: &str = "2.6em";

/// Gap between the lines of the recipient address block.
const RECIPIENT_GAP: &str = "0.42em";

/// Gap below the date, separating it from the recipient address block.
const DATE_GAP: &str = "1.3em";

pub fn cv(document: &Document) -> String {
    let mut lines = vec![preamble(), header(document)];

    section(&mut lines, "Profile");
    paragraph(&mut lines, &document.summary);

    section(&mut lines, "Experience");
    let mut has_previous_experience_block = false;
    for role in &document.experience {
        if has_previous_experience_block {
            experience_break(&mut lines);
        }
        entry(
            &mut lines,
            &role.role,
            &role.company,
            &role.dates,
            &role.location,
            false,
        );
        paragraph(&mut lines, &role.summary);
        bullets(&mut lines, &role.highlights);
        let tech = join_items(&role.technologies);
        if !tech.is_empty() {
            lines.push(format!(
                "#block(above: 0.15em, below: 0.35em)[#label-text(\"Tools\", \"{}\")]",
                t(&tech)
            ));
        }
        has_previous_experience_block = true;
        for earlier in &role.progression {
            experience_break(&mut lines);
            entry(
                &mut lines,
                &earlier.role,
                &role.company,
                &earlier.dates,
                &earlier.location,
                true,
            );
            paragraph(&mut lines, &earlier.summary);
            bullets(&mut lines, &earlier.highlights);
        }
    }

    section(&mut lines, "Skills");
    for group in &document.skills {
        skill_group(&mut lines, &group.name, &group.items);
    }

    section(&mut lines, "Education & Certifications");
    for item in &document.education {
        entry(
            &mut lines,
            &item.name,
            &item.institution,
            &item.dates,
            "",
            true,
        );
        paragraph(&mut lines, &item.detail);
    }

    for group in &document.additional {
        section(&mut lines, group.name.as_deref().unwrap_or("Additional"));
        bullets(&mut lines, &group.items);
    }

    lines.join("\n")
}

/// A letter carries no header block — no name, headline or contact line — so
/// the correspondence itself is the whole document.
pub fn cover_letter(document: &Document) -> String {
    let mut lines = vec![preamble()];
    spaced_paragraph(&mut lines, &document.date, DATE_GAP);
    for recipient in clean_lines(&document.recipient) {
        lines.push(format!(
            "#block(below: {RECIPIENT_GAP})[#text(size: 9.6pt, \"{}\")]",
            t(recipient)
        ));
    }
    if !document.subject.is_empty() {
        lines.push(format!(
            "#block(above: 0.9em, below: 0.8em)[#text(weight: 700, fill: accent, \"{}\")]",
            t(&document.subject)
        ));
    }
    spaced_paragraph(&mut lines, &document.salutation, BODY_GAP);
    for text in clean_lines(&document.body) {
        spaced_paragraph(&mut lines, text, BODY_GAP);
    }
    spaced_paragraph(&mut lines, &document.sign_off, SIGN_OFF_GAP);
    paragraph(&mut lines, &document.name);
    lines.join("\n")
}

fn preamble() -> String {
    [
        "#let accent = rgb(\"#165a72\")".to_string(),
        "#let ink = rgb(\"#17212b\")".to_string(),
        "#let muted = rgb(\"#5c6875\")".to_string(),
        "#let soft = rgb(\"#eef5f7\")".to_string(),
        "#let rule = rgb(\"#d8e4e8\")".to_string(),
        "#let chip(label) = box(inset: (x: 0.46em, y: 0.16em), radius: 0.75em, fill: soft, stroke: rule + 0.35pt)[#text(size: 8.2pt, fill: accent, label)]".to_string(),
        "#let label-text(label, value) = text(size: 8.5pt, fill: muted)[#text(weight: 700, fill: accent, label + \": \") + value]".to_string(),
        "#set page(paper: \"a4\", margin: (x: 1.32cm, y: 1.24cm))".to_string(),
        "#set text(font: \"Libertinus Serif\", size: 9.55pt, fill: ink, lang: \"en\")".to_string(),
        "#set par(justify: true, leading: 0.5em)".to_string(),
        "#show heading.where(level: 1): it => block(above: 0.78em, below: 0.42em)[#grid(columns: (auto, 1fr), gutter: 0.7em, align: horizon)[#text(size: 9.2pt, weight: 700, fill: accent, upper(it.body))][#line(length: 100%, stroke: rule + 0.55pt)]]".to_string(),
        "#show list: set block(spacing: 0.28em)".to_string(),
    ]
    .join("\n")
}

/// The centred name, headline and contact block that opens a CV.
fn header(document: &Document) -> String {
    let mut pieces = vec![format!(
        "#align(center)[#text(size: 24pt, weight: 700, fill: ink, \"{}\")]",
        t(&document.name)
    )];
    if !document.headline.is_empty() {
        pieces.push(format!(
            "#align(center)[#text(size: 9.4pt, fill: accent, \"{}\")]",
            t(&document.headline)
        ));
    }
    let contact = document.contact_line();
    if !contact.is_empty() {
        pieces.push(format!(
            "#align(center)[#text(size: 8pt, fill: muted, \"{}\")]",
            t(&contact)
        ));
    }
    pieces.push("#align(center)[#line(length: 38%, stroke: accent + 0.7pt)]".to_string());
    pieces.push("#v(0.55em)".to_string());
    pieces.join("\n")
}

fn section(lines: &mut Vec<String>, title: &str) {
    if !title.is_empty() {
        lines.push(format!("\n= {title}"));
    }
}

fn paragraph(lines: &mut Vec<String>, value: &str) {
    spaced_paragraph(lines, value, "0.48em");
}

fn spaced_paragraph(lines: &mut Vec<String>, value: &str, gap: &str) {
    let value = value.trim();
    if !value.is_empty() {
        lines.push(format!("#block(below: {gap})[#text(\"{}\")]", t(value)));
    }
}

fn experience_break(lines: &mut Vec<String>) {
    lines.push("#v(0.62em)".to_string());
}

fn entry(
    lines: &mut Vec<String>,
    role: &str,
    company: &str,
    dates: &str,
    location: &str,
    compact: bool,
) {
    let when = join_parts(&[dates, location], " · ");
    let left = if company.is_empty() {
        role.to_string()
    } else {
        format!("{role} — {company}")
    };
    let gap = if compact { "0.18em" } else { "0.34em" };
    lines.push(format!("#v({gap})"));
    lines.push(format!(
        "#grid(columns: (1fr, auto), gutter: 1em)[#text(weight: 700, fill: ink, \"{}\")][#text(size: 8.2pt, fill: muted, \"{}\")]",
        t(&left),
        t(&when)
    ));
}

fn skill_group(lines: &mut Vec<String>, key: &str, values: &[String]) {
    let items = clean_lines(values);
    if key.is_empty() || items.is_empty() {
        return;
    }
    let chips = items
        .iter()
        .map(|item| format!("#chip(\"{}\")", t(item)))
        .collect::<Vec<_>>()
        .join(" ");
    lines.push(format!(
        "#block(below: 0.5em)[#text(weight: 700, fill: accent, \"{}\") #h(0.5em) {chips}]",
        t(key)
    ));
}

fn bullets(lines: &mut Vec<String>, values: &[String]) {
    let items = clean_lines(values);
    if items.is_empty() {
        return;
    }
    lines.push("#list(".to_string());
    for item in items {
        lines.push(format!("  [#text(\"{}\")],", t(item)));
    }
    lines.push(")".to_string());
}

/// Escape a value for use inside a Typst string literal.
fn t(value: &str) -> String {
    let mut escaped = String::with_capacity(value.len());
    for character in value.chars() {
        match character {
            '\\' => escaped.push_str("\\\\"),
            '"' => escaped.push_str("\\\""),
            '\r' => {}
            '\n' => escaped.push(' '),
            _ => escaped.push(character),
        }
    }
    escaped
}

#[cfg(test)]
mod tests {
    use super::*;

    fn document(toml: &str) -> Document {
        toml::from_str(toml).expect("valid TOML")
    }

    #[test]
    fn escapes_string_literals() {
        assert_eq!(t(r#"a "quote" and \ back"#), r#"a \"quote\" and \\ back"#);
        assert_eq!(t("line\r\nbreak"), "line break");
    }

    #[test]
    fn cv_renders_sections_and_progression_entries() {
        let source = cv(&document(
            r#"
            name = "Ada Lovelace"
            summary = "Analytical engines."

            [[experience]]
            role = "Lead"
            company = "Analytical Engine Co"
            dates = "1843"
            location = "London"
            highlights = ["Wrote the first algorithm"]
            technologies = ["Punch cards", "Notes"]

            [[experience.progression]]
            role = "Collaborator"
            dates = "1842"

            [[skills]]
            name = "Mathematics"
            items = ["Analysis"]
            "#,
        ));

        assert!(source.contains("\n= Profile"));
        assert!(source.contains("\"Lead — Analytical Engine Co\""));
        assert!(source.contains("\"1843 · London\""));
        // Progression entries inherit the parent company and render compact.
        assert!(source.contains("\"Collaborator — Analytical Engine Co\""));
        assert!(source.contains("#v(0.18em)"));
        assert!(source.contains("#label-text(\"Tools\", \"Punch cards, Notes\")"));
        assert!(source.contains("#chip(\"Analysis\")"));
    }

    #[test]
    fn cover_letter_renders_recipient_and_subject() {
        let source = cover_letter(&document(
            r#"
            type = "cover-letter"
            name = "Ada Lovelace"
            headline = "Mathematician"
            subject = "Application"
            recipient = ["Hiring Manager", "  "]
            body = ["First paragraph."]
            sign_off = "Yours sincerely,"

            [[contact]]
            label = "Email"
            value = "ada@example.com"
            "#,
        ));

        assert_eq!(
            source
                .matches(&format!("#block(below: {RECIPIENT_GAP})"))
                .count(),
            1
        );
        assert!(source.contains("fill: accent, \"Application\""));
        assert!(source.contains(&format!(
            "#block(below: {BODY_GAP})[#text(\"First paragraph.\")]"
        )));
        // Room for a signature between the sign-off and the name.
        assert!(source.contains(&format!(
            "#block(below: {SIGN_OFF_GAP})[#text(\"Yours sincerely,\")]"
        )));
        // Letters carry no header: no title, headline, contact line or rule.
        assert!(!source.contains("#align(center)"));
        assert!(!source.contains("Mathematician"));
        assert!(!source.contains("ada@example.com"));
    }

    #[test]
    fn cv_keeps_the_header_block() {
        let source = cv(&document(
            "name = \"Ada Lovelace\"\nheadline = \"Mathematician\"",
        ));
        assert!(source.contains("size: 24pt, weight: 700, fill: ink, \"Ada Lovelace\""));
        assert!(source.contains("\"Mathematician\""));
    }

    #[test]
    fn empty_values_render_nothing() {
        let mut lines = Vec::new();
        paragraph(&mut lines, "   ");
        section(&mut lines, "");
        bullets(&mut lines, &[" ".to_string()]);
        skill_group(&mut lines, "Skills", &[]);
        assert!(lines.is_empty());
    }
}
