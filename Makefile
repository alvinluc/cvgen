SHELL := /bin/bash

format ?= pdf

cv:
	@test -n "$(file)" || (echo "Usage: make cv file=<input-name> [format=pdf|docx]" && exit 1)
	uv run cv-generator $(file) $(format)

cover-letter:
	@test -n "$(file)" || (echo "Usage: make cover-letter file=<input-name> [format=pdf|docx]" && exit 1)
	uv run cv-generator $(file) $(format)
