using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace SpecialCows.Tests
{
    public class ContentPatcherJsonTests
    {
        // Walk up from the test assembly directory until we find the repo root —
        // identified as the first ancestor that contains a "SpecialCows.CP" child directory.
        private static readonly string RepoRoot = FindRepoRoot();
        private static readonly string CpRoot = Path.Combine(RepoRoot, "SpecialCows.CP");

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "SpecialCows.CP")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException(
                $"Repo root not found: no ancestor of '{AppContext.BaseDirectory}' " +
                "contains a 'SpecialCows.CP' directory.");
        }

        // Short repo-relative label for readable failure messages.
        private static string Rel(string absPath) =>
            Path.GetRelativePath(RepoRoot, absPath);

        // ── MemberData sources ────────────────────────────────────────────────

        /// <summary>All .json files under SpecialCows.CP/, discovered recursively.</summary>
        public static IEnumerable<object[]> AllCpJsonFiles() =>
            Directory
                .EnumerateFiles(CpRoot, "*.json", SearchOption.AllDirectories)
                .OrderBy(p => p)
                .Select(p => new object[] { p });

        /// <summary>All .json files under SpecialCows.CP/assets/data/, discovered recursively.</summary>
        public static IEnumerable<object[]> DataDirJsonFiles() =>
            Directory
                .EnumerateFiles(
                    Path.Combine(CpRoot, "assets", "data"),
                    "*.json",
                    SearchOption.AllDirectories)
                .OrderBy(p => p)
                .Select(p => new object[] { p });

        // ── Test 1: every .json file under SpecialCows.CP/ is valid JSON ──────

        [Theory]
        [MemberData(nameof(AllCpJsonFiles))]
        public void JsonFile_ParsesWithoutException(string filePath)
        {
            // JsonDocument.Parse throws JsonException on malformed input.
            // We also assert the root is an object (not an array or scalar)
            // since all CP files must be JSON objects at the top level.
            var raw = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(raw);
            Assert.True(
                doc.RootElement.ValueKind == JsonValueKind.Object,
                $"{Rel(filePath)}: root element must be a JSON object, got {doc.RootElement.ValueKind}");
        }

        // ── Test 2: SpecialCows.CP/manifest.json has required CP/SMAPI fields ─

        [Fact]
        public void ManifestJson_HasRequiredSmapiFields()
        {
            var path = Path.Combine(CpRoot, "manifest.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("Name", out _),
                "manifest.json is missing required field 'Name'");
            Assert.True(root.TryGetProperty("UniqueID", out _),
                "manifest.json is missing required field 'UniqueID'");
            Assert.True(root.TryGetProperty("Version", out _),
                "manifest.json is missing required field 'Version'");
        }

        // ── Test 3: SpecialCows.CP/content.json has Format and Changes array ──

        [Fact]
        public void ContentJson_HasFormatFieldAndChangesArray()
        {
            var path = Path.Combine(CpRoot, "content.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("Format", out _),
                "content.json is missing required field 'Format'");

            Assert.True(root.TryGetProperty("Changes", out var changes),
                "content.json is missing required field 'Changes'");
            Assert.True(changes.ValueKind == JsonValueKind.Array,
                "content.json: 'Changes' must be a JSON array");
        }

        // ── Test 4: each assets/data/ file has a Changes array with ≥1 entry ──

        [Theory]
        [MemberData(nameof(DataDirJsonFiles))]
        public void DataFile_HasChangesArrayWithAtLeastOneEntry(string filePath)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
            var root = doc.RootElement;

            Assert.True(root.TryGetProperty("Changes", out var changes),
                $"{Rel(filePath)}: missing top-level 'Changes' array");
            Assert.True(changes.ValueKind == JsonValueKind.Array,
                $"{Rel(filePath)}: 'Changes' must be a JSON array");
            Assert.True(changes.GetArrayLength() >= 1,
                $"{Rel(filePath)}: 'Changes' must contain at least one entry");
        }
    }
}
