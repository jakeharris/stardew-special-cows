# Test Plan

Covers every behavior the mod implements or intends to implement. Items marked
*(not yet implemented)* exist in the design but are missing code or data. Items
marked *(needs art)* will show placeholder visuals until sprites land.

---

## 1. Animal Registration

- [ ] Both animals make the standard cow sound
- [ ] Adult Strawberry Cow and Chocolate Cow display correctly
- [ ] You cannot buy Strawberry Cow or Chocolate Cow from Marnie
- [ ] Strawberry Cow can become pregnant in a Deluxe Barn
- [ ] Chocolate Cow can become pregnant in a Deluxe Barn
- [ ] Calves born from Strawberry and Chocolate Cows are normal White Cows (`BirthType: "White Cow"`), not Strawberry or Chocolate Cows
- [ ] A calf born from a transformed Brown Cow is a Brown Cow
- [ ] A calf born from a transformed White Cow is a White Cow

---

## 2. Milk Production

- [ ] Strawberry Cow produces **Strawberry Milk** when milked with the Milk Pail
- [ ] Chocolate Cow produces **Chocolate Milk** when milked with the Milk Pail
- [ ] Regular milk is produced every day (DaysToProduce = 1)
- [ ] **Large Strawberry Milk** drops at high friendship / luck / Coopmaster profession (verify deluxe path actually fires — see code TODO)
- [ ] **Large Chocolate Milk** drops under the same conditions
- [ ] Milking a freshly transformed cow produces the *new* type's milk, not the old type's (produce is swapped to the new type on transformation if pending, or cleared if already milked)
- [ ] Milking a freshly transformed cow works the first day (when the cow had pending produce at transformation time)
- [ ] Transforming a cow that has already been milked today does not grant a second milking — the new type's milk appears tomorrow instead
- [ ] White → Strawberry (unmilked) → Reversal Tea → milk pail yields vanilla milk same day (produce swapped back); White → Strawberry → milk → Reversal Tea → no milk today (exploit closed)
- [ ] Strawberry Milk and Large Strawberry Milk appear in the shipping collection when first shipped
- [ ] Chocolate Milk and Large Chocolate Milk appear in the shipping collection when first shipped

---

## 3. Transformation Items — Registration

- [ ] **Strawberry Transformation Tea** appears as a valid item (name, description, icon)
- [ ] **Chocolate Transformation Tea** appears as a valid item
- [ ] **Reversal Tea** appears as a valid item
- [ ] All three teas show sell price correctly (Strawberry 80g, Chocolate 80g, Reversal 60g)
- [ ] All three teas are inedible (eating attempt is blocked or gives -300 edibility penalty)
- [ ] All three teas cannot be gifted to NPCs
- [ ] All three teas can be shipped (ExcludeFromShippingCollection = false) and appear in the shipping collection under Artisan Goods (Category -26), not Cooking

---

## 4. Crafting Recipes — Tea

- [ ] None of the three tea recipes appear in the crafting menu before Marnie's letter has been received
- [ ] All three tea recipes appear in the crafting menu after Marnie's letter is opened
- [ ] Strawberry Transformation Tea crafts from **2× Tea Leaves + 1× Strawberry**
- [ ] Chocolate Transformation Tea crafts from **2× Tea Leaves + 1× Coffee Bean**
- [ ] Reversal Tea crafts from **2× Tea Leaves + 1× Quartz**
- [ ] Crafting any tea consumes the correct ingredients and produces exactly 1 tea

---

## 5. Transformation Mechanic — Happy Path

- [ ] Holding **Strawberry Transformation Tea** and right-clicking (or pressing action button) on an adult **White Cow** transforms it into a Strawberry Cow
- [ ] Holding **Strawberry Transformation Tea** and right-clicking an adult **Brown Cow** transforms it into a Strawberry Cow
- [ ] Holding **Chocolate Transformation Tea** and right-clicking an adult **White Cow** transforms it into a Chocolate Cow
- [ ] Holding **Chocolate Transformation Tea** and right-clicking an adult **Brown Cow** transforms it into a Chocolate Cow
- [ ] The cow's sprite updates immediately after transformation
- [ ] The original cow type is stored and can be recalled for reversal
- [ ] One tea is consumed from the stack on success
- [ ] A HUD success message names the cow and its new type

---

## 6. Transformation Mechanic — Reversal

- [ ] Holding **Reversal Tea** and right-clicking a Strawberry Cow reverts it to its stored original type
- [ ] Holding **Reversal Tea** and right-clicking a Chocolate Cow reverts it to its stored original type
- [ ] A cow transformed twice (e.g. White → Strawberry → Chocolate) reverts to **White** (original type stored at first transformation, not overwritten on second)
- [ ] A reverted cow's sprite updates immediately
- [ ] One Reversal Tea is consumed on success
- [ ] A HUD success message confirms the reversion

---

## 7. Transformation Mechanic — Error Cases

