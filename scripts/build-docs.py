#!/usr/bin/env python3
"""Build all documentation languages using one shared DocFX configuration.

Requires Python 3 and the restored local DocFX tool. Run from any directory;
generated configurations and HTML stay outside the tracked source files.
"""

from copy import deepcopy
import json
import os
from pathlib import Path
import shutil
import subprocess


ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "docs/site"
OUTPUT = ROOT / "artifacts/docs-site"
LANGUAGES = ("sk", "cs", "ru", "uk")


def relative_source(path):
    """DocFX requires sources outside its config directory to be relative."""
    return os.path.relpath(path, SITE / "obj").replace("\\", "/")


def main():
    # Clear only the generated site so removed pages cannot survive a rebuild.
    if OUTPUT.exists():
        shutil.rmtree(OUTPUT)
    subprocess.run(
        ["dotnet", "docfx", str(SITE / "docfx.json"), "--warningsAsErrors"],
        cwd=ROOT,
        check=True,
    )

    base = json.loads((SITE / "docfx.json").read_text(encoding="utf-8"))
    generated = SITE / "obj"
    generated.mkdir(exist_ok=True)
    for language in LANGUAGES:
        source = SITE / "locales" / language
        config = {"build": deepcopy(base["build"])}
        build = config["build"]
        # Reuse English API metadata, extracted once from the real C# project.
        # Each language gets its own HTML and search index, with stable page names.
        build["content"] = [
            {"files": ["*.md", "toc.yml", "api/index.md"], "src": relative_source(source)},
            {"files": ["*.yml"], "src": relative_source(SITE / "api"), "dest": "api"},
        ]
        build["resource"] = [{"files": ["assets/logo.svg"], "src": relative_source(ROOT / "docs")}]
        build["dest"] = str(OUTPUT / language)
        build["template"] = ["default", "modern", str(SITE / "template"), str(source / "template")]
        build["globalMetadata"].update({
            "_lang": language,
            "_appTitle": json.loads((source / "template/token.json").read_text(encoding="utf-8"))["appTitle"],
        })
        config_path = generated / f"docfx.{language}.json"
        config_path.write_text(json.dumps(config, ensure_ascii=False, indent=2), encoding="utf-8")
        subprocess.run(
            ["dotnet", "docfx", "build", str(config_path), "--warningsAsErrors"],
            cwd=ROOT,
            check=True,
        )

    # GitHub Pages must serve DocFX's underscore-prefixed assets unchanged.
    (OUTPUT / ".nojekyll").touch()
    print(f"Built English, Slovak, Czech, Russian and Ukrainian: {OUTPUT}")


if __name__ == "__main__":
    main()
