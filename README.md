# CV Generator

A lightweight Python generator for CVs and cover letters.

Inputs are TOML files in `input/`. Outputs are PDF or DOCX files in `output/`.

## Prerequisites

- Python 3.12+
- [uv](https://docs.astral.sh/uv/)
- [Task](https://taskfile.dev/installation/)
- [Typst](https://typst.app/) for PDF output

DOCX output uses only Python's standard library.

## Generate Documents

Install Task first, then run the project tasks from the repository root:

```sh
task cv file=john-doe format=pdf
task cv file=john-doe format=docx
task cover-letter file=john-doe-cover-letter format=pdf
```

`format` defaults to `pdf`. Supported formats are `pdf`, `docx`, and `doc` as an alias for `docx`.

The Taskfile commands are thin wrappers around the Python CLI. They run:

```sh
uv run cv-generator john-doe pdf
uv run cv-generator john-doe-cover-letter docx
```

You can call `uv run cv-generator <input-name> <format>` directly if you do not want to use Task.

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
- top-level `name`, `headline`, `date`, `recipient`, `subject`, `salutation`, `body`, and `sign_off`
- repeated `[[contact]]`

See [input/john-doe.toml](input/john-doe.toml) and [input/john-doe-cover-letter.toml](input/john-doe-cover-letter.toml).

## Design Notes

The PDF template is intentionally typographic rather than icon-led: Noto Serif, compact spacing, restrained colour, ruled section headings, and skill chips.

Icons such as Phosphor can look fresh in portfolios, but they add little to a CV and can make the layout feel busier or less ATS-friendly. The current template keeps visual polish in the typography and structure instead.