- [ ] Using any tea on a **baby cow** shows an error message and does not transform
- [ ] Using **Strawberry Tea** on a **Strawberry Cow** (already that type) shows an error and does not consume the item
- [ ] Using **Chocolate Tea** on a **Chocolate Cow** shows an error and does not consume the item
- [ ] Using **Reversal Tea** on a **White Cow** or **Brown Cow** (not transformed) shows an error and does not consume the item
- [ ] Using any tea with **no cow within range** does nothing (no message, no item consumed)
- [ ] Using any tea on a non-cow animal (chicken, goat, etc.) does nothing
- [ ] The action button during an **open menu** does not trigger transformation (`Game1.activeClickableMenu != null`) *(code TODO — not yet guarded)*
- [ ] The action button during a **cutscene or event** does not trigger transformation (`Game1.CurrentEvent != null`) *(code TODO — not yet guarded)*

---

## 8. Transformation Mechanic — Targeting

- [ ] Right-clicking on a cow's tile selects that cow and not an adjacent animal
- [ ] With two cows adjacent, the correct one is targeted (the one the cursor is on)
- [ ] Targeting works correctly with a **controller** (controller cursor tile vs. mouse cursor tile) *(flagged as needing controller test before release)*

---

## 9. Cooking Recipes — Unlock

All three cooking recipes (Strawberry Ice Cream, Chocolate Ice Cream, Hot Chocolate)
are taught by a single letter from Demetrius (`SpecialCows_DemetriusCooking`),
triggered the morning after the player first collects any special milk.

- [ ] Demetrius's letter arrives the morning after the player first collects Strawberry Milk
- [ ] Demetrius's letter arrives the morning after the player first collects Chocolate Milk
- [ ] Picking up Large Strawberry Milk or Large Chocolate Milk also satisfies the trigger (any of the four special milks counts)
- [ ] Demetrius's letter does not arrive twice (already-received check via `mailReceived`)
- [ ] Opening the letter teaches Strawberry Ice Cream, Chocolate Ice Cream, and Hot Chocolate
- [ ] On a save where the letter was received but a recipe is missing (legacy bug recovery), the `SaveLoaded` self-heal in `ModEntry` grants the missing recipes

---

## 10. Cooking Recipes — Functionality

- [ ] Strawberry Ice Cream can be cooked at the kitchen with **1× Strawberry Milk, 1x Sugar**
- [ ] Chocolate Ice Cream can be cooked at the kitchen with **1× Chocolate Milk, 1x Sugar**
- [ ] Hot Chocolate can be cooked at the kitchen with **1× Chocolate Milk**
- [ ] Each recipe produces exactly the correct output item
- [ ] Cooked items appear in the Cooking tab of the Collections menu

---

## 11. Cooked Items — Stats and Buffs

- [ ] **Strawberry Ice Cream** restores **100 Energy** and **45 Health** (edibility 40)
- [ ] Strawberry Ice Cream applies **+1 Luck** buff on consumption
- [ ] Strawberry Ice Cream buff lasts the intended duration (Duration: 600, matches Spicy Eel)
- [ ] Strawberry Ice Cream sells for **175g**
- [ ] **Chocolate Ice Cream** restores **100 Energy** and **45 Health** (edibility 40)
- [ ] Chocolate Ice Cream applies **+1 Mining** buff on consumption
- [ ] Chocolate Ice Cream buff lasts the intended duration (Duration: 600)
- [ ] Chocolate Ice Cream sells for **175g**
- [ ] **Hot Chocolate** restores ~**80 Energy** and ~**36 Health** (edibility 32)
- [ ] Hot Chocolate applies **+1 Mining** and **+1 Defense** buffs on consumption
- [ ] Hot Chocolate buff lasts the intended duration (Duration: 480)
- [ ] Hot Chocolate plays the drink animation (IsDrink = true), not the eat animation
- [ ] Hot Chocolate does not apply any Tipsy / alcohol debuff
- [ ] Hot Chocolate sells for **185g**
- [ ] All three cooked items appear in the shipping collection when first shipped

---

## 12. Mail Letters — Content

- [ ] Marnie's letter reads in Marnie's voice and correctly describes the tea mechanic
- [ ] Demetrius's letter reads in Demetrius's voice and correctly hints at his fondness for ice cream
- [ ] Recipe grant tokens in both letters use the exact recipe name strings that match the entries in `Data/CookingRecipes` (mismatch will silently fail to teach the recipe)
- [ ] Recipe grant tokens in Marnie's letter use the qualified item IDs for crafting recipes (`{{ModId}}_StrawberryTransformationTea`, etc.)

---

## 13. Regression — Vanilla Behavior

- [ ] White Cow and Brown Cow still produce vanilla milk normally
- [ ] Existing farm animals are unaffected on save load after installing the mod
- [ ] No errors or warnings in the SMAPI console on a clean game start
- [ ] No errors in the SMAPI console when entering and leaving the barn
- [ ] Shipping screen and end-of-day summary function normally
- [ ] Removing the mod does not corrupt saves (confirm graceful degradation)

### 14. Cheese Press

The special milks intentionally lack the `cow_milk_item` and `large_milk_item`
context tags so the press refuses them.

- [ ] Cheese Press refuses Strawberry Milk (does not start processing, no input animation)
- [ ] Cheese Press refuses Large Strawberry Milk
- [ ] Cheese Press refuses Chocolate Milk
- [ ] Cheese Press refuses Large Chocolate Milk
- [ ] Cheese Press still accepts vanilla Milk, Large Milk, Goat Milk, and Large Goat Milk normally
