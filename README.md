# CV Generator

A lightweight C# generator for CVs and cover letters.

Inputs are TOML files in `input/`. Outputs are PDF or DOCX files in `output/`.

## Prerequisites

- .NET SDK 10.0
- [just](https://github.com/casey/just) for the task recipes (optional — every
  recipe is a one-line `dotnet` command)

Two NuGet packages and nothing else:

| Package | Role |
| --- | --- |
| [QuestPDF](https://www.questpdf.com/) | PDF layout |
| [Tomlyn](https://github.com/xoofx/Tomlyn) | TOML parsing, via its source generator |

No external tools are required to run the generator. DOCX is written directly as
WordprocessingML and PDF is laid out in process; no temporary files are written.
See [Fonts](#fonts) for the one caveat on PDF output.

## Generate documents

Run `just` from the repository root, naming the input file:

```sh
just gen john-doe
just gen john-doe docx
just gen john-doe ats
just gen john-doe-cover
```

There is no separate recipe for cover letters — the document type comes from the
`type` field in the input file. Run `just` on its own to list the recipes.

`format` defaults to `pdf`. Supported formats are `pdf`, `docx`, `doc` as an
alias for `docx`, and `ats`.

The recipe is a thin wrapper around the CLI:

```sh
dotnet run --project CvGenerator -- john-doe pdf
```

You can also publish once and call the binary directly:

```sh
just publish            # win-x64 by default
just publish linux-x64

./artifacts/cv-generator <input-name> <format> [-i input] [-o output]
```

That produces one file, about 28 MB, with no .NET runtime required on the target
machine. Copy it anywhere and run it.

Getting to a genuinely single file took two things beyond `PublishSingleFile`.
Native libraries are excluded by default, so QuestPDF's Skia and qpdf DLLs sat
beside the binary until `IncludeNativeLibrariesForSelfExtract` folded them in —
they now unpack to a temporary directory on first run. And QuestPDF's build
targets copy its bundled Lato font, 18 files and 12 MB, which content files
cannot be bundled at all; `Directory.Build.props` removes them. The renderer
names its font family explicitly on every page, so nothing resolves to Lato, and
Libertinus covers a broader range than Lato does. CI counts the published files
rather than trusting the claim, since a future package could quietly reintroduce
one.

Trimming reports no warnings, and CI drives the published binary over both
document types to prove the linker did not remove anything the renderers reach
for at run time. A Native AOT publish (`-p:PublishAot=true`) should therefore
also work; it needs a platform linker — on Windows, the Desktop Development with
C++ workload — and has not been verified here.

## Development

```sh
just check   # dotnet format --verify-no-changes, build, and test
just clean   # dotnet clean, and remove any published binary
```

Everything else is plain `dotnet`: `dotnet build`, `dotnet test`, `dotnet format`.

`CvGenerator.Tests/PdfPreview.cs` rasterises the sample inputs to PNGs for
eyeballing a layout change. It is skipped unless `CVGEN_PREVIEW_DIR` is set:

```sh
CVGEN_PREVIEW_DIR=/tmp/preview dotnet test --filter PdfPreview
```

CI runs the same checks on every push and pull request, across Linux and
Windows, then publishes and smoke-tests a binary for each.

Line endings are CRLF, since development happens on Windows. The exceptions are
files a POSIX shell executes — the justfile, `*.sh`, `*.py`, and the workflow
YAML — which stay LF because a stray `\r` there is a syntax error. `.gitattributes`
and `.editorconfig` both encode this and need to be kept in step.

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

Every field is optional, so a partially filled profile still renders — missing
values simply produce no output. Unknown keys are ignored.

See [input/john-doe.toml](input/john-doe.toml) and [input/john-doe-cover.toml](input/john-doe-cover.toml).

## Design Notes

The PDF template is intentionally typographic rather than icon-led: a serif face, compact spacing, restrained colour, ruled section headings, and skill chips.

Icons such as Phosphor can look fresh in portfolios, but they add little to a CV and can make the layout feel busier or less ATS-friendly. The PDF template keeps visual polish in the typography and structure instead. The styled DOCX does use a small set of monochrome glyphs on its contact and date lines, which is why the `ats` format exists: a single column, no table and no images, so parsers extract cleanly.

PDF creation timestamps are recorded in UTC rather than local time, so the same input stamps the same way wherever it is generated.

### Fonts

PDF output is only machine-independent if the fonts travel with the binary, so
`CvGenerator/Fonts/` ships Libertinus Serif — regular, bold, italic and bold
italic. They are embedded at build time, registered at startup, and subset into
every PDF written, so one input lays out identically anywhere.

Any other TrueType file dropped into that directory is picked up the same way,
no code change needed. Empty the directory and the renderer falls back through
the family chain in `Pdf/PdfFonts.cs` to whatever serif the host provides, and
line breaks start varying between machines — which is why the test suite asserts
on the font name recorded inside a generated PDF rather than on the files being
present.

The DOCX output declares Calibri, with Carlito — metric-compatible — as the
substitute for machines without it, typically LibreOffice on Linux.

### Licensing

This project is MIT licensed.

QuestPDF is dual-licensed: its Community licence is free below a revenue
threshold, so check the current terms before shipping this commercially.

Libertinus is under the SIL Open Font License, reproduced at
[CvGenerator/Fonts/OFL.txt](CvGenerator/Fonts/OFL.txt) and embedded in the binary
so the licence travels with the fonts.

## Layout of the Source

| Path | Role |
| --- | --- |
| `CvGenerator/Program.cs` | Entry point |
| `CvGenerator/Cli.cs` | Argument parsing and output naming |
| `CvGenerator/Model/Document.cs` | The profile types and shared text helpers |
| `CvGenerator/Model/TomlLoader.cs` | TOML binding and the serializer context |
| `CvGenerator/Pdf/` | QuestPDF renderer, theme, and font registration |
| `CvGenerator/Docx/` | WordprocessingML writer and its fixed package parts |
| `CvGenerator/Fonts/` | Libertinus Serif, embedded at build time |
| `CvGenerator.Tests/` | xUnit test suite |
| `assets/icons/` | The DOCX glyphs; regenerate with `generate.py` |
| `.github/workflows/ci.yml` | Build, test, publish, and smoke-test on Linux and Windows |

Nothing reflects over the model at run time: TOML binding goes through Tomlyn's
source generator, which is what keeps the trimmed publish clean.

This started as a Rust project that linked in [Typst](https://typst.app/) as a
PDF compiler, and was ported to C# in full. The DOCX writer is a direct
translation of the original; the PDF template was rebuilt on QuestPDF.
