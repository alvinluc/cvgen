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
}

impl Format {
    /// `doc` is an alias for `docx`.
    fn extension(self) -> &'static str {
        match self {
            Format::Pdf => "pdf",
            Format::Doc | Format::Docx => "docx",
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
    let output_path = cli
        .output_dir
        .join(format!("{}.{}", cli.name, cli.format.extension()));

    let document = load_toml(&input_path)?;

    if cli.format == Format::Pdf {
        pdf::render(&document, &output_path)?;
    } else {
        docx::render(&document, &output_path)?;
    }

    println!("Generated {}", output_path.display());
    Ok(())
}
