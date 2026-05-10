# To-Do List

Sections are roughly in dependency order — art and data must land before the
gameplay loop is closeable, mail triggers depend on data being stable, etc.

---

## Art

Nothing ships without sprites. All texture work blocks the corresponding data entries.

- [x] **Strawberry Cow spritesheet** — sprite at `assets/animals/Strawberry Cow.png`.
  `FarmAnimalData.Texture` in `farm_animals.json` points to it via `{{InternalAssetKey}}`.
- [x] **Chocolate Cow spritesheet** — same spec as Strawberry Cow (adult only; no baby sprite needed).
- [x] **Item sprites** — `assets/items/items.png` is a 5×2 sheet of 16×16 px cells.
  `Texture` and `SpriteIndex` wired up in `items.json` for all ten items:
  - Row 0: StrawberryMilk (0), LargeStrawberryMilk (1), ChocolateMilk (2), LargeChocolateMilk (3), StrawberryIceCream (4)
  - Row 1: ChocolateIceCream (5), StrawberryTransformationTea (6), ChocolateTransformationTea (7), ReversalTea (8), HotChocolate (9)

---

## Content & Data

### Tea crafting recipes

- [x] **Add `Data/CraftingRecipes` entries** for all three teas.
  - Strawberry Transformation Tea: 2× Tea Leaves + 1× Strawberry
  - Chocolate Transformation Tea: 2× Tea Leaves + 1× Coffee Bean
  - Reversal Tea: 2× Tea Leaves + 1× Quartz
- [x] **Verify `%item craftingRecipe <name> %%` is a valid SDV 1.6 mail token.** Confirmed
  working — all three tea crafting recipes are correctly granted when the Marnie letter is opened.
- [x] **Gate crafting recipes behind Marnie's letter.** CP `When: HasFlag` condition on
  the `Data/CraftingRecipes` entry ensures recipes are absent until the Marnie letter has
  been received (i.e. `mailReceived` contains `StrawberryMilkMafia.SpecialCows_MarnieTea`).

### Cooking recipes

- [x] **Add `Data/CookingRecipes` entries** for Strawberry Ice Cream, Chocolate Ice Cream,
  and Hot Chocolate.
- [x] **Add `Data/Objects` entries** for all three cooking outputs (`StrawberryIceCream`,
  `ChocolateIceCream`, `HotChocolate`) in `items.json`, with stats, buffs, and prices.
- [x] **Finalize ingredient lists** — ice creams require 1× special milk + 1× Sugar (item
  245). Hot Chocolate requires only 1× Chocolate Milk.
- [x] **Verify recipe unlock** — `%item cookingRecipe` tokens require the `Data/CookingRecipes`
  key (e.g. `Strawberry Ice Cream`), not the item ID. Fixed in mail.json. A `SaveLoaded` hook
  in `ModEntry.cs` also self-heals: if the Demetrius letter is in `mailReceived` but any recipe
  is missing, it grants them directly — covers saves affected by the original bug and any future
  token failures.

### Mail triggers

- [x] **Demetrius cooking letter** (`SpecialCows_DemetriusCooking`) — delivery implemented
  via `Player.InventoryChanged` hook in `ModEntry.cs`. Fires the first time the player
  collects any special milk (Strawberry or Chocolate, regular or large); letter arrives
  the next morning. Teaches all three cooking recipes.
- [x] **Marnie tea letter** (`SpecialCows_MarnieTea`) — delivery implemented via
  `GameLoop.DayStarted` hook in `ModEntry.cs`. Conditions: friendship with Caroline ≥ 500
  (2♥) **AND** Caroline's sunroom event seen (event ID `719926`) **AND** friendship with
  Marnie ≥ 500 (2♥). Checks `mailReceived` before queuing to prevent duplicates.
- [x] **Finalise letter copy** — both letters have placeholder text. Hand to a writer to
  match Marnie's and Demetrius's vanilla voices before release.
