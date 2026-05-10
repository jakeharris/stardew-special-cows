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
path is transformation. Special cows can become pregnant in a Deluxe Barn
(`CanGetPregnant: true`), but calves born from them are normal White Cows
(`BirthType: "White Cow"`).

---

## Item IDs and naming

CP data keys use `{{ModId}}` which resolves to `StrawberryMilkMafia.SpecialCows.CP`.
C# constants use the fully-qualified form (e.g. `"StrawberryMilkMafia.SpecialCows.CP_StrawberryMilk"`).
Mail IDs use an underscore before the name: `StrawberryMilkMafia.SpecialCows_MarnieTea`.

Never hardcode `SpecialCows.CP_` in CP data values — always use `{{ModId}}_`.

---

## Tea crafting recipes

Taught by Marnie's letter (`SpecialCows_MarnieTea`). Trigger is implemented via a
`GameLoop.DayStarted` hook in `ModEntry.cs`; recipes are also CP-gated on the mail
flag (`HasFlag |contains=StrawberryMilkMafia.SpecialCows_MarnieTea`) so they can't
appear in the crafting menu before the letter arrives.

| Tea | Ingredients | Price |
|---|---|---|
| Strawberry Transformation Tea | 2× Tea Leaves (815) + 1× Strawberry (400) | 80g |
| Chocolate Transformation Tea | 2× Tea Leaves (815) + 1× Coffee Bean (433) | 80g |
| Reversal Tea | 2× Tea Leaves (815) + 1× Quartz (80) | 60g |

Teas are inedible (edibility -300), not giftable, and can be shipped.

### Marnie letter trigger conditions
- Friendship with Caroline ≥ 500 (2♥)
- Caroline's sunroom event seen (event ID `719926`)
- Friendship with Marnie ≥ 500 (2♥)

The `DayStarted` hook checks `Game1.player.mailReceived` before queuing to prevent
duplicates. The `%item craftingRecipe <name> %%` mail token is confirmed working in
SDV 1.6 — all three recipes are correctly granted when the letter is opened.

---

## Cooking recipes

Taught by Demetrius's letter (`SpecialCows_DemetriusCooking`). Trigger is implemented:
`Player.InventoryChanged` fires when the player first collects any special milk; letter
arrives the next morning.

| Recipe | Ingredients | Output price | Buffs |
|---|---|---|---|
| Strawberry Ice Cream | 1× Strawberry Milk + 1× Sugar (245) | 175g | +1 Luck |
| Chocolate Ice Cream | 1× Chocolate Milk + 1× Sugar (245) | 175g | +1 Mining |
| Hot Chocolate | 1× Chocolate Milk | 185g | +1 Mining, +1 Defense |

All three are `Type: "Cooking"`, `Category: -7`. Not artisan goods (not Category -26).
Hot Chocolate is `IsDrink: true` — uses drink animation, no alcohol debuff.
Buff `Duration` matches the unit used by vanilla buff foods like Spicy Eel (600
ice cream / 480 hot chocolate) — confirmed applying and ticking correctly in-game.

Output prices are deliberately set so cooking the ice creams is a slight loss
versus shipping the milk raw (175g − 180g milk − ~100g sugar). Cooking is for
the buff, not the margin. Hot Chocolate is roughly break-even on milk (+5g).

---

## Milk items

| Item | Price | Edibility |
|---|---|---|
| Strawberry Milk | 180g | 18 |
| Large Strawberry Milk | 270g | 25 |
| Chocolate Milk | 180g | 18 |
| Large Chocolate Milk | 270g | 25 |

Special milks deliberately omit the `cow_milk_item` and `large_milk_item` context
tags so the Cheese Press rejects them — keeping Marnie's letter's promise that
"the milk won't make cheese." This is the only intended consumer of those tags
that we care about; gift tastes and quests use item IDs / categories, not these
tags. The `category_milk` (Category -6) classification is preserved, so Rancher
profession bonuses still apply.

Pricing places special milk between vanilla Cow Milk (125g) and Goat Milk (225g).
This is intentionally good for early-to-mid game (44% uplift on raw shipping)
and gives way to Cheese (230g via Cheese Press, +84% on regular Cow Milk) in
late game when scale matters. Reversal Tea is the bridge: a player committing
to a cheese economy can revert specific cows.

---

## Key design decisions

- **No Cheese Press integration.** Enforced by stripping `cow_milk_item` / `large_milk_item`
  context tags from the special milks. Cooking recipes are the only downstream products.
- **No secondary ingredients for Hot Chocolate.** Just the milk.
- **Prices have deliberate texture** — avoid round numbers where a slightly odd value
  (e.g. 185g) makes the economy feel more alive.
- **Teas are shippable** (ExcludeFromShippingCollection = false), comparable to Green Tea.
- **No day-1 crafting recipe access.** Crafting recipes are gated behind Marnie's letter
  via a CP `HasFlag` condition on `Data/CraftingRecipes`.
- **Cooking is a buff path, not a money path.** Ice cream recipes lose money vs raw shipping;
  Hot Chocolate breaks even. Players cook for the buff or for the food.
- **Special cows can breed but don't breed true.** `CanGetPregnant: true` in
  `farm_animals.json`. Calves inherit the parent's *pre-transformation* type, not the
  special type: a transformed White Cow births White Cows, a transformed Brown Cow births
  Brown Cows. Enforced by a `DayStarted` fixup (`FixupNewbornCalves` in `ModEntry.cs`)
  that rewrites age-0 calves using the parent's `OriginalType` modData. `BirthType: "White Cow"`
  stays in data as a fallback for the unreachable "non-transformed special cow" edge case.
- **Mid-game vs late-game tension.** Special milk shipping (180g) beats raw vanilla milk
  (125g) but loses to vanilla Cheese (230g via press). Reversal Tea exists so a player
  scaling cheese production can opt cows back out of the special path.

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

## Keeping docs current

After every change — code or data — update these two files before closing the task:

- `docs/to-dos.md` — mark completed items `[x]`, update wording to reflect what was actually done, and remove stale caveats (e.g. "not yet implemented" notes that are now resolved).
- `docs/test-plan.md` — remove gap notices that have been addressed, update "not yet implemented" annotations on test cases that are now implemented, and add new test cases for any new behavior.

Neither file needs a perfect rewrite on every pass — just keep them honest about the current state of the mod.

---

## Code style

- Nullable enabled; use `?` and null-checks rather than suppressing warnings
- No implicit usings — all `using` statements are explicit
- Prefer named constants over inline strings for item IDs and mod data keys
- Comments explain *why*, not *what* — the existing code in `TransformationHandler.cs`
  is a good model for the preferred style
