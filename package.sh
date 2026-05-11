#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

# Read version from the C# mod manifest
VERSION=$(grep '"Version"' "$SCRIPT_DIR/SpecialCows/manifest.json" | sed 's/.*"Version": "\(.*\)".*/\1/')

RELEASE_NAME="SpecialCows $VERSION"
RELEASE_DIR="$SCRIPT_DIR/releases/$RELEASE_NAME"
BUILD_OUT="$SCRIPT_DIR/SpecialCows/bin/Debug/net6.0"

echo "Building $RELEASE_NAME..."
dotnet build "$SCRIPT_DIR/SpecialCows/SpecialCows.csproj" -nologo -clp:ErrorsOnly

mkdir -p "$RELEASE_DIR/SpecialCows" "$RELEASE_DIR/SpecialCows.CP"

cp "$BUILD_OUT/SpecialCows.dll" "$RELEASE_DIR/SpecialCows/"
cp "$SCRIPT_DIR/SpecialCows/manifest.json" "$RELEASE_DIR/SpecialCows/"

cp -r "$SCRIPT_DIR/SpecialCows.CP/." "$RELEASE_DIR/SpecialCows.CP/"
find "$RELEASE_DIR" \( -name ".gitkeep" -o -name "*.aseprite" \) -delete

ZIP_PATH="$SCRIPT_DIR/releases/$RELEASE_NAME.zip"
rm -f "$ZIP_PATH"
(cd "$SCRIPT_DIR/releases" && zip -qr "$ZIP_PATH" "$RELEASE_NAME")
rm -rf "$RELEASE_DIR"

echo "Released: releases/$RELEASE_NAME.zip"
