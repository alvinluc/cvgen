# CV Generator

Generates a CV from a Markdown file into PDF, DOCX, or TXT format.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Or [Docker](https://docs.docker.com/get-docker/)

## Setup

Place your Markdown source file in the `in/` directory (e.g. `in/john-doe.md` for a CV, `in/john-doe-cover-letter.md` for a covering letter).

### Generate a CV

```sh
$env:File = "john-doe"
$env:Format = "docx"
make cv
```

### Generate a covering letter

```sh
$env:File = "john-doe-cover-letter"
$env:Format = "pdf"
make cover-letter
```

## Makefile targets

| Target         | Description                                             |
| -------------- | ------------------------------------------------------- |
| `cv`           | Render the input file as a CV                           |
| `cover-letter` | Render the input file as a covering letter              |

Both targets read `File` (filename in `in/` without `.md`) and `Format` (`pdf`, `doc`, or `text`) from the environment.

## Arguments (direct CLI)

| Argument             | Description                                  | Required |
| -------------------- | -------------------------------------------- | -------- |
| `filename`           | Name of the file in `in/` without `.md`      | Yes      |
| `format`             | Output format: `pdf`, `doc`, or `text`       | No       |
| `-c, --cover-letter` | Render the input file as a covering letter   | No       |

Output defaults to `pdf` if format is omitted. Generated files are written to the `out/` directory.

### Covering letter format

A covering letter uses the same Markdown-with-YAML approach as a CV. The YAML front matter accepts `name`, `left-column`, `right-column`, `date`, `recipient`, `subject`, `salutation`, and `sign-off`; the body is plain paragraphs.

See `in/example-cover-letter.md` for a complete example.
