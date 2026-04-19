# Special Cows — Project Guide

Stardew Valley mod that adds two special cow types (Strawberry Cow, Chocolate Cow)
obtainable only through a transformation mechanic. Currently a personal mod; Nexus Mods
is the eventual release target.

Made by **Strawberry Milk Mafia** (team name). The `UniqueID` uses `StrawberryMilkMafia`
(no spaces) because spaces break CP tokens and file paths. The `Author` display field in
both manifests can and should read `"Strawberry Milk Mafia"` with spaces.

---

## Architecture

Two components that must be deployed together:

| Directory | Type | Mod ID |
|---|---|---|
| `SpecialCows/` | C# SMAPI mod (net6.0) | `StrawberryMilkMafia.SpecialCows` |
| `SpecialCows.CP/` | Content Patcher pack | `StrawberryMilkMafia.SpecialCows.CP` |

The C# mod handles runtime logic (transformation, mail triggers). The CP pack owns all
data (items, animals, recipes, mail). Neither works alone.

## Build & deploy

```
dotnet build
```

Run from `SpecialCows/` (or the solution root). The post-build target in `SpecialCows.csproj`
automatically deploys the CP pack to the macOS Steam Mods folder. To override the path,
add a `.csproj.user` file (gitignored) or pass it on the command line:

```
dotnet build /p:ModsDir="/path/to/Stardew Valley/Mods"
```

Default deploy target: `~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods`

---

## Core mechanic — transformation

The player holds a tea item and right-clicks (or presses the action button) on an adult
cow. `TransformationHandler.TryTransform()` runs on every `ButtonPressed` event and
gates early if the held item isn't one of the three teas.

- Valid target animals: White Cow, Brown Cow, Strawberry Cow, Chocolate Cow
- Only adult cows can be transformed (checked via `FarmAnimal.isAdult()`)
- Original type is stored in `animal.modData["StrawberryMilkMafia.SpecialCows/OriginalType"]`
- Produce is cleared on transformation so the new type's milk can drop the same day
- Texture reloads via `ReloadTextureIfNeeded(forceReload: true)`

Strawberry and Chocolate Cows are **not purchasable from Marnie**. The only acquisition
path is transformation. Calves born from special cows are normal White Cows (`BirthType = "White Cow"`).

---

## Item IDs and naming

CP data keys use `{{ModId}}` which resolves to `StrawberryMilkMafia.SpecialCows.CP`.
C# constants use the fully-qualified form (e.g. `"StrawberryMilkMafia.SpecialCows.CP_StrawberryMilk"`).
Mail IDs use an underscore before the name: `StrawberryMilkMafia.SpecialCows_MarnieTea`.

Never hardcode `SpecialCows.CP_` in CP data values — always use `{{ModId}}_`.

---

## Tea crafting recipes

Taught by Marnie's letter. Trigger not yet implemented — see to-dos.

| Tea | Ingredients | Price |
|---|---|---|
| Strawberry Transformation Tea | 2× Tea Leaves (815) + 1× Strawberry (398) | 80g |
| Chocolate Transformation Tea | 2× Tea Leaves (815) + 1× Coffee Bean (433) | 80g |
| Reversal Tea | 2× Tea Leaves (815) + 1× Quartz (80) | 60g |

Teas are inedible (edibility -300), not giftable, and can be shipped.

### Marnie letter trigger conditions
- Friendship with Caroline ≥ 500 (2♥)
- Caroline's sunroom event seen (event ID `719926`)
- Friendship with Marnie ≥ 500 (2♥)

Needs a `DayStarted` hook in `ModEntry.cs`. Check `Game1.player.mailReceived` before queuing.
Also verify that `%item craftingRecipe <name> %%` is a valid SDV 1.6 mail token.

---

## Cooking recipes

Taught by Demetrius's letter (`SpecialCows_DemetriusCooking`). Trigger is implemented:
`Player.InventoryChanged` fires when the player first collects any special milk; letter
arrives the next morning.

| Recipe | Ingredients | Output price | Buffs |
|---|---|---|---|
| Strawberry Ice Cream | 1× Strawberry Milk + 1× Sugar (245) | 135g | +1 Luck |
| Chocolate Ice Cream | 1× Chocolate Milk + 1× Sugar (245) | 135g | +1 Mining |
| Hot Chocolate | 1× Chocolate Milk | 185g | +1 Mining, +1 Defense |

All three are `Type: "Cooking"`, `Category: -7`. Not artisan goods (not Category -26).
Hot Chocolate is `IsDrink: true` — uses drink animation, no alcohol debuff.
Buff duration unit is unconfirmed (currently 600s ice cream / 480s hot chocolate) — verify in-game.

---

## Milk items

| Item | Price | Edibility |
|---|---|---|
| Strawberry Milk | 180g | 30 |
| Large Strawberry Milk | 270g | 50 |
| Chocolate Milk | 180g | 30 |
| Large Chocolate Milk | 270g | 50 |

Both milks have `ContextTag: "cow_milk_item"` which makes them valid Cheese Press inputs
by default. No cheese items exist — artisan goods pipeline was decided against.

---

## Key design decisions

- **No Cheese Press integration.** Cooking recipes are the only downstream products.
- **No secondary ingredients for Hot Chocolate.** Just the milk.
- **Prices have deliberate texture** — avoid round numbers where a slightly odd value
  (e.g. 185g) makes the economy feel more alive.
- **Teas are shippable** (ExcludeFromShippingCollection = false), comparable to Green Tea.
- **No day-1 crafting recipe access** is intended, but the gate is not yet implemented.
- **Calves are always vanilla White Cows.** Special cow types don't breed true.

---

## File map

```
SpecialCows/
  ModEntry.cs                 — entry point, event subscriptions
  TransformationHandler.cs    — all transformation logic

SpecialCows.CP/
  content.json                — includes all data files
  assets/data/
    items.json                — Data/Objects (all 10 custom items)
    farm_animals.json         — Data/FarmAnimals (Strawberry Cow, Chocolate Cow)
    recipes.json              — Data/CookingRecipes
    crafting_recipes.json     — Data/CraftingRecipes
    mail.json                 — Data/Mail (Marnie tea letter, Demetrius cooking letter)

docs/
  to-dos.md                   — canonical remaining work, kept up to date
  test-plan.md                — checklist of every behavior to verify
```

---

## Code style

- Nullable enabled; use `?` and null-checks rather than suppressing warnings
- No implicit usings — all `using` statements are explicit
- Prefer named constants over inline strings for item IDs and mod data keys
- Comments explain *why*, not *what* — the existing code in `TransformationHandler.cs`
  is a good model for the preferred style
