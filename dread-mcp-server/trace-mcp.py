#!/usr/bin/env python3
"""Log stdin/stdout between Cursor and dread-mcp-server for debugging."""
import subprocess
import sys
import threading
from pathlib import Path

DIR = Path(__file__).resolve().parent
LOG = Path("/tmp/dread-mcp-trace.log")
NODE_SCRIPT = DIR / "dist" / "index.js"


def main() -> None:
    LOG.write_bytes(b"=== trace start ===\n")
    proc = subprocess.Popen(
        ["node", str(NODE_SCRIPT)],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        env=None,
    )

    def pump_stderr() -> None:
        assert proc.stderr is not None
        for chunk in iter(lambda: proc.stderr.read(4096), b""):
            LOG.write_bytes(b"ERR:" + chunk)
            LOG.flush()

    def pump_stdout() -> None:
        assert proc.stdout is not None
        while True:
            chunk = proc.stdout.read(4096)
            if not chunk:
                break
            LOG.write_bytes(b"OUT:" + chunk)
            LOG.flush()
            sys.stdout.buffer.write(chunk)
            sys.stdout.buffer.flush()

    threading.Thread(target=pump_stderr, daemon=True).start()
    threading.Thread(target=pump_stdout, daemon=True).start()

    assert proc.stdin is not None
    try:
        while True:
            chunk = sys.stdin.buffer.read(4096)
            if not chunk:
                break
            LOG.write_bytes(b"IN:" + chunk)
            LOG.flush()
            proc.stdin.write(chunk)
            proc.stdin.flush()
    finally:
        proc.stdin.close()
        proc.wait(timeout=5)
        LOG.write_bytes(f"=== exit {proc.returncode} ===\n".encode())


if __name__ == "__main__":
    main()
