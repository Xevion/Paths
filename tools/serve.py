#!/usr/bin/env python3
"""Serve the WebGL build locally with the same headers the host will send.

The build is gzipped with no JS fallback, so a plain static server (python -m http.server)
hands the loader compressed bytes and it fails to start. This mirrors the rules in the
template's _headers file so local and deployed behave the same.
"""

import http.server
import sys

TYPES = {
    ".wasm.gz": "application/wasm",
    ".framework.js.gz": "application/javascript",
    ".data.gz": "application/octet-stream",
}


class Handler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        for suffix, mime in TYPES.items():
            if path.endswith(suffix):
                return mime
        return super().guess_type(path)

    def end_headers(self):
        if self.path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        super().end_headers()


if __name__ == "__main__":
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8000
    http.server.test(HandlerClass=Handler, port=port, bind="127.0.0.1")
