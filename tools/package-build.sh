#!/usr/bin/env bash
# Package le build Web Unity au format borne (index.html + Build/ + thumbnail.png + info.json)
# et le copie dans la borne locale (tools/anatidae-arcade/public/RythmeRunner).
# Usage : tools/package-build.sh [dossier_du_build]   (défaut : game/Build)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="${1:-$ROOT/game/Build}"
GAME=RythmeRunner
DIST="$ROOT/dist/$GAME"
ARCADE="$ROOT/tools/anatidae-arcade/public/$GAME"

[ -f "$SRC/index.html" ] || { echo "❌ Pas de index.html dans $SRC : fais d'abord un build Web dans Unity."; exit 1; }
[ -f "$ROOT/game/BuildExtras/info.json" ] || { echo "❌ game/BuildExtras/info.json manquant"; exit 1; }
[ -f "$ROOT/game/BuildExtras/thumbnail.png" ] || { echo "❌ game/BuildExtras/thumbnail.png manquant"; exit 1; }

rm -rf "$DIST" && mkdir -p "$DIST"
cp -R "$SRC"/. "$DIST"/
cp "$ROOT/game/BuildExtras/info.json" "$ROOT/game/BuildExtras/thumbnail.png" "$DIST"/
[ -f "$ROOT/game/BuildExtras/attract.mp4" ] && cp "$ROOT/game/BuildExtras/attract.mp4" "$DIST"/
echo "✅ $DIST"

if [ -d "$ROOT/tools/anatidae-arcade/public" ]; then
  rm -rf "$ARCADE" && cp -R "$DIST" "$ARCADE"
  echo "✅ copié dans la borne locale → lance : cd tools/anatidae-arcade && node server.js puis http://localhost:3000"
else
  echo "ℹ️  Borne locale absente (voir docs/anatidae.md § Tester en local)"
fi
