# CV Generator

Generates a CV from a Markdown file into PDF, DOCX, or TXT format.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Or [Docker](https://docs.docker.com/get-docker/)

## Setup

Place your Markdown CV file in the `in/` directory (e.g. `in/john-doe.md`).

### Running it?

```sh
$env:File = "john-doe"
$env:Format = "docx"
make cv @args
```

## Arguments

| Argument   | Description                                    | Required |
| ---------- | ---------------------------------------------- | -------- |
| `filename` | Name of the file in `in/` without `.md`        | Yes      |
| `format`   | Output format: `pdf`, `doc`, or `text`         | No       |

Output defaults to `pdf` if format is omitted. Generated files are written to the `out/` directory.
