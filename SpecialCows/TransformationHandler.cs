using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace SpecialCows
{
    public class TransformationHandler
    {
        // ── Item IDs (unqualified — matches Item.ItemId in SDV 1.6) ──────────
        // These must match the keys registered in Data/Objects by the CP pack.
        // The CP pack's UniqueID is StrawberryMilkMafia.SpecialCows.CP, so
        // {{ModId}} in content patches resolves to that prefix.
        private const string StrawberryTeaId = "StrawberryMilkMafia.SpecialCows.CP_StrawberryTransformationTea";
        private const string ChocolateTeaId  = "StrawberryMilkMafia.SpecialCows.CP_ChocolateTransformationTea";
        private const string ReversalTeaId   = "StrawberryMilkMafia.SpecialCows.CP_ReversalTea";

        // ── Custom animal type strings (must match Data/FarmAnimals keys) ─────
        private const string StrawberryCowType = "Strawberry Cow";
        private const string ChocolateCowType  = "Chocolate Cow";

        // ── modData key for preserving the pre-transformation cow type ────────
        private const string OriginalTypeKey = "StrawberryMilkMafia.SpecialCows/OriginalType";

        // ── All FarmAnimal.type.Value strings this mod considers "a cow" ──────
        private static readonly HashSet<string> ValidCowTypes = new()
        {
            "White Cow", "Brown Cow", StrawberryCowType, ChocolateCowType
        };

        private readonly IMonitor monitor;

        public TransformationHandler(IMonitor monitor)
        {
            this.monitor = monitor;
        }

        // Returns true if the mod consumed the input (tea was held and the click was on a cow),
        // whether or not the transformation succeeded. Returns false on early-out paths where
        // no tea was held or no animal was at the cursor — the game's normal handler should run.
        public bool TryTransform(Farmer player, GameLocation location)
        {
            // ── Step 1: gate on active item being one of the three teas ───────
            string? teaId = player.ActiveItem?.ItemId;
            if (teaId != StrawberryTeaId &&
                teaId != ChocolateTeaId  &&
                teaId != ReversalTeaId)
                return false;

            // ── Step 2: find an animal within 1 tile of the cursor ────────────
            // GameLocation.Animals is on the base class (SDV 1.6), so this
            // works for both Farm (outdoor) and AnimalHouse (barn/coop interior)
            // without any location-type casting.
            FarmAnimal? animal = FindAnimalAtCursor(location, Game1.currentCursorTile);
            if (animal is null) return false;

            // ── Step 3: confirm the animal is a recognised cow type ───────────
            string currentType = animal.type.Value;
            if (!ValidCowTypes.Contains(currentType)) return false;

            // ── Step 4: confirm the cow is an adult ───────────────────────────
            // FarmAnimal.isAdult() checks age against the FarmAnimalData maturity
            // threshold, replacing the old ageWhenMature NetInt field (removed
            // in SDV 1.6).
            if (!animal.isAdult())
            {
                ShowError("This calf is too young to transform.");
                return true;
            }

            // ── Step 5: resolve target type, validate pairing ─────────────────
            bool isReversal = teaId == ReversalTeaId;
            string targetType;

            if (teaId == StrawberryTeaId)
            {
                if (currentType == StrawberryCowType)
                {
                    ShowError("This cow is already a strawberry cow.");
                    return true;
                }
                targetType = StrawberryCowType;
            }
            else if (teaId == ChocolateTeaId)
            {
                if (currentType == ChocolateCowType)
                {
                    ShowError("This cow is already a chocolate cow.");
                    return true;
                }
                targetType = ChocolateCowType;
            }
            else // ReversalTea
            {
                if (currentType != StrawberryCowType && currentType != ChocolateCowType)
                {
                    ShowError("This cow doesn't need to be restored.");
                    return true;
                }
                targetType = animal.modData.TryGetValue(OriginalTypeKey, out string? saved)
                    ? saved
                    : "White Cow";
            }

            // ── Step 6: perform the transformation ────────────────────────────
            // Capture before swapping type: null means already milked today.
            bool hadPendingProduce = animal.currentProduce.Value != null;

            if (!isReversal)
            {
                // Persist original type so Reversal Tea can restore it later.
                // Only write on first transformation — subsequent transforms must
                // not overwrite it, or a double-transformed cow reverts to the
                // intermediate type instead of its true origin.
                if (!animal.modData.ContainsKey(OriginalTypeKey))
                    animal.modData[OriginalTypeKey] = currentType;
            }
            else
            {
                animal.modData.Remove(OriginalTypeKey);
            }

            animal.type.Value = targetType;

            // If produce was pending, swap it to the new type's produce so the
            // player can milk once today for the new milk. If already milked,
            // leave null — dayUpdate tomorrow will roll the new type normally.
            // Keeping this conditional closes the tea→milk→reverse→milk exploit.
            if (hadPendingProduce)
            {
                string? newProduceId = animal.GetAnimalData()?.ProduceItemIds?.FirstOrDefault()?.ItemId;
                animal.currentProduce.Value = newProduceId;
            }
            else
            {
                animal.currentProduce.Value = null;
            }

            // ReloadTextureIfNeeded(forceReload: true) rebuilds the sprite from
            // the type string. The old reloadData() / reloadSprite() methods
            // were removed in SDV 1.6; ReloadTextureIfNeeded is the replacement.
            // TODO: if the new texture doesn't appear in-game, also call
            //   animal.reload(animal.homeInterior) to fully reinitialise data.
            animal.ReloadTextureIfNeeded(forceReload: true);

            // ── Step 7: consume one tea from the stack ────────────────────────
            player.reduceActiveItemByOne();

            // ── Step 8: show success HUD message ──────────────────────────────
            string cowName = string.IsNullOrWhiteSpace(animal.displayName)
                ? "Your cow"
                : animal.displayName;
            Game1.addHUDMessage(new HUDMessage(
                $"{cowName} has been transformed into a {targetType}!",
                HUDMessage.newQuest_type));

            monitor.Log($"Transformed '{cowName}' from {currentType} → {targetType}.", LogLevel.Debug);
            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the first FarmAnimal within 1 tile of <paramref name="cursorTile"/>,
        /// or null if none is found. Works for any GameLocation because
        /// GameLocation.Animals is on the base class in SDV 1.6.
        /// </summary>
        private static FarmAnimal? FindAnimalAtCursor(GameLocation location, Vector2 cursorTile)
        {
            foreach (FarmAnimal animal in location.Animals.Values)
            {
                if (IsWithinOneTile(animal.Tile, cursorTile))
                    return animal;
            }
            return null;
        }

        private static bool IsWithinOneTile(Vector2 animalTile, Vector2 cursorTile)
        {
            return Math.Abs(animalTile.X - cursorTile.X) <= 1f &&
                   Math.Abs(animalTile.Y - cursorTile.Y) <= 1f;
        }

        private static void ShowError(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));
        }
    }
}
