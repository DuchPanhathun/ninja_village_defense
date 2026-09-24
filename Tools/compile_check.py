#!/usr/bin/env python3
"""
Fast, lock-free C# compile check for Ninja Village Defense.

Reuses the exact compiler response files Unity generated (Library/Bee/artifacts/*/<Asm>.rsp)
— same defines, references, analyzers, langversion — but swaps in the CURRENT source files
and writes output to a temp dir. Runs Unity's bundled Roslyn, so it needs no open Editor and
never touches the project lock: many agents can run it concurrently.

Requires the project to have been opened/imported once (so Library/Bee has the .rsp files).

Usage:
  python3 Tools/compile_check.py            # NinjaVillage + NinjaVillage.Editor + Tests (Editor defines)
  python3 Tools/compile_check.py --player   # also compile NinjaVillage as an Android player (no UNITY_EDITOR)
  python3 Tools/compile_check.py --warnings # also print warnings located in Assets/_Project
Exit code 0 = no errors.
"""
import glob, os, re, subprocess, sys, tempfile

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def _editor_data():
    """UNITY_EDITOR_DATA env var, else the Hub install of the version in ProjectSettings/ProjectVersion.txt."""
    if os.environ.get("UNITY_EDITOR_DATA"):
        return os.environ["UNITY_EDITOR_DATA"]
    version = re.search(r"m_EditorVersion: (\S+)", open(os.path.join(PROJECT, "ProjectSettings/ProjectVersion.txt")).read()).group(1)
    candidates = [
        os.path.expanduser(f"~/Unity/Hub/Editor/{version}/Editor/Data"),                       # Linux
        f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents",                        # macOS
        f"C:/Program Files/Unity/Hub/Editor/{version}/Editor/Data",                            # Windows
    ]
    for path in candidates:
        if os.path.isdir(path):
            return path
    sys.exit(f"Unity {version} not found; set UNITY_EDITOR_DATA to its Editor/Data folder.")


EDITOR_DATA = _editor_data()
DOTNET = f"{EDITOR_DATA}/NetCoreRuntime/dotnet"
CSC = f"{EDITOR_DATA}/DotNetSdkRoslyn/csc.dll"

ASSEMBLIES = [
    # name, source globs (relative to project), exclude prefix
    ("NinjaVillage", ["Assets/_Project/Scripts/**/*.cs"], ["Assets/_Project/Scripts/Editor/"]),
    ("NinjaVillage.Editor", ["Assets/_Project/Scripts/Editor/**/*.cs"], []),
    ("NinjaVillage.Tests.EditMode", ["Assets/_Project/Tests/EditMode/**/*.cs"], []),
    ("NinjaVillage.Tests.PlayMode", ["Assets/_Project/Tests/PlayMode/**/*.cs"], []),
]


def find_rsp(name):
    hits = glob.glob(f"{PROJECT}/Library/Bee/artifacts/*.dag/{name}.rsp")
    if not hits:
        sys.exit(f"No {name}.rsp under Library/Bee — open/import the project once in Unity first.")
    return max(hits, key=os.path.getmtime)


def sources(globs, excludes):
    files = []
    for g in globs:
        for f in glob.glob(os.path.join(PROJECT, g), recursive=True):
            rel = os.path.relpath(f, PROJECT)
            if any(rel.startswith(e) for e in excludes):
                continue
            files.append(rel)
    return sorted(set(files))


def build_rsp(name, out_dir, player=False):
    lines = open(find_rsp(name), encoding="utf-8").read().splitlines()
    kept = []
    for line in lines:
        s = line.strip()
        if not s:
            continue
        if s.startswith('"') and s.endswith('.cs"'):
            continue  # old source list
        if s.startswith("-out:") or s.startswith("-refout:") or s.startswith("/additionalfile:"):
            continue
        # Point cross-references to our freshly compiled outputs.
        m = re.match(r'-r:"Library/Bee/artifacts/[^"]*/(NinjaVillage[^"/]*)\.ref\.dll"', s)
        if m:
            kept.append(f'-r:"{out_dir}/{m.group(1)}.dll"')
            continue
        if player:
            if s.startswith("-define:UNITY_EDITOR") or s == "-define:ENABLE_MONO":
                continue
            if s.startswith("-r:"):
                path = s[3:].strip('"')
                base = os.path.basename(path)
                editor_only = (base.startswith("UnityEditor") or ".Editor." in base or base.endswith(".Editor.ref.dll")
                               or (path.startswith("Assets/") and "/Editor/" in path))
                if editor_only:
                    continue
        kept.append(s)
    if player:
        kept += ["-define:UNITY_ANDROID", "-define:ENABLE_IL2CPP", "-define:DEVELOPMENT_BUILD"]
    suffix = ".Player" if player else ""
    kept.append(f'-out:"{out_dir}/{name}{suffix}.dll"')
    return kept


def compile_asm(name, globs, excludes, out_dir, player=False, show_warnings=False):
    files = sources(globs, excludes)
    if not files:
        return 0, []
    rsp_lines = build_rsp(name, out_dir, player) + [f'"{f}"' for f in files]
    rsp_path = os.path.join(out_dir, f"{name}{'.Player' if player else ''}.rsp")
    with open(rsp_path, "w", encoding="utf-8") as fh:
        fh.write("\n".join(rsp_lines) + "\n")
    proc = subprocess.run([DOTNET, "exec", CSC, "/noconfig", f"@{rsp_path}"],
                          cwd=PROJECT, capture_output=True, text=True)
    out = (proc.stdout or "") + (proc.stderr or "")
    errors = sorted(set(l.strip() for l in out.splitlines() if ": error " in l))
    warnings = sorted(set(l.strip() for l in out.splitlines()
                          if ": warning " in l and "Assets/_Project" in l)) if show_warnings else []
    label = f"{name}{' [player/Android]' if player else ''}"
    print(f"== {label}: {len(files)} files, {len(errors)} error(s)" + (f", {len(warnings)} warning(s)" if show_warnings else ""))
    for l in errors + warnings:
        print("  " + l)
    if proc.returncode != 0 and not errors:
        print(out[-3000:])
        return 1, []
    return len(errors), errors


def main():
    player = "--player" in sys.argv
    show_warnings = "--warnings" in sys.argv
    out_dir = tempfile.mkdtemp(prefix="nv_compile_")
    total = 0
    for name, globs, excludes in ASSEMBLIES:
        n, _ = compile_asm(name, globs, excludes, out_dir, show_warnings=show_warnings)
        total += n
        if n and name == "NinjaVillage":
            print("   (dependent assemblies skipped until NinjaVillage compiles)")
            break
    if player and total == 0:
        n, _ = compile_asm("NinjaVillage", ASSEMBLIES[0][1], ASSEMBLIES[0][2], out_dir, player=True, show_warnings=show_warnings)
        total += n
    print("RESULT:", "OK" if total == 0 else f"{total} error(s)")
    sys.exit(0 if total == 0 else 1)


if __name__ == "__main__":
    main()
