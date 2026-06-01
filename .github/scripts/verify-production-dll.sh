#!/usr/bin/env bash
# Verify a Release/production Dread.dll matches CD/Thunderstore expectations.
# Usage: verify-production-dll.sh <path-to-Dread.dll>
set -euo pipefail

dll="${1:?Usage: verify-production-dll.sh <path-to-Dread.dll>}"

if [ ! -f "$dll" ]; then
  echo "::error::Dread.dll not found: $dll"
  exit 1
fi

agent_debug_types=(DebugServerSystem DebugOverlaySystem TestCrashSystem)

for name in "${agent_debug_types[@]}"; do
  if strings "$dll" | grep -q "$name"; then
    echo "::error::Development-only type $name found in production Dread.dll"
    exit 1
  fi
done

echo "Production DLL verified: no development-only system types"
