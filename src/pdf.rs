use std::fs;
use std::path::Path;
use std::sync::LazyLock;

use time::OffsetDateTime;
use typst::World;
use typst::diag::{FileError, FileResult, SourceDiagnostic, Warned};
use typst::foundations::{Bytes, Datetime, Duration};
use typst::syntax::{FileId, Source};
use typst::text::{Font, FontBook};
use typst::utils::LazyHash;
use typst::{Library, LibraryExt};
use typst_layout::PagedDocument;
use typst_pdf::{PdfOptions, Timestamp};

use crate::model::Document;
use crate::template;

/// Compile a document to PDF in-process. Typst is linked in, so no external
/// tools or system fonts are needed.
pub fn render(document: &Document, output_path: &Path) -> Result<(), String> {
    let source = if document.is_cover_letter() {
        template::cover_letter(document)
    } else {
        template::cv(document)
    };

    let world = TypstWorld::new(source);
    let Warned { output, warnings } = typst::compile::<PagedDocument>(&world);
    for warning in &warnings {
        eprintln!("warning: {}", warning.message);
    }
    let compiled = output.map_err(|errors| describe("could not compile the document", &errors))?;

    let options = PdfOptions {
        timestamp: world.now().map(Timestamp::new_utc),
        ..PdfOptions::default()
    };
    let bytes = typst_pdf::pdf(&compiled, &options)
        .map_err(|errors| describe("could not export the PDF", &errors))?;

    if let Some(parent) = output_path.parent() {
        fs::create_dir_all(parent).map_err(|error| format!("{}: {error}", parent.display()))?;
    }
    fs::write(output_path, bytes).map_err(|error| format!("{}: {error}", output_path.display()))
}

/// The bundled Typst fonts, parsed once. Libertinus Serif — the template font —
/// is among them, so output does not depend on what the host has installed.
static FONTS: LazyLock<Vec<Font>> = LazyLock::new(|| {
    typst_assets::fonts()
        .flat_map(|data| Font::iter(Bytes::new(data)))
        .collect()
});

/// A single-file, in-memory compilation environment.
struct TypstWorld {
    library: LazyHash<Library>,
    book: LazyHash<FontBook>,
    source: Source,
}

impl TypstWorld {
    fn new(text: String) -> Self {
        Self {
            library: LazyHash::new(Library::default()),
            book: LazyHash::new(FontBook::from_fonts(FONTS.iter())),
            source: Source::detached(text),
        }
    }

    /// The PDF creation timestamp, in UTC. Local time is deliberately not used:
    /// reading the system timezone is unsound once worker threads are running.
    fn now(&self) -> Option<Datetime> {
        let now = OffsetDateTime::now_utc();
        Datetime::from_ymd_hms(
            now.year(),
            now.month() as u8,
            now.day(),
            now.hour(),
            now.minute(),
            now.second(),
        )
    }
}

impl World for TypstWorld {
    fn library(&self) -> &LazyHash<Library> {
        &self.library
    }

    fn book(&self) -> &LazyHash<FontBook> {
        &self.book
    }

    fn main(&self) -> FileId {
        self.source.id()
    }

    fn source(&self, id: FileId) -> FileResult<Source> {
        if id == self.source.id() {
            Ok(self.source.clone())
        } else {
            Err(not_found(id))
        }
    }

    fn file(&self, id: FileId) -> FileResult<Bytes> {
        if id == self.source.id() {
            Ok(Bytes::from_string(self.source.text().to_string()))
        } else {
            Err(not_found(id))
        }
    }

    fn font(&self, index: usize) -> Option<Font> {
        FONTS.get(index).cloned()
    }

    fn today(&self, offset: Option<Duration>) -> Option<Datetime> {
        let now = match offset {
            None => OffsetDateTime::now_utc(),
            Some(offset) => OffsetDateTime::now_utc() + time::Duration::from(offset),
        };
        Datetime::from_ymd(now.year(), now.month() as u8, now.day())
    }
}

fn not_found(id: FileId) -> FileError {
    FileError::NotFound(id.vpath().get_without_slash().into())
}

/// Flatten Typst diagnostics into a single error message.
fn describe(context: &str, errors: &[SourceDiagnostic]) -> String {
    let mut message = context.to_string();
    for error in errors {
        message.push_str("\n  ");
        message.push_str(&error.message);
        for hint in &error.hints {
            message.push_str("\n  hint: ");
            message.push_str(&hint.v);
        }
    }
    message
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn bundled_fonts_cover_the_template_font() {
        let book = FontBook::from_fonts(FONTS.iter());
        assert!(book.contains_family("libertinus serif"));
    }

    #[test]
    fn renders_a_pdf_without_external_tools() {
        let document: Document = toml::from_str(
            r#"
            name = "Ada Lovelace"
            summary = "Analytical engines."

            [[skills]]
            name = "Mathematics"
            items = ["Analysis"]
            "#,
        )
        .expect("valid TOML");

        let path = std::env::temp_dir().join(format!("cv-generator-{}.pdf", std::process::id()));
        render(&document, &path).expect("render succeeds");

        let bytes = fs::read(&path).expect("readable");
        assert!(bytes.starts_with(b"%PDF-"));
        assert!(bytes.len() > 1000);

        fs::remove_file(&path).expect("cleanup");
    }
}
