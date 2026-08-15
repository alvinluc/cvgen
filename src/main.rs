mod docx;
mod model;
mod pdf;
mod template;

use std::path::PathBuf;
use std::process::ExitCode;

use clap::{Parser, ValueEnum};

use crate::model::load_toml;

/// Generate a CV or cover letter from a TOML profile.
#[derive(Parser)]
#[command(name = "cv-generator", version, about)]
struct Cli {
    /// Input file name without .toml
    name: String,

    /// Output format
    #[arg(value_enum, default_value_t = Format::Pdf)]
    format: Format,

    #[arg(short, long, default_value = "input")]
    input_dir: PathBuf,

    #[arg(short, long, default_value = "output")]
    output_dir: PathBuf,
}

#[derive(Clone, Copy, PartialEq, ValueEnum)]
enum Format {
    Doc,
    Docx,
    Pdf,
    /// Single-column, icon-free DOCX tuned for ATS parsers
    Ats,
}

impl Format {
    /// `doc` is an alias for `docx`; the ATS variant gets its own suffix so
    /// it can live next to the styled DOCX.
    fn file_name(self, name: &str) -> String {
        match self {
            Format::Pdf => format!("{name}.pdf"),
            Format::Doc | Format::Docx => format!("{name}.docx"),
            Format::Ats => format!("{name}-ats.docx"),
        }
    }
}

fn main() -> ExitCode {
    match run(Cli::parse()) {
        Ok(()) => ExitCode::SUCCESS,
        Err(error) => {
            eprintln!("error: {error}");
            ExitCode::FAILURE
        }
    }
}

fn run(cli: Cli) -> Result<(), String> {
    let input_path = cli.input_dir.join(format!("{}.toml", cli.name));
    let output_path = cli.output_dir.join(cli.format.file_name(&cli.name));

    let document = load_toml(&input_path)?;

    match cli.format {
        Format::Pdf => pdf::render(&document, &output_path)?,
        Format::Ats => docx::render_ats(&document, &output_path)?,
        Format::Doc | Format::Docx => docx::render(&document, &output_path)?,
    }

    println!("Generated {}", output_path.display());
    Ok(())
}
