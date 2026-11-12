#!/usr/bin/env bash
# Builds the multi-version KirbyMod package (Git Bash):
#   KirbyMod.dll                  the loader (Loader/), picks the build matching the running game
#   compat/<version>/KirbyMod.dll the mod built against each game version that has refs in $REFS/game-<version>/
#   KirbyMod.pck, KirbyMod.json   shared
# Output: $DIST (the Workshop content folder). With --install, also replaces the game's mods/KirbyMod/.
set -euo pipefail
cd "$(dirname "$0")"

VERSIONS=(0.107.1 0.113.0)          # oldest first; the last one is also the default in Directory.Build.props
REFS=D:/StS2Modding/refs
DIST=D:/StS2Modding/dist/KirbyMod-0.3.0-workshop/KirbyMod
STAGE=D:/StS2Modding/dist/stage
GAME_MODS="D:/Steam/steamapps/common/Slay the Spire 2/mods/KirbyMod"

rm -rf "$STAGE" && mkdir -p "$STAGE"

# The pck does not depend on the game version, but its build (and MegaDot's inner build) needs one that has refs.
PCK_TARGET=""
for v in "${VERSIONS[@]}"; do [ -f "$REFS/game-$v/sts2.dll" ] && PCK_TARGET=$v; done
echo "== pck (built against $PCK_TARGET)"
RitsuLibReferenceTarget=$PCK_TARGET dotnet publish -c Debug -p:ModsPath="$STAGE/pub/" -v q -nologo | grep -E "error|Art:" || true
test -f "$STAGE/pub/KirbyMod/KirbyMod.pck"

echo "== loader"
dotnet build Loader/KirbyMod.Loader.csproj -c Debug -o "$STAGE/loader" -v q -nologo | grep -E " error " || true
test -f "$STAGE/loader/KirbyMod.Loader.dll"

for v in "${VERSIONS[@]}"; do
  if [ ! -f "$REFS/game-$v/sts2.dll" ]; then
    echo "!! no refs for $v ($REFS/game-$v), skipped"
    continue
  fi
  echo "== mod for game $v"
  dotnet build KirbyMod.csproj -t:Rebuild -c Debug -p:IsInnerGodotExport=true -p:RitsuLibReferenceTarget="$v" \
    -o "$STAGE/compat/$v" -v q -nologo | grep -E " error " || true
  test -f "$STAGE/compat/$v/KirbyMod.dll"
done

echo "== assemble $DIST"
rm -rf "$DIST" && mkdir -p "$DIST/compat"
cp "$STAGE/pub/KirbyMod/KirbyMod.pck" KirbyMod.json "$DIST/"
cp "$STAGE/loader/KirbyMod.Loader.dll" "$DIST/KirbyMod.dll"
for v in "${VERSIONS[@]}"; do
  [ -f "$STAGE/compat/$v/KirbyMod.dll" ] || continue
  mkdir -p "$DIST/compat/$v"
  cp "$STAGE/compat/$v/KirbyMod.dll" "$STAGE/compat/$v/KirbyMod.pdb" "$DIST/compat/$v/"
done
find "$DIST" -type f | sed "s|$DIST/||" | sort

if [ "${1:-}" = "--install" ]; then
  if tasklist | grep -qi SlayTheSpire2; then
    echo "!! game is running, not installed"
  else
    rm -rf "$GAME_MODS" && mkdir -p "$GAME_MODS" && cp -r "$DIST"/* "$GAME_MODS/"
    echo "== installed to $GAME_MODS"
  fi
fi
