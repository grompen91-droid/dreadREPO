#!/usr/bin/env bash
set -euo pipefail
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ ! -f "$DIR/dist/index.js" ]]; then
  echo "dread-mcp-server: missing dist/index.js. Run: cd dread-mcp-server && npm install && npm run build" >&2
  exit 1
fi

exec node "$DIR/dist/index.js"
