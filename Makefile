# Generate CVs and cover letters from TOML inputs.
#
# Usage:
#   make cv file=john-doe
#   make cv file=john-doe format=docx
#   make cover-letter file=john-doe-cover-letter

format ?= pdf

.PHONY: help cv cover-letter

help:
	@echo "Targets:"
	@echo "  cv            Generate a CV from an input TOML file (requires file=...)"
	@echo "  cover-letter  Generate a cover letter from an input TOML file (requires file=...)"
	@echo ""
	@echo "Variables:"
	@echo "  file          Input name (without .toml), required"
	@echo "  format        Output format: pdf (default), docx, doc"

cv:
	@test -n "$(file)" || { echo "error: 'file' is required, e.g. make cv file=john-doe"; exit 1; }
	@uv run cv-generator $(file) $(format)

cover-letter:
	@test -n "$(file)" || { echo "error: 'file' is required, e.g. make cover-letter file=john-doe-cover-letter"; exit 1; }
	@uv run cv-generator $(file) $(format)
