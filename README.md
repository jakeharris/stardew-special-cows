# Strawberry & Chocolate Cows

A SMAPI mod for Stardew Valley that lets players transform adult cows into strawberry or chocolate variants using special transformation teas. Strawberry cows produce Strawberry Milk and chocolate cows produce Chocolate Milk, each usable in themed cooking recipes. Marnie mails the tea recipes once you've built a friendship with Caroline and Marnie, and Demetrius mails two ice cream recipes the day after you first collect special milk. Built with SMAPI + Content Patcher on the SDV 1.6 API — no Json Assets required. v1.0 covers two transformation teas plus a Reversal Tea that restores a cow to its original type, two milk variants each with a large version, and three cooking recipes (Strawberry Ice Cream, Chocolate Ice Cream, and Hot Chocolate, whose recipe Gus sells at the Stardrop Saloon).

## Getting Started

### Prerequisites

- [SMAPI](https://smapi.io/) 4.0.0+
- [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) 2.3.0+
- Stardew Valley 1.6+

### Installing the mod

Download `SpecialCows 1.0.0.zip` from the [1.0.0 release](https://github.com/jakeharris/stardew-special-cows/releases/tag/1.0.0)
(also checked in under `releases/`). The zip holds a `SpecialCows 1.0.0` folder with two
folders inside: SpecialCows and SpecialCows.CP. Move both into your Mods folder. Neither
works without the other.

Windows: `C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods`
Linux: `~/.local/share/Steam/steamapps/common/Stardew Valley/Mods`
macOS: `~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods`

## Development

1. Install the [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0).
2. Clone this repo.
3. Run from the repo root:

```sh
dotnet build
```

The build compiles the C# mod and automatically deploys the Content Patcher pack to the
default macOS Steam Mods folder:

```
~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods
```

**Different install path?** Create `SpecialCows/SpecialCows.csproj.user` (gitignored) with
your path:

```xml
<Project>
  <PropertyGroup>
    <ModsDir>/path/to/Stardew Valley/Mods</ModsDir>
  </PropertyGroup>
</Project>
```

Or pass it inline:

```sh
dotnet build /p:ModsDir="/path/to/Stardew Valley/Mods"
```

### Packaging a release

```sh
./package.sh
```

This builds the mod, reads the version from `SpecialCows/manifest.json`, and writes
`releases/SpecialCows <version>.zip` containing both folders.
