# To-Do List

Status as of v0.0.1 scaffold. Sections are roughly in dependency order — art and data must
land before the gameplay loop is closeable, mail triggers depend on data being stable, etc.

---

## Art

Nothing ships without sprites. All texture work blocks the corresponding data entries.

- [ ] **Strawberry Cow spritesheet** — 128 × 128 px, 4 columns × 4 rows, 32 × 32 px per
  frame. Must match the layout of `Animals/White Cow` so the existing animation indices work.
  Register via a CP `Load` action targeting `Animals/Strawberry Cow`; update
  `FarmAnimalData.Texture` in `farm_animals.json` to that path.
- [ ] **Chocolate Cow spritesheet** — same spec as Strawberry Cow.
- [ ] **Baby Strawberry Cow spritesheet** — matches `Animals/Baby White Cow` layout (64 × 64
  px, 2 columns × 2 rows, 32 × 32 px per frame). Update `FarmAnimalData.BabyTexture`.
- [ ] **Baby Chocolate Cow spritesheet** — same spec.
- [ ] **Item sprites** — all seven items currently fall back to the default object sheet at
  index 0/1/2, which gives them wrong icons. A single sprite sheet PNG with one 16 × 16 px
  cell per item is the minimum. Load it via CP, then update each entry in `items.json`:
  - `StrawberryMilk` / `LargeStrawberryMilk`
  - `ChocolateMilk` / `LargeChocolateMilk`
  - `StrawberryTransformationTea` / `ChocolateTransformationTea` / `ReversalTea`

---

## Content & Data

### Tea recipes (players have no way to obtain teas yet)

- [ ] **Add `Data/CraftingRecipes` entries** for all three teas. Decide on ingredients —
  current candidates: Strawberry + Tea Leaves + Sugar for the strawberry tea; Coffee Bean +
  Tea Leaves + Sugar for the chocolate tea; plain Tea Leaves for the reversal tea. Add the
  entries to a new `assets/data/crafting_recipes.json` and wire it into `content.json`.
- [ ] **Add output items for the artisan cooking recipes** (`StrawberryIceCream`,
  `HotChocolate`) to `assets/data/items.json` — they are referenced in
  `assets/data/recipes.json` but have no `Data/Objects` entries yet.

### Cooking recipes (stubs exist, but are incomplete)

- [ ] **Finalize ingredient lists** in `assets/data/recipes.json` — ingredient counts are
  marked TODO. Decide secondary ingredients (e.g. Sugar, Egg) and update the recipe strings.
- [ ] **Verify recipe unlock** — recipes are intended to be taught via Caroline/Marnie mail.
  Confirm the `%item cookingRecipe <name>%%` mail token syntax matches the recipe name
  strings exactly so the game teaches them on mail open.

### Mail triggers (letters exist, delivery does not)

- [ ] **Wire up heart-gated mail delivery.** The `Data/Mail` entries for
  `StrawberryMilkMafia.SpecialCows_CarolineTea` and `_MarnieTea` exist, but nothing sends
  them. Two options:
  - **CP approach:** add a `Data/Characters/Marnie` / `Data/Characters/Caroline` mail
    condition via `EditData` using game-state queries on friendship level.
  - **C# approach:** subscribe to `GameLaunched` or `DayStarted` in `ModEntry` and queue
    the mail via `Game1.mailbox.Add(...)` when both friendship thresholds are met and the
    letters haven't been sent yet (check `Game1.player.mailReceived`).
  - Both-conditions requirement (2♥ Caroline **and** 2♥ Marnie) must be enforced; neither
    letter should fire until both thresholds are crossed.
- [ ] **Finalise letter copy** — placeholder text stands in for both letters. Hand to a
  writer to match Caroline's and Marnie's vanilla voice before release.
- [ ] **Consider attaching a sample tea** to each letter (one `StrawberryTransformationTea`
  with Caroline's, one `ChocolateTransformationTea` with Marnie's) so new players can try
  the mechanic immediately. Use `%item (O)StrawberryMilkMafia.SpecialCows.CP_<id> 1 %%`.

### Artisan goods pipeline

- [ ] **Decide scope.** The original design brief mentions "artisan goods." Options:
  - Register Strawberry Milk / Chocolate Milk as valid Cheese Press inputs so they produce
    themed cheeses (`Data/Machines` in SDV 1.6).
  - Alternatively, keep the cooking recipes (Ice Cream, Hot Chocolate) as the only
    downstream products and skip cheese entirely.
- [ ] **If cheese:** add `Data/Objects` entries for Strawberry Cheese / Chocolate Cheese and
  configure the Cheese Press rules in `Data/Machines`.

---

## Code

- [ ] **Verify deluxe produce path.** `DeluxeProduceItemIds` (large milk) is registered but
  the game's internal logic that calls `GetProduceID(deluxe: true)` vs. `(deluxe: false)` is
  tied to luck, friendship, and possibly profession bonuses. Test that large milk actually
  drops at high friendship before marking produce complete.
- [ ] **Verify `ReloadTextureIfNeeded` is sufficient after transformation.** If the animal's
  produce or other runtime state doesn't update correctly mid-day, fall back to calling
  `animal.reload(animal.homeInterior)` instead, which does a full re-initialisation.
  There is an existing TODO comment in `TransformationHandler.cs`.
- [ ] **Targeting precision.** The current check (`Math.Abs(tile.X - cursor.X) <= 1`) can
  hit an adjacent animal. Consider tightening to `<= 0` (exact tile match), or switch to
  `FarmAnimal.GetCursorPetBoundingBox()` which already accounts for the animal's visual
  hitbox.
- [ ] **Suppress action button during unrelated interactions.** `OnButtonPressed` currently
  calls `TryTransform` on every right-click / controller-A press while the world is ready.
  Add an early-out if a menu is open (`Game1.activeClickableMenu != null`) or if the player
  is in an event (`Game1.CurrentEvent != null`).
- [ ] **`GameLaunched` hook** — the TODO stub in `ModEntry.Entry` should be filled in if
  any inter-mod API surface is needed (e.g. GMCM config, SpaceCore compatibility).

---

## Polish & Release Prep

- [ ] **Bump version numbers.** Both manifests are currently `0.0.1`. Decide on a versioning
  scheme and update before any public release.
- [ ] **README build instructions** — the "Build instructions TBD" stub in `README.md`.
- [ ] **Localisation.** All display strings (HUD messages, item descriptions, mail text) are
  hardcoded English. Add an `i18n/default.json` file and replace literals with
  `helper.Translation.Get(...)` calls if non-English locales are a goal.
- [ ] **Nexus / mod page metadata** — description, screenshots, compatibility notes.
- [ ] **Controller targeting test.** The player who reported testing noted they usually play
  with a controller. Confirm the `IsActionButton()` check and tile-cursor logic work
  correctly with a controller before release (controller cursor tile may differ from mouse
  cursor tile).
