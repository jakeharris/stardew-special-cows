# Test Plan

Covers every behavior the mod implements or intends to implement. Items marked
*(not yet implemented)* exist in the design but are missing code or data. Items
marked *(needs art)* will show placeholder visuals until sprites land.

---

## 1. Animal Registration

- [ ] Both animals make the standard cow sound
- [ ] Adult Strawberry Cow and Chocolate Cow display correctly *(needs art — currently uses vanilla White Cow sprite)*
- [ ] You cannot buy Strawberry Cow or Chocolate Cow from Marnie
- [ ] Calves born from Strawberry and Chocolate Cows are normal cows, not Strawberry or Chocolate Cows

---

## 2. Milk Production

- [ ] Strawberry Cow produces **Strawberry Milk** when milked with the Milk Pail
- [ ] Chocolate Cow produces **Chocolate Milk** when milked with the Milk Pail
- [ ] Regular milk is produced every day (DaysToProduce = 1)
- [ ] **Large Strawberry Milk** drops at high friendship / luck / Coopmaster profession (verify deluxe path actually fires — see code TODO)
- [ ] **Large Chocolate Milk** drops under the same conditions
- [ ] Milking a freshly transformed cow produces the *new* type's milk, not the old type's (produce cleared on transformation)
- [ ] Milking a freshly transformed cow works the first day
- [ ] Strawberry Milk and Large Strawberry Milk appear in the shipping collection when first shipped
- [ ] Chocolate Milk and Large Chocolate Milk appear in the shipping collection when first shipped

---

## 3. Transformation Items — Registration

- [ ] **Strawberry Transformation Tea** appears as a valid item (name, description, icon) *(needs art)*
- [ ] **Chocolate Transformation Tea** appears as a valid item *(needs art)*
- [ ] **Reversal Tea** appears as a valid item *(needs art)*
- [ ] All three teas show sell price correctly (500g, 500g, 300g)
- [ ] All three teas are inedible (eating attempt is blocked or gives -300 edibility penalty)
- [ ] All three teas cannot be gifted to NPCs
- [ ] All three teas can be shipped (ExcludeFromShippingCollection = false) and appear in the shipping collection

---

## 4. Crafting Recipes — Tea

- [ ] Strawberry Transformation Tea recipe is visible in the crafting menu
- [ ] Strawberry Transformation Tea crafts from **2× Tea Leaves + 1× Strawberry**
- [ ] Chocolate Transformation Tea recipe is visible in the crafting menu
- [ ] Chocolate Transformation Tea crafts from **2× Tea Leaves + 1× Coffee Bean**
- [ ] Reversal Tea recipe is visible in the crafting menu
- [ ] Reversal Tea crafts from **2× Tea Leaves + 1× Quartz**
- [ ] Crafting any tea consumes the correct ingredients and produces exactly 1 tea

> **Gap:** There is currently no unlock trigger for the crafting recipes. They
> may appear in the crafting menu from day 1, which is probably unintended.
> Confirm desired unlock mechanism (mail? friendship level? skill level?) and
> implement before release.

---

## 5. Transformation Mechanic — Happy Path

- [ ] Holding **Strawberry Transformation Tea** and right-clicking (or pressing action button) on an adult **White Cow** transforms it into a Strawberry Cow
- [ ] Holding **Strawberry Transformation Tea** and right-clicking an adult **Brown Cow** transforms it into a Strawberry Cow
- [ ] Holding **Chocolate Transformation Tea** and right-clicking an adult **White Cow** transforms it into a Chocolate Cow
- [ ] Holding **Chocolate Transformation Tea** and right-clicking an adult **Brown Cow** transforms it into a Chocolate Cow
- [ ] The cow's sprite updates immediately after transformation *(needs art for correct sprite; currently reloads to White Cow placeholder)*
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

- [ ] **Strawberry Ice Cream** recipe is taught by Caroline's letter *(mail delivery not yet implemented — recipe cannot currently be unlocked in-game)*
- [ ] **Hot Chocolate** recipe is taught by Marnie's letter *(same)*
- [ ] Caroline's letter does not arrive until the player has **2♥ with Caroline AND 2♥ with Marnie** *(delivery not yet implemented)*
- [ ] Marnie's letter does not arrive until the same combined threshold is met
- [ ] Neither letter arrives twice (already-received check via `mailReceived`)

> **Gap:** **Chocolate Ice Cream** has no unlock mechanism. It is not mentioned
> in either mail letter. Decide how it is taught before release.

---

## 10. Cooking Recipes — Functionality

- [ ] Strawberry Ice Cream can be cooked at the kitchen with **1× Strawberry Milk**
- [ ] Chocolate Ice Cream can be cooked at the kitchen with **1× Chocolate Milk**
- [ ] Hot Chocolate can be cooked at the kitchen with **1× Chocolate Milk**
- [ ] Each recipe produces exactly the correct output item
- [ ] Cooked items appear in the Cooking tab of the Collections menu

---

## 11. Cooked Items — Stats and Buffs

- [ ] **Strawberry Ice Cream** restores **100 Energy** and **45 Health**
- [ ] Strawberry Ice Cream applies **+1 Luck** buff on consumption
- [ ] Strawberry Ice Cream buff lasts the intended duration (verify feels right in-game — duration unit unconfirmed, currently set to 600)
- [ ] Strawberry Ice Cream sells for **135g**
- [ ] **Chocolate Ice Cream** restores **100 Energy** and **45 Health**
- [ ] Chocolate Ice Cream applies **+1 Mining** buff on consumption
- [ ] Chocolate Ice Cream buff lasts the intended duration (600)
- [ ] Chocolate Ice Cream sells for **135g**
- [ ] **Hot Chocolate** restores **58 Energy** and **26 Health**
- [ ] Hot Chocolate applies **+1 Mining** and **+1 Defense** buffs on consumption
- [ ] Hot Chocolate buff lasts the intended duration (480)
- [ ] Hot Chocolate plays the drink animation (IsDrink = true), not the eat animation
- [ ] Hot Chocolate does not apply any Tipsy / alcohol debuff
- [ ] Hot Chocolate sells for **185g**
- [ ] All three cooked items appear in the shipping collection when first shipped

---

## 12. Mail Letters — Content

- [ ] Caroline's letter reads in Caroline's voice and correctly describes the tea mechanic *(copy is placeholder — needs writer pass before release)*
- [ ] Marnie's letter reads in Marnie's voice *(same)*
- [ ] Recipe grant tokens in both letters use the exact recipe name strings that match the entries in `Data/CookingRecipes` (mismatch will silently fail to teach the recipe)

---

## 13. Regression — Vanilla Behavior

- [ ] White Cow and Brown Cow still produce vanilla milk normally
- [ ] Existing farm animals are unaffected on save load after installing the mod
- [ ] No errors or warnings in the SMAPI console on a clean game start
- [ ] No errors in the SMAPI console when entering and leaving the barn
- [ ] Shipping screen and end-of-day summary function normally
- [ ] Removing the mod does not corrupt saves (confirm graceful degradation)
