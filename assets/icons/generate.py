#!/usr/bin/env python3
"""Regenerate the monochrome icon PNGs embedded in the DOCX output.

Pure stdlib: shapes are signed-distance functions rasterised with 2x2
supersampling into 64x64 RGBA PNGs. Run from this directory:

    python3 generate.py
"""

import math
import struct
import zlib

SIZE = 64
COLOR = (0x70, 0x70, 0x70)


def circle(px, py, cx, cy, r):
    return math.hypot(px - cx, py - cy) - r


def rbox(px, py, cx, cy, hx, hy, r):
    qx = abs(px - cx) - hx + r
    qy = abs(py - cy) - hy + r
    return min(max(qx, qy), 0.0) + math.hypot(max(qx, 0.0), max(qy, 0.0)) - r


def capsule(px, py, ax, ay, bx, by, r):
    vx, vy = bx - ax, by - ay
    wx, wy = px - ax, py - ay
    t = max(0.0, min(1.0, (wx * vx + wy * vy) / (vx * vx + vy * vy)))
    return math.hypot(wx - t * vx, wy - t * vy) - r


def convex(px, py, points):
    """SDF of a convex polygon given clockwise points (y grows downward)."""
    d = -1e9
    n = len(points)
    for i in range(n):
        ax, ay = points[i]
        bx, by = points[(i + 1) % n]
        ex, ey = bx - ax, by - ay
        length = math.hypot(ex, ey)
        nx, ny = ey / length, -ex / length
        d = max(d, (px - ax) * nx + (py - ay) * ny)
    return d


def union(*ds):
    return min(ds)


def cut(d, hole):
    return max(d, -hole)


def phone(px, py):
    frame = cut(
        rbox(px, py, 32, 32, 10, 24, 4),
        rbox(px, py, 32, 30, 7, 17.5, 2),
    )
    dot = circle(px, py, 32, 51.5, 2.2)
    return union(frame, dot)


def email(px, py):
    outline = cut(
        rbox(px, py, 32, 32, 23, 17, 3),
        rbox(px, py, 32, 32, 20, 14, 1.5),
    )
    left = capsule(px, py, 11, 17, 32, 35, 2.2)
    right = capsule(px, py, 53, 17, 32, 35, 2.2)
    return union(outline, left, right)


def pin(px, py):
    head = circle(px, py, 32, 25, 15)
    tail = convex(px, py, [(18.5, 32), (45.5, 32), (32, 57)])
    return cut(union(head, tail), circle(px, py, 32, 25, 6.5))


def link(px, py):
    ring = cut(circle(px, py, 32, 32, 20), circle(px, py, 32, 32, 16.5))
    equator = capsule(px, py, 13.5, 32, 50.5, 32, 1.8)
    sx = (px - 32) * 2.4 + 32
    meridian = cut(circle(sx, py, 32, 32, 20), circle(sx, py, 32, 32, 16.5))
    return union(ring, equator, meridian)


def calendar(px, py):
    outline = cut(
        rbox(px, py, 32, 34.5, 22, 20.5, 3),
        rbox(px, py, 32, 34.5, 19, 17.5, 1.5),
    )
    header = rbox(px, py, 32, 20.5, 22, 6.5, 3)
    peg_left = capsule(px, py, 20, 9, 20, 17, 2.4)
    peg_right = capsule(px, py, 44, 9, 44, 17, 2.4)
    return union(outline, header, peg_left, peg_right)


def coverage(sdf, x, y):
    total = 0.0
    for ox, oy in ((0.25, 0.25), (0.75, 0.25), (0.25, 0.75), (0.75, 0.75)):
        d = sdf(x + ox, y + oy)
        total += max(0.0, min(1.0, 0.5 - d))
    return total / 4.0


def write_png(path, sdf):
    raw = bytearray()
    for y in range(SIZE):
        raw.append(0)  # filter: none
        for x in range(SIZE):
            alpha = round(coverage(sdf, x, y) * 255)
            raw.extend((*COLOR, alpha))

    def chunk(tag, data):
        payload = tag + data
        return struct.pack(">I", len(data)) + payload + struct.pack(">I", zlib.crc32(payload))

    ihdr = struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0)
    png = (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", ihdr)
        + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
        + chunk(b"IEND", b"")
    )
    with open(path, "wb") as handle:
        handle.write(png)
    print(f"wrote {path}")


if __name__ == "__main__":
    for name, sdf in (
        ("phone", phone),
        ("email", email),
        ("pin", pin),
        ("link", link),
        ("calendar", calendar),
    ):
        write_png(f"{name}.png", sdf)
