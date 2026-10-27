#!/usr/bin/env python
"""Unity WebGL derlemesini Brotli ile sikistirir ve index.html / sw.js'i gunceller.

Kullanim: python -I tools/postprocess_web.py <derleme_klasoru>
"""
import os
import re
import sys

import brotli

LIMIT = 100 * 1024 * 1024
TARGETS = ("web.data", "web.wasm", "web.framework.js")
URL_RE = re.compile(r"Build/web\.(?:data|wasm|framework\.js)(?!\.br)")


def mb(n):
    return "%.2f MB" % (n / 1048576)


def read(path):
    with open(path, "r", encoding="utf-8", newline="") as f:
        return f.read()


def write(path, text):
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def main():
    if len(sys.argv) != 2 or not os.path.isdir(sys.argv[1]):
        print("Kullanim: postprocess_web.py <derleme_klasoru>")
        return 2
    root = os.path.abspath(sys.argv[1])
    build = os.path.join(root, "Build")
    log = []

    for name in TARGETS:
        src = os.path.join(build, name)
        dst = src + ".br"
        if os.path.isfile(src):
            with open(src, "rb") as f:
                raw = f.read()
            out = brotli.compress(raw, quality=11)
            with open(dst, "wb") as f:
                f.write(out)
            os.remove(src)
            log.append("brotli: %s %s -> %s" % (name, mb(len(raw)), mb(len(out))))
        elif os.path.isfile(dst):
            log.append("zaten sikistirilmis: %s.br" % name)
        else:
            print("HATA: Build/%s bulunamadi" % name)
            return 1

    for fname in ("index.html", "sw.js"):
        path = os.path.join(root, fname)
        if not os.path.isfile(path):
            print("HATA: %s bulunamadi" % fname)
            return 1
        text = read(path)
        new, n = URL_RE.subn(lambda m: m.group(0) + ".br", text)
        if fname == "sw.js":
            new, k = re.subn(r"fetch\(url,\s*\{\s*cache\s*:\s*'reload'\s*\}\)",
                             "fetch(url,{cache:'no-cache'})", new)
            log.append("sw.js: %d yol .br yapildi, %d fetch cache:no-cache" % (n, k))
        else:
            log.append("index.html: %d URL .br yapildi" % n)
        if new != text:
            write(path, new)

    print("Yapilanlar:")
    for line in log:
        print("  " + line)

    too_big = []
    print("Dosya boyutlari:")
    for d, _, files in os.walk(root):
        for fn in sorted(files):
            p = os.path.join(d, fn)
            size = os.path.getsize(p)
            print("  %10s  %s" % (mb(size), os.path.relpath(p, root).replace(os.sep, "/")))
            if size > LIMIT:
                too_big.append(p)
    if too_big:
        for p in too_big:
            print("HATA: 100 MB'tan buyuk dosya: " + p)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
