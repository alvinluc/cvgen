# Generate CVs and cover letters from TOML inputs.
#
# The document type (cv or cover letter) comes from the input file itself,
# so one recipe covers both.

# Show the available recipes
default:
    @just --list --unsorted

# Generate input/<name>.toml into output/ (format: pdf, docx, doc)
gen name format="pdf":
    @cargo run --release --quiet -- {{ name }} {{ format }}

# Build the release binary
build:
    @cargo build --release

# Run fmt check, clippy, and tests
check:
    @cargo fmt --check
    @cargo clippy --all-targets -- -D warnings
    @cargo test

# Format the source
fmt:
    @cargo fmt

# Remove build artefacts
clean:
    @cargo clean
