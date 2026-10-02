"""Write stable metadata for new source files without regenerating any assets."""
import hashlib
from pathlib import Path

root = Path(__file__).resolve().parents[1]
for source in (root / "Assets").rglob("*.cs"):
    meta = Path(str(source) + ".meta")
    if meta.exists():
        continue
    relative = source.relative_to(root).as_posix()
    guid = hashlib.md5(("space-attack/" + relative).encode()).hexdigest()
    meta.write_text(f"fileFormatVersion: 2\nguid: {guid}\nMonoImporter:\n  externalObjects: {{}}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {{instanceID: 0}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n", encoding="utf-8")
