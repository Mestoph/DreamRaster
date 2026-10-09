#!/usr/bin/env python3
# SPDX-License-Identifier: AGPL-3.0-or-later
# Copyright (c) 2026 Mestoph
"""Local source/archive verifier for DreamRaster.

Checks:
- ZIP integrity (CRC/readability, duplicate/path-traversal entries)
- generated/unwanted files in the source tree and ZIP
- JSON, XML and YAML syntax
- default download URLs documented in DOWNLOAD_SOURCES.md and mirrored in AppSettings.cs
- SHA-256 values documented in DOWNLOAD_SOURCES.md and mirrored in AppSettings.cs
- archive SHA-256 against a companion checksum file
- optional HTTP reachability for documented URLs without downloading model payloads

PyYAML is required for full YAML parsing. Install it with:
    python -m pip install -r scripts/requirements-verify.txt
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path, PurePosixPath
from typing import Iterable

try:
    import yaml  # type: ignore
except ImportError:  # handled as a verifier failure, with a useful message
    yaml = None

HEX64_RE = re.compile(r"^[0-9a-fA-F]{64}$")
URL_RE = re.compile(r"https?://[^\s<>`\]\[\)\(\"']+")

# Generated/local-only directories. The root-only list mirrors .gitignore entries;
# the anywhere list covers build/IDE caches that should never be in a source archive.
ROOT_ONLY_DIRS = {
    ".git",
    ".vs",
    "downloads",
    "images",
    "videos",
    "logs",
    "models",
    "runtime",
    "workspace",
    "build-reports",
    "artifacts",
}
ANYWHERE_DIRS = {"bin", "obj", ".vs", "TestResults", "__pycache__"}
UNWANTED_FILENAMES = {"Thumbs.db", "Desktop.ini", ".DS_Store"}
UNWANTED_SUFFIXES = (
    ".user",
    ".suo",
    ".userosscache",
    ".sln.docstates",
    ".part",
    ".tmp",
    ".fallback",
    ".curl.part",
    ".curlparts",
    ".pyc",
    ".pyo",
)
XML_SUFFIXES = {".xml", ".resx", ".csproj", ".props", ".targets", ".config", ".manifest", ".nuspec"}
YAML_SUFFIXES = {".yml", ".yaml"}


class Reporter:
    def __init__(self) -> None:
        self.failures: list[str] = []
        self.warnings: list[str] = []
        self.passed = 0

    def ok(self, message: str) -> None:
        self.passed += 1
        print(f"[OK]   {message}")

    def fail(self, message: str) -> None:
        self.failures.append(message)
        print(f"[FAIL] {message}")

    def warn(self, message: str) -> None:
        self.warnings.append(message)
        print(f"[WARN] {message}")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def normalize_relative(path: Path, root: Path) -> PurePosixPath:
    return PurePosixPath(path.relative_to(root).as_posix())


def is_unwanted(rel: PurePosixPath) -> bool:
    parts = rel.parts
    if not parts:
        return False

    # .nuget/packages is specifically a local restore cache.
    lowered = tuple(part.lower() for part in parts)
    for i in range(len(lowered) - 1):
        if lowered[i] == ".nuget" and lowered[i + 1] == "packages":
            return True

    if parts[0] in ROOT_ONLY_DIRS:
        return True
    if any(part in ANYWHERE_DIRS for part in parts[:-1]):
        return True
    if rel.name in UNWANTED_FILENAMES:
        return True
    if rel.as_posix().lower() == "config/settings.json":
        return True

    name_lower = rel.name.lower()
    return any(name_lower.endswith(suffix.lower()) for suffix in UNWANTED_SUFFIXES)


def iter_source_files(root: Path) -> Iterable[Path]:
    # A normal Git checkout contains root/.git. It is expected locally/CI and must
    # not be scanned as source content. Archive entries are checked separately,
    # where .git remains forbidden by is_unwanted().
    for current, dirs, files in os.walk(root):
        current_path = Path(current)
        if current_path == root:
            dirs[:] = [name for name in dirs if name != ".git"]
        elif current_path == root / ".nuget":
            # Le cache NuGet local est explicitement autorisé et ignoré ;
            # il n'appartient jamais aux sources à publier.
            dirs[:] = [name for name in dirs if name != "packages"]
        for name in files:
            path = current_path / name
            if path.is_file():
                yield path


def check_unwanted_tree(root: Path, report: Reporter) -> None:
    offenders = [normalize_relative(path, root).as_posix() for path in iter_source_files(root) if is_unwanted(normalize_relative(path, root))]
    if offenders:
        preview = ", ".join(offenders[:12])
        extra = f" (+{len(offenders) - 12})" if len(offenders) > 12 else ""
        report.fail(f"Fichiers locaux/générés présents dans les sources: {preview}{extra}")
    else:
        report.ok("Aucun fichier local/généré interdit dans les sources")


def check_zip_integrity(archive: Path, report: Reporter) -> list[str]:
    if not archive.is_file():
        report.fail(f"Archive introuvable: {archive}")
        return []

    try:
        with zipfile.ZipFile(archive, "r") as zf:
            names = [info.filename for info in zf.infolist()]
            duplicate_names = sorted({name for name in names if names.count(name) > 1})
            if duplicate_names:
                report.fail(f"Entrées ZIP dupliquées: {', '.join(duplicate_names[:10])}")
            else:
                report.ok("Aucune entrée ZIP dupliquée")

            unsafe = []
            for name in names:
                p = PurePosixPath(name)
                if p.is_absolute() or ".." in p.parts or re.match(r"^[A-Za-z]:", name):
                    unsafe.append(name)
            if unsafe:
                report.fail(f"Chemins ZIP dangereux: {', '.join(unsafe[:10])}")
            else:
                report.ok("Aucun chemin ZIP dangereux")

            bad = zf.testzip()
            if bad is None:
                report.ok(f"Intégrité ZIP/CRC valide ({len(names)} entrées)")
            else:
                report.fail(f"CRC ZIP invalide pour: {bad}")
            return names
    except (zipfile.BadZipFile, OSError) as exc:
        report.fail(f"Archive ZIP illisible: {exc}")
        return []


def strip_archive_root(name: str, source_root_name: str) -> PurePosixPath:
    p = PurePosixPath(name)
    parts = p.parts
    if parts and parts[0] == source_root_name:
        parts = parts[1:]
    return PurePosixPath(*parts) if parts else PurePosixPath(".")


def check_unwanted_archive(names: list[str], source_root_name: str, report: Reporter) -> None:
    offenders = []
    for name in names:
        if name.endswith("/"):
            continue
        rel = strip_archive_root(name, source_root_name)
        if rel.as_posix() != "." and is_unwanted(rel):
            offenders.append(name)
    if offenders:
        preview = ", ".join(offenders[:12])
        extra = f" (+{len(offenders) - 12})" if len(offenders) > 12 else ""
        report.fail(f"Fichiers locaux/générés présents dans l'archive: {preview}{extra}")
    else:
        report.ok("Aucun fichier local/généré interdit dans l'archive")


def check_json(root: Path, report: Reporter) -> None:
    files = sorted(path for path in iter_source_files(root) if path.suffix.lower() == ".json")
    errors: list[str] = []
    for path in files:
        try:
            with path.open("r", encoding="utf-8-sig") as stream:
                json.load(stream)
        except (OSError, UnicodeError, json.JSONDecodeError) as exc:
            errors.append(f"{path.relative_to(root)}: {exc}")
    if errors:
        report.fail("JSON invalides:\n       " + "\n       ".join(errors[:12]))
    else:
        report.ok(f"JSON valides ({len(files)} fichiers)")


def check_xml(root: Path, report: Reporter) -> None:
    files = sorted(path for path in iter_source_files(root) if path.suffix.lower() in XML_SUFFIXES)
    errors: list[str] = []
    for path in files:
        try:
            ET.parse(path)
        except (OSError, ET.ParseError) as exc:
            errors.append(f"{path.relative_to(root)}: {exc}")
    if errors:
        report.fail("XML invalides:\n       " + "\n       ".join(errors[:12]))
    else:
        report.ok(f"XML valides ({len(files)} fichiers)")


def make_unique_key_loader():
    if yaml is None:
        return None

    class UniqueKeyLoader(yaml.SafeLoader):  # type: ignore[misc, valid-type]
        pass

    def construct_mapping(loader, node, deep=False):
        mapping = {}
        for key_node, value_node in node.value:
            key = loader.construct_object(key_node, deep=deep)
            try:
                duplicate = key in mapping
            except TypeError:
                duplicate = False
            if duplicate:
                raise yaml.constructor.ConstructorError(  # type: ignore[attr-defined]
                    "while constructing a mapping",
                    node.start_mark,
                    f"duplicate key: {key!r}",
                    key_node.start_mark,
                )
            mapping[key] = loader.construct_object(value_node, deep=deep)
        return mapping

    UniqueKeyLoader.add_constructor(  # type: ignore[attr-defined]
        yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG,  # type: ignore[attr-defined]
        construct_mapping,
    )
    return UniqueKeyLoader


def check_yaml(root: Path, report: Reporter) -> None:
    files = sorted(path for path in iter_source_files(root) if path.suffix.lower() in YAML_SUFFIXES)
    if not files:
        report.ok("Aucun fichier YAML à valider")
        return
    if yaml is None:
        report.fail(
            "PyYAML est requis pour valider les YAML. Installez-le avec: "
            "python -m pip install -r scripts/requirements-verify.txt"
        )
        return

    loader = make_unique_key_loader()
    errors: list[str] = []
    for path in files:
        try:
            text = path.read_text(encoding="utf-8-sig")
            yaml.load(text, Loader=loader)  # type: ignore[arg-type, union-attr]
        except Exception as exc:  # PyYAML exposes several parser/scanner exception classes
            errors.append(f"{path.relative_to(root)}: {exc}")
    if errors:
        report.fail("YAML invalides:\n       " + "\n       ".join(errors[:12]))
    else:
        report.ok(f"YAML valides, clés dupliquées refusées ({len(files)} fichiers)")


def load_sample_settings(root: Path, report: Reporter) -> dict[str, object] | None:
    path = root / "config" / "settings.sample.json"
    try:
        with path.open("r", encoding="utf-8-sig") as stream:
            value = json.load(stream)
        if not isinstance(value, dict):
            raise ValueError("la racine JSON n'est pas un objet")
        return value
    except (OSError, UnicodeError, json.JSONDecodeError, ValueError) as exc:
        report.fail(f"Impossible de lire config/settings.sample.json: {exc}")
        return None


def extract_documented_urls(text: str) -> set[str]:
    return {match.rstrip(".,;:") for match in URL_RE.findall(text)}


def check_urls_and_hashes(root: Path, report: Reporter) -> set[str]:
    settings = load_sample_settings(root, report)
    if settings is None:
        return set()

    doc_path = root / "DOWNLOAD_SOURCES.md"
    app_settings_path = root / "src" / "DreamRaster" / "AppSettings.cs"
    try:
        doc = doc_path.read_text(encoding="utf-8-sig")
        app_settings = app_settings_path.read_text(encoding="utf-8-sig")
    except (OSError, UnicodeError) as exc:
        report.fail(f"Impossible de lire la documentation/configuration: {exc}")
        return set()

    default_urls = {
        key: value
        for key, value in settings.items()
        if key.lower().endswith("url") and isinstance(value, str) and value.startswith(("http://", "https://"))
    }
    documented_urls = extract_documented_urls(doc)

    malformed = []
    for url in sorted(documented_urls):
        parsed = urllib.parse.urlsplit(url)
        if parsed.scheme not in {"http", "https"} or not parsed.netloc:
            malformed.append(url)
    if malformed:
        report.fail(f"URLs documentées mal formées: {', '.join(malformed[:10])}")
    else:
        report.ok(f"Syntaxe des URLs documentées valide ({len(documented_urls)} URLs)")

    missing_doc = [f"{key}={value}" for key, value in default_urls.items() if value not in doc]
    if missing_doc:
        report.fail("URLs par défaut absentes de DOWNLOAD_SOURCES.md: " + "; ".join(missing_doc))
    else:
        report.ok(f"Toutes les URLs par défaut sont documentées ({len(default_urls)})")

    missing_code = [f"{key}={value}" for key, value in default_urls.items() if value not in app_settings]
    if missing_code:
        report.fail("URLs de settings.sample.json absentes de AppSettings.cs: " + "; ".join(missing_code))
    else:
        report.ok("URLs par défaut cohérentes avec AppSettings.cs")

    hashes = {
        key: value.lower()
        for key, value in settings.items()
        if key.lower().endswith("sha256") and isinstance(value, str)
    }
    invalid_hashes = [f"{key}={value}" for key, value in hashes.items() if not HEX64_RE.fullmatch(value)]
    if invalid_hashes:
        report.fail("SHA-256 de configuration invalides: " + "; ".join(invalid_hashes))
    else:
        report.ok(f"Format SHA-256 valide ({len(hashes)} valeurs)")

    missing_hash_doc = [f"{key}={value}" for key, value in hashes.items() if value not in doc.lower()]
    if missing_hash_doc:
        report.fail("SHA-256 absents de DOWNLOAD_SOURCES.md: " + "; ".join(missing_hash_doc))
    else:
        report.ok("Tous les SHA-256 par défaut sont documentés")

    missing_hash_code = [f"{key}={value}" for key, value in hashes.items() if value not in app_settings.lower()]
    if missing_hash_code:
        report.fail("SHA-256 de settings.sample.json absents de AppSettings.cs: " + "; ".join(missing_hash_code))
    else:
        report.ok("SHA-256 par défaut cohérents avec AppSettings.cs")

    return documented_urls


def parse_checksum_file(path: Path, archive: Path) -> str:
    text = path.read_text(encoding="utf-8-sig").strip()
    if not text:
        raise ValueError("fichier de checksum vide")
    first = text.splitlines()[0].strip()
    match = re.match(r"^([0-9a-fA-F]{64})(?:\s+[*]?(.+))?$", first)
    if not match:
        raise ValueError("format attendu: <sha256>  <nom-fichier>")
    expected = match.group(1).lower()
    declared_name = (match.group(2) or "").strip()
    if declared_name and Path(declared_name).name != archive.name:
        raise ValueError(f"le checksum référence '{declared_name}', pas '{archive.name}'")
    return expected


def check_archive_checksum(archive: Path | None, checksum: Path | None, report: Reporter) -> None:
    if archive is None:
        report.warn("Aucune archive fournie: contrôle SHA-256 de l'archive ignoré")
        return
    if not archive.is_file():
        report.fail(f"Archive introuvable pour SHA-256: {archive}")
        return
    actual = sha256_file(archive)
    report.ok(f"SHA-256 calculé pour {archive.name}: {actual}")

    if checksum is None:
        report.warn("Aucun fichier .sha256.txt fourni: comparaison du SHA-256 ignorée")
        return
    if not checksum.is_file():
        report.fail(f"Fichier de checksum introuvable: {checksum}")
        return
    try:
        expected = parse_checksum_file(checksum, archive)
    except (OSError, UnicodeError, ValueError) as exc:
        report.fail(f"Checksum illisible/invalide: {exc}")
        return
    if actual.lower() == expected:
        report.ok("SHA-256 de l'archive conforme au fichier de checksum")
    else:
        report.fail(f"SHA-256 différent: attendu {expected}, obtenu {actual}")


def http_probe(url: str, timeout: float) -> tuple[bool, str]:
    headers = {
        "User-Agent": "DreamRaster-LocalVerifier/1.0",
        "Accept": "*/*",
    }
    request = urllib.request.Request(url, method="HEAD", headers=headers)
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            code = getattr(response, "status", 200)
            return 200 <= code < 400, f"HTTP {code}"
    except urllib.error.HTTPError as exc:
        # Some file hosts reject HEAD. Retry a tiny ranged GET, then close immediately.
        if exc.code in {400, 403, 405, 416}:
            range_headers = dict(headers)
            range_headers["Range"] = "bytes=0-0"
            request = urllib.request.Request(url, method="GET", headers=range_headers)
            try:
                with urllib.request.urlopen(request, timeout=timeout) as response:
                    code = getattr(response, "status", 200)
                    return 200 <= code < 400, f"HTTP {code} (GET Range)"
            except Exception as retry_exc:
                return False, str(retry_exc)
        return False, f"HTTP {exc.code}"
    except Exception as exc:
        return False, str(exc)


def check_network_urls(urls: set[str], timeout: float, report: Reporter) -> None:
    if not urls:
        report.warn("Aucune URL documentée à tester sur le réseau")
        return
    failures = []
    for index, url in enumerate(sorted(urls), 1):
        ok, detail = http_probe(url, timeout)
        print(f"       [{index}/{len(urls)}] {'OK' if ok else 'FAIL'} {url} -> {detail}")
        if not ok:
            failures.append(f"{url} ({detail})")
    if failures:
        report.fail("URLs inaccessibles: " + "; ".join(failures[:10]))
    else:
        report.ok(f"Accessibilité HTTP vérifiée ({len(urls)} URLs)")


def auto_archive(root: Path) -> Path | None:
    candidates = sorted(root.parent.glob("DreamRaster*.zip"))
    return candidates[0] if len(candidates) == 1 else None


def auto_checksum(archive: Path | None) -> Path | None:
    if archive is None:
        return None
    exact = Path(str(archive) + ".sha256.txt")
    if exact.is_file():
        return exact
    alt = archive.with_suffix(".sha256.txt")
    return alt if alt.is_file() else None


def resolve_path(value: str | None, base: Path) -> Path | None:
    if value is None:
        return None
    p = Path(value).expanduser()
    return p.resolve() if p.is_absolute() else (base / p).resolve()


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Vérifie localement les sources et l'archive DreamRaster.")
    parser.add_argument("--root", help="Racine du dépôt (défaut: parent de scripts/).")
    parser.add_argument("--archive", help="Archive ZIP source à vérifier. Auto-détectée si possible à côté du dépôt.")
    parser.add_argument("--checksum", help="Fichier SHA-256 de l'archive. Auto-détecté si possible.")
    parser.add_argument("--check-network", action="store_true", help="Teste aussi l'accessibilité HTTP des URLs documentées.")
    parser.add_argument("--network-timeout", type=float, default=12.0, help="Timeout HTTP par URL, en secondes (défaut: 12).")
    return parser


def main() -> int:
    args = build_parser().parse_args()
    script_path = Path(__file__).resolve()
    default_root = script_path.parent.parent
    root = resolve_path(args.root, Path.cwd()) or default_root

    if not root.is_dir():
        print(f"[FAIL] Racine introuvable: {root}", file=sys.stderr)
        return 2

    archive = resolve_path(args.archive, Path.cwd()) if args.archive else auto_archive(root)
    checksum = resolve_path(args.checksum, Path.cwd()) if args.checksum else auto_checksum(archive)

    print("DreamRaster - vérification locale")
    print(f"Racine   : {root}")
    print(f"Archive  : {archive if archive else '(non fournie / non détectée)'}")
    print(f"Checksum : {checksum if checksum else '(non fourni / non détecté)'}")
    print()

    report = Reporter()
    check_unwanted_tree(root, report)
    check_json(root, report)
    check_xml(root, report)
    check_yaml(root, report)
    documented_urls = check_urls_and_hashes(root, report)

    archive_names: list[str] = []
    if archive is not None:
        archive_names = check_zip_integrity(archive, report)
        if archive_names:
            check_unwanted_archive(archive_names, root.name, report)
    else:
        report.warn("Archive non détectée: contrôles ZIP ignorés")

    check_archive_checksum(archive, checksum, report)

    if args.check_network:
        check_network_urls(documented_urls, max(1.0, args.network_timeout), report)

    print()
    print(f"Résumé: {report.passed} OK, {len(report.warnings)} avertissement(s), {len(report.failures)} échec(s).")
    if report.failures:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
