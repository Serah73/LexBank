#!/usr/bin/env bash
# Drops the development database. The API recreates it (with the seeded rules) the next time it starts.

cd "$(dirname "$0")" || exit 1

dotnet ef database drop --force --project src/backend/LexBank.Infrastructure --startup-project src/backend/LexBank.Api
