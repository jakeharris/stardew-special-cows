# Strawberry & Chocolate Cows

A SMAPI mod for Stardew Valley that lets players transform adult cows into strawberry or chocolate variants using special transformation teas crafted from unique recipes. Strawberry cows produce Strawberry Milk and chocolate cows produce Chocolate Milk, each usable in themed artisan goods. Recipe letters arrive via mail once you've built a friendship with Caroline and Marnie. Built with SMAPI + Content Patcher on the SDV 1.6 API — no Json Assets required. v1.0 scope covers the three transformation teas, two milk variants each with a large version, two artisan cooking recipes, and the reversal tea to restore a cow to its original type.

## Getting Started

### Prerequisites

- [SMAPI](https://smapi.io/) 4.0.0+
- [Content Patcher](https://www.nexusmods.com/stardewvalley/mods/1915) (latest)
- Stardew Valley 1.6+

### Build

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

### Install (pre-built)

No pre-built release exists yet. Build from source using the steps above.