- [ ] **Consider attaching a sample tea** to Marnie's letter (one `StrawberryTransformationTea`
  and one `ChocolateTransformationTea`) so new players can try the mechanic immediately.
  Use `%item (O){{ModId}}_StrawberryTransformationTea 1 %%` syntax.

### Artisan goods pipeline

Decided: skip cheese entirely. The cooking recipes (Ice Cream, Hot Chocolate) are the
only downstream products.

- [x] **Block special milks from the Cheese Press.** Achieved by stripping the
  `cow_milk_item` and `large_milk_item` context tags from all four special milks in
  `items.json`. The vanilla press matches inputs by these tags, so it now ignores
  them. No `Data/Machines` override needed.

---

## Code

- [x] **Verify deluxe produce path.** `DeluxeProduceItemIds` (large milk) is registered but
  the game's internal logic that calls `GetProduceID(deluxe: true)` vs. `(deluxe: false)` is
  tied to luck, friendship, and possibly profession bonuses. Test that large milk actually
  drops at high friendship before marking produce complete.
- [x] **Enable special cow pregnancy and fix calf type inheritance.** `CanGetPregnant: true`
  in `farm_animals.json` allows breeding in a Deluxe Barn. `BirthType: "White Cow"` stays
  as a sane data-level default, but a `DayStarted` fixup in `ModEntry.FixupNewbornCalves()`
  rewrites any age-0 calf born from a special cow to the parent's `OriginalType` (the
  pre-transformation breed). A transformed White Cow births White Cows; a transformed Brown
  Cow births Brown Cows. The `pregnate` test command also uses `ResolveCalfType()` so it
  exercises the same logic as natural birth.
- [ ] **Verify `ReloadTextureIfNeeded` is sufficient after transformation.** If the animal's
  produce or other runtime state doesn't update correctly mid-day, fall back to calling
  `animal.reload(animal.homeInterior)` instead, which does a full re-initialisation.
  There is an existing TODO comment in `TransformationHandler.cs`.
- [ ] **Targeting precision.** The current check (`Math.Abs(tile.X - cursor.X) <= 1`) can
  hit an adjacent animal. Consider tightening to `<= 0` (exact tile match), or switch to
  `FarmAnimal.GetCursorPetBoundingBox()` which already accounts for the animal's visual
  hitbox.
- [x] **Suppress action button during unrelated interactions.** `OnButtonPressed` guards
  early if a menu is open (`Game1.activeClickableMenu != null`) or the player is in an
  event (`Game1.CurrentEvent != null`). `TryTransform` now returns `bool`; when `true`,
  `Helper.Input.Suppress` is called so the cow status menu doesn't open after a transformation.
- [ ] **`GameLaunched` hook** — the TODO stub in `ModEntry.Entry` should be filled in if
  any inter-mod API surface is needed (e.g. GMCM config, SpaceCore compatibility).

---

## Polish & Release Prep

- [x] **Verify buff duration unit.** Confirmed in-game: `Duration: 600` and `Duration: 480`
  apply correctly and tick down at the same rate as vanilla buff foods like Spicy Eel.
  No adjustment needed.
- [ ] **Bump version numbers.** Both manifests are currently `0.0.1`. Decide on a versioning
  scheme and update before any public release.
- [x] **README build instructions** — written in `README.md`, covering default macOS path, `.csproj.user` override, and inline `ModsDir` override.
- [ ] **Localisation.** All display strings (HUD messages, item descriptions, mail text) are
  hardcoded English. Add an `i18n/default.json` file and replace literals with
  `helper.Translation.Get(...)` calls if non-English locales are a goal.
- [ ] **Nexus / mod page metadata** — description, screenshots, compatibility notes.
- [ ] **Controller targeting test.** The player who reported testing noted they usually play
  with a controller. Confirm the `IsActionButton()` check and tile-cursor logic work
  correctly with a controller before release (controller cursor tile may differ from mouse
  cursor tile).
