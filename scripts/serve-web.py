"""Serve the Unity web build locally with the same gzip/MIME headers as Vercel."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import sys


class Handler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        return super().guess_type(path.removesuffix('.gz'))

    def end_headers(self):
        if self.path.split('?', 1)[0].endswith('.gz'):
            self.send_header('Content-Encoding', 'gzip')
        self.send_header('X-Content-Type-Options', 'nosniff')
        super().end_headers()


if __name__ == '__main__':
    root = Path(__file__).resolve().parent.parent / 'builds/web'
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8765
    print(f'Toenland: http://localhost:{port}', flush=True)
    ThreadingHTTPServer(('127.0.0.1', port), partial(Handler, directory=str(root))).serve_forever()
