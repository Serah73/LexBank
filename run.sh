#!/usr/bin/env bash
# Starts the API and the frontend in this terminal. Ctrl+C stops both.

cd "$(dirname "$0")" || exit 1

trap 'kill 0' EXIT

dotnet run --project src/backend/LexBank.Api --launch-profile http &
npm --prefix src/frontend run dev
