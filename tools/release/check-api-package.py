"""Read-only API ZIP compatibility checker; no extraction or cleanup."""
import hashlib, io, pathlib, stat, unicodedata, zipfile

MAX_ARCHIVE = 256 * 1024 * 1024
MAX_EXPANDED = 2 * 1024 * 1024 * 1024

def validate_archive(blob, expected=None):
    if len(blob) > MAX_ARCHIVE:
        raise ValueError("Archive exceeds reviewed inspection bound")
    result, seen, total = {}, set(), 0
    with zipfile.ZipFile(io.BytesIO(blob)) as archive:
        for item in archive.infolist():
            name = item.filename
            if item.orig_filename != name or any(ord(c) < 32 or ord(c) == 127 for c in name) or unicodedata.normalize("NFC", name) != name:
                raise ValueError("Ambiguous archive member name")
            extra = item.extra
            position = 0
            while position < len(extra):
                if len(extra) - position < 4:
                    raise ValueError("Truncated archive extra-field header")
                kind_id = int.from_bytes(extra[position:position + 2], "little")
                length = int.from_bytes(extra[position + 2:position + 4], "little")
                position += 4
                if length > len(extra) - position or kind_id == 0x7075:
                    raise ValueError("Truncated or alternate-name archive extra field")
                position += length
            parts = name.rstrip("/").split("/")
            if not name or "\\" in name or name.startswith("/") or any(x in ("", ".", "..") or ":" in x for x in parts):
                raise ValueError("Unsafe archive path")
            canonical = "/".join(parts)
            if canonical in seen:
                raise ValueError("Duplicate archive member")
            seen.add(canonical)
            mode = item.external_attr >> 16
            kind = stat.S_IFMT(mode)
            if kind not in (0, stat.S_IFREG, stat.S_IFDIR) or (kind == stat.S_IFDIR) != item.is_dir() and kind != 0:
                raise ValueError("Unsupported archive entry")
            if canonical == "release.zip":
                raise ValueError("Root release.zip requires exact disposition; preserved")
            total += item.file_size
            if total > MAX_EXPANDED or item.flag_bits & 1:
                raise ValueError("Archive exceeds bounds or is encrypted")
            if item.is_dir():
                continue
            digest = hashlib.sha256()
            with archive.open(item) as stream:
                for chunk in iter(lambda: stream.read(1024 * 1024), b""):
                    digest.update(chunk)
            result[canonical] = {"sha256": digest.hexdigest(), "executable": mode & 0o111}
    for name in seen:
        parts = name.split("/")
        if any("/".join(parts[:i]) in result for i in range(1, len(parts))):
            raise ValueError("Regular file is an ancestor of another member")
    if "Gccs.Api.dll" not in result:
        raise ValueError("API assembly missing at root")
    if expected is not None and result != expected:
        raise ValueError("Archive and publish-tree contents/modes differ")
    return result

def inspect_archive_path(archive_path, expected_sha256):
    archive_path = pathlib.Path(archive_path)
    if archive_path.is_symlink() or not archive_path.is_file() or archive_path.stat().st_size > MAX_ARCHIVE:
        raise ValueError("Unreviewed archive source or size")
    before = archive_path.read_bytes()
    if hashlib.sha256(before).hexdigest() != expected_sha256:
        raise ValueError("Archive identity mismatch")
    result = validate_archive(before)
    if archive_path.read_bytes() != before:
        raise ValueError("Archive changed during inspection")
    return before, result

def inspect_tree(root_path):
    root_path = pathlib.Path(root_path)
    if root_path.is_symlink() or not root_path.is_dir():
        raise ValueError("Unreviewed publish-tree source")
    expected = {}
    for entry in root_path.rglob("*"):
        mode = entry.lstat().st_mode
        relative = entry.relative_to(root_path).as_posix()
        if relative == "release.zip" or stat.S_ISLNK(mode):
            raise ValueError("Root collision or symlink; preserve source")
        if stat.S_ISREG(mode):
            digest = hashlib.sha256()
            with entry.open("rb") as stream:
                for chunk in iter(lambda: stream.read(1024 * 1024), b""):
                    digest.update(chunk)
            expected[relative] = {"sha256": digest.hexdigest(), "executable": mode & 0o111}
        elif not stat.S_ISDIR(mode):
            raise ValueError("Unsupported publish-tree entry")
    if "Gccs.Api.dll" not in expected:
        raise ValueError("API assembly missing at root")
    return expected

def inspect_paths(archive_path, root_path, expected_sha256):
    archive_path = pathlib.Path(archive_path)
    before, result = inspect_archive_path(archive_path, expected_sha256)
    if result != inspect_tree(root_path):
        raise ValueError("Archive and publish-tree contents/modes differ")
    if archive_path.read_bytes() != before:
        raise ValueError("Archive changed during comparison")
    return result

def main(argv=None):
    import argparse, re
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", nargs="?"); parser.add_argument("expected_sha256", nargs="?"); parser.add_argument("--publish-root"); parser.add_argument("--check-tree")
    args = parser.parse_args(argv)
    try:
        if args.check_tree:
            if args.archive or args.expected_sha256 or args.publish_root:
                raise ValueError("Conflicting modes")
            result = inspect_tree(args.check_tree)
        else:
            if not args.archive or not re.fullmatch(r"[0-9a-f]{64}", args.expected_sha256 or ""):
                raise ValueError("Original digest required")
            result = inspect_paths(args.archive, args.publish_root, args.expected_sha256) if args.publish_root else inspect_archive_path(args.archive, args.expected_sha256)[1]
    except Exception:
        print("API package compatibility not established; preserve inputs and stop")
        return 1
    print("API package inspection passed (%d regular files)" % len(result))
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
