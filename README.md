# CV Generator

A lightweight Rust generator for CVs and cover letters.

Inputs are TOML files in `input/`. Outputs are PDF or DOCX files in `output/`.

## Prerequisites

- Rust 1.97.1+ (2024 edition)
- [just](https://github.com/casey/just) for the task recipes (optional — every
  recipe is a one-line `cargo` command)

Nothing else. The binary is self-contained: [Typst](https://typst.app/) is linked
in as a library for PDF output and DOCX is written directly, so no external CLI
and no installed fonts are required. The Typst fonts — including Libertinus
Serif, which the template uses — are embedded in the binary, so output is
identical on any machine. The trade-off is build time and a release binary of
roughly 50 MB.

## Generate Documents

Run `just` from the repository root, naming the input file:

```sh
just gen john-doe
just gen john-doe docx
just gen john-doe-cover-letter
```

There is no separate recipe for cover letters — the document type comes from the
`type` field in the input file. Run `just` on its own to list the recipes.

`format` defaults to `pdf`. Supported formats are `pdf`, `docx`, and `doc` as an alias for `docx`.

The recipe is a thin wrapper around the Rust CLI. It runs:

```sh
cargo run --release -- john-doe pdf
cargo run --release -- john-doe-cover-letter docx
```

You can also build once with `just build` and call the binary directly:

```sh
./target/release/cv-generator <input-name> <format> [-i input] [-o output]
```

## Development

```sh
just check   # cargo fmt --check, clippy, and tests
just fmt     # cargo fmt
just clean   # cargo clean
```

## Input Format

The generator uses TOML rather than free-form Markdown so the layout code can stay small and predictable.

CV files use:

- `type = "cv"`
- top-level `name`, `headline`, and `summary`
- repeated `[[contact]]`
- repeated `[[experience]]`, with optional `[[experience.progression]]`
- repeated `[[skills]]`
- repeated `[[education]]`
- repeated `[[additional]]`

Cover letters use:

- `type = "cover-letter"`
- top-level `date`, `recipient`, `subject`, `salutation`, `body`, `sign_off`, and `name`

Letters render no header block, so `headline` and `[[contact]]` are ignored; `name`
appears only under the sign-off.

See [input/john-doe.toml](input/john-doe.toml) and [input/john-doe-cover-letter.toml](input/john-doe-cover-letter.toml).

## Design Notes

The PDF template is intentionally typographic rather than icon-led: Libertinus Serif, compact spacing, restrained colour, ruled section headings, and skill chips.

Icons such as Phosphor can look fresh in portfolios, but they add little to a CV and can make the layout feel busier or less ATS-friendly. The current template keeps visual polish in the typography and structure instead.

PDF generation builds Typst markup in memory (`src/template.rs`) and compiles it through the linked-in Typst compiler (`src/pdf.rs`); no temporary files are written. PDF creation timestamps are recorded in UTC rather than local time, because reading the system timezone is unsound once the compiler's worker threads are running.
