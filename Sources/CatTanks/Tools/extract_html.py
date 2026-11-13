#!/usr/bin/env python3
"""Re-extract the supplied offline HTML bundle without executing JavaScript.

Usage: python3 Tools/extract_html.py /path/to/CHOI_NGAY.html
Then run: node Tools/export_catalog.mjs (Node.js 18+).
"""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import re


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("html", type=Path)
    args = parser.parse_args()
    root = Path(__file__).resolve().parent.parent
    raw = args.html.read_bytes()
    match = re.search(r'<script\s+id="meow-resources"\s+type="application/json">([\s\S]*?)</script>', raw.decode("utf-8"))
    if not match:
        raise SystemExit("Missing meow-resources JSON bundle")
    resources = json.loads(match.group(1))
    images = 0
    for name, (mime, data) in resources.items():
        relative = Path(name.lstrip("/"))
        if ".." in relative.parts:
            raise ValueError("Unsafe path: " + name)
        content = base64.b64decode(data, validate=True)
        if name.startswith("/assets/") and name.endswith(".png"):
            destination = root / "Assets/CatTanks/Resources/Art" / Path(*relative.parts[1:])
            images += 1
        elif name.startswith("/src/") or name.endswith(".css") or name.endswith(".json"):
            destination = root / "Documentation/OriginalWeb" / relative
            if name.endswith(".js"):
                prefix = os.path.relpath(root / "Documentation/OriginalWeb/src", destination.parent).replace(os.sep, "/")
                content = content.decode("utf-8").replace("meow:/src/", prefix + "/").encode("utf-8")
        else:
            continue
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(content)
    (root / "Documentation/OriginalWeb/package.json").write_text('{"type":"module"}\n')
    print(json.dumps({"source_sha256": hashlib.sha256(raw).hexdigest(), "resources": len(resources), "png_assets": images}, indent=2))


if __name__ == "__main__":
    main()
