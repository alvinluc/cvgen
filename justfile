# Generate CVs and cover letters from TOML inputs.
#
# The document type (cv or cover letter) comes from the input file itself,
# so one recipe covers both.
#
# Only commands worth shortening live here. Everything else is plain dotnet:
# `dotnet build`, `dotnet test`, `dotnet format`.

# just shells out to `sh` by default, which Windows has no reason to provide.
# PowerShell ships with the OS, so use it there and leave sh in place elsewhere.
set windows-shell := ["powershell.exe", "-NoLogo", "-NoProfile", "-Command"]

# Show the available recipes
default:
    @just --list --unsorted

# Generate input/<name>.toml into output/ (format: pdf, docx, doc, ats)
gen name format="pdf":
    @dotnet run --project CvGenerator -c Release -v q -- {{ name }} {{ format }}

# Run the format check, build, and tests
check:
    @dotnet format --verify-no-changes
    @dotnet build -c Release -v q
    @dotnet test -c Release -v q --nologo

# Publish a self-contained, trimmed single-file binary into ./artifacts (~14 MB)
publish rid="win-x64":
    @dotnet publish CvGenerator -c Release -r {{ rid }} --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=full -o artifacts

# Remove build artefacts, including the published binary
[unix]
clean:
    @dotnet clean -v q
    @rm -rf artifacts

# Remove build artefacts, including the published binary
[windows]
clean:
    @dotnet clean -v q
    @if (Test-Path artifacts) { Remove-Item -Recurse -Force artifacts }
