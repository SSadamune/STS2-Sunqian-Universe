#!/usr/bin/env python3
"""Strip trailing NULs from Godot-exported .import stubs inside a PCK.

Godot's exporter writes those stubs with a trailing NUL, which makes
ResourceFormatImporter fail (EOF / UTF-8 spam) when shop hovers load png paths.
"""

from __future__ import annotations

import hashlib
import struct
import sys
from pathlib import Path

PACK_MAGIC = b"GDPC"
PACK_FORMAT_V3 = 3
PACK_REL_FILEBASE = 0x2
PCK_PADDING = 16
HEADER_SIZE = 104


def _pad(alignment: int, n: int) -> int:
    rest = n % alignment
    return 0 if rest == 0 else alignment - rest


def read_pck(path: Path) -> tuple[int, int, int, int, int, list[tuple[str, int, bytes]]]:
    blob = path.read_bytes()
    if blob[:4] != PACK_MAGIC:
        raise ValueError(f"{path} is not a Godot PCK")

    ver, maj, minor, patch, flags = struct.unpack_from("<IIIII", blob, 4)
    if ver != PACK_FORMAT_V3:
        raise ValueError(f"unsupported pack version {ver}")

    file_base, dir_offset = struct.unpack_from("<QQ", blob, 24)
    off = dir_offset
    file_count = struct.unpack_from("<I", blob, off)[0]
    off += 4

    files: list[tuple[str, int, bytes]] = []
    for _ in range(file_count):
        plen = struct.unpack_from("<I", blob, off)[0]
        off += 4
        raw_name = blob[off : off + plen]
        name = raw_name.split(b"\x00", 1)[0].decode("utf-8")
        off += plen
        ofs, size = struct.unpack_from("<QQ", blob, off)
        off += 16
        off += 16  # md5
        flags_f = struct.unpack_from("<I", blob, off)[0]
        off += 4
        if flags_f:
            raise ValueError(f"encrypted/removal PCK entry not supported: {name}")
        files.append((name, flags_f, blob[file_base + ofs : file_base + ofs + size]))

    return ver, maj, minor, patch, flags, files


def strip_trailing_nuls(data: bytes) -> bytes:
    return data.rstrip(b"\x00")


def write_pck(
    path: Path,
    maj: int,
    minor: int,
    patch: int,
    files: list[tuple[str, bytes]],
) -> None:
    files = sorted(files, key=lambda item: item[0])
    file_base = HEADER_SIZE + _pad(PCK_PADDING, HEADER_SIZE)

    payload = bytearray()
    payload.extend(PACK_MAGIC)
    payload.extend(struct.pack("<IIIII", PACK_FORMAT_V3, maj, minor, patch, PACK_REL_FILEBASE))
    file_base_ofs = len(payload)
    payload.extend(struct.pack("<Q", 0))
    dir_ofs_ofs = len(payload)
    payload.extend(struct.pack("<Q", 0))
    payload.extend(b"\x00" * 64)
    payload.extend(b"\x00" * _pad(PCK_PADDING, len(payload)))
    assert len(payload) == file_base
    struct.pack_into("<Q", payload, file_base_ofs, file_base)

    directory: list[tuple[str, int, int, bytes]] = []
    for name, data in files:
        ofs = len(payload) - file_base
        payload.extend(data)
        payload.extend(b"\x00" * _pad(PCK_PADDING, len(payload)))
        directory.append((name, ofs, len(data), hashlib.md5(data).digest()))

    payload.extend(b"\x00" * _pad(PCK_PADDING, len(payload)))
    dir_offset = len(payload)
    struct.pack_into("<Q", payload, dir_ofs_ofs, dir_offset)

    payload.extend(struct.pack("<I", len(directory)))
    for name, ofs, size, md5 in directory:
        encoded = name.encode("utf-8")
        pad = _pad(4, len(encoded))
        payload.extend(struct.pack("<I", len(encoded) + pad))
        payload.extend(encoded)
        payload.extend(b"\x00" * pad)
        payload.extend(struct.pack("<QQ", ofs, size))
        payload.extend(md5)
        payload.extend(struct.pack("<I", 0))

    path.write_bytes(payload)


def fix_pck(pck_path: Path) -> int:
    ver, maj, minor, patch, flags, entries = read_pck(pck_path)
    if flags & ~PACK_REL_FILEBASE:
        raise ValueError(f"unsupported pack flags {flags:#x}")

    by_name: dict[str, bytes] = {}
    stripped = 0
    for name, _flags, data in entries:
        if name.endswith(".import"):
            cleaned = strip_trailing_nuls(data)
            if cleaned != data:
                stripped += 1
            by_name[name] = cleaned
        else:
            by_name[name] = data

    write_pck(pck_path, maj, minor, patch, list(by_name.items()))
    return stripped


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: strip_pck_import_nuls.py <pack.pck>", file=sys.stderr)
        return 2
    pck_path = Path(sys.argv[1])
    stripped = fix_pck(pck_path)
    print(f"Wrote {pck_path.name}: stripped NULs from {stripped} .import files")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
