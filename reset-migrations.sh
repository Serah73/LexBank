#!/usr/bin/env bash
# Deletes the generated migrations so InitialCreate can be regenerated (development only, see src/backend/CLAUDE.md).

cd "$(dirname "$0")" || exit 1

rm -r src/backend/LexBank.Infrastructure/Migrations
