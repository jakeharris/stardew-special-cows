using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;

namespace SpecialCows
{
    public class ModEntry : Mod
    {
        private const string DemetriusMailId = "StrawberryMilkMafia.SpecialCows_DemetriusCooking";
        private const string MarnieTeaMailId = "StrawberryMilkMafia.SpecialCows_MarnieTea";
        private const string OriginalTypeKey = "StrawberryMilkMafia.SpecialCows/OriginalType";

        private static readonly HashSet<string> SpecialCowTypes = new() { "Strawberry Cow", "Chocolate Cow" };

        private static readonly HashSet<string> SpecialMilkIds = new()
        {
            "StrawberryMilkMafia.SpecialCows.CP_StrawberryMilk",
            "StrawberryMilkMafia.SpecialCows.CP_LargeStrawberryMilk",
            "StrawberryMilkMafia.SpecialCows.CP_ChocolateMilk",
            "StrawberryMilkMafia.SpecialCows.CP_LargeChocolateMilk",
        };

        private const string StrawberryIceCreamRecipe = "StrawberryMilkMafia.SpecialCows.CP_StrawberryIceCream";
        private const string ChocolateIceCreamRecipe = "StrawberryMilkMafia.SpecialCows.CP_ChocolateIceCream";
        private const string HotChocolateRecipe = "StrawberryMilkMafia.SpecialCows.CP_HotChocolate";

        // Recipes granted by the Demetrius letter — the SaveLoaded self-heal makes sure
        // the player has them if the letter was received but the mail token failed to grant.
        private static readonly string[] DemetriusCookingRecipes =
        {
            StrawberryIceCreamRecipe,
            ChocolateIceCreamRecipe,
        };

        // Old display-name keys that pre-date the rename to qualified IDs. Migrated on
        // SaveLoaded so saves from earlier dev versions don't end up with orphan entries.
        private static readonly Dictionary<string, string> LegacyCookingRecipeRenames = new()
        {
            { "Strawberry Ice Cream", StrawberryIceCreamRecipe },
            { "Chocolate Ice Cream", ChocolateIceCreamRecipe },
            { "Hot Chocolate", HotChocolateRecipe },
        };

        private TransformationHandler _handler = null!;

        // True once we've queued or already delivered the Demetrius letter this session.
        // Guards OnInventoryChanged against rapid-fire duplicate additions to mailForTomorrow,
        // which would cause the letter to appear on multiple consecutive days.
        private bool _demetriusMailScheduled = false;

        public override void Entry(IModHelper helper)
        {
            // TODO: subscribe to GameLaunched here if integration with other mods is needed

            _handler = new TransformationHandler(this.Monitor);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;

            helper.ConsoleCommands.Add(
                "pregnate",
                "Force every adult cow on the farm to immediately give birth to a calf in its home barn. " +
                "Use this to verify that Strawberry/Chocolate Cow breeding produces the expected calf type.",
                this.OnPregnateCommand);

            helper.ConsoleCommands.Add(
                "learn_recipe",
                "Grant a cooking recipe by name to the current player. " +
                "Usage: learn_recipe \"Hot Chocolate\". Used for testing/recovery when a shop or mail grant fails.",
                this.OnLearnRecipeCommand);
        }

        private void OnLearnRecipeCommand(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                Monitor.Log("Load a save first.", LogLevel.Warn);
                return;
            }

            if (args.Length == 0)
            {
                Monitor.Log("Usage: learn_recipe <recipe name>  (quote multi-word names)", LogLevel.Warn);
                return;
            }

            string name = string.Join(" ", args);
            if (Game1.player.cookingRecipes.ContainsKey(name))
            {
                Monitor.Log($"Already know cooking recipe '{name}'.", LogLevel.Info);
                return;
            }

            Game1.player.cookingRecipes.Add(name, 0);
            Monitor.Log($"Granted cooking recipe '{name}'.", LogLevel.Info);
        }

        // SDV 1.6 has no animal pregnancy *state* — FarmAnimal.dayUpdate() rolls the dice fresh
        // each morning and, on success, instantiates a new FarmAnimal of the parent's type and
        // adds it to the home AnimalHouse. There's nothing we can flip on the parent to make
        // birth happen tomorrow; the closest we can get is to replicate the vanilla birth path
        // ourselves, right now, while the player is awake. That's exactly what we want for
        // testing — it exercises the same code shape ("which type does the calf become?") that
        // the real birth would.
        private void OnPregnateCommand(string command, string[] args)
        {
            if (!Context.IsWorldReady)
            {
                Monitor.Log("Load a save first.", LogLevel.Warn);
                return;
            }

            int count = 0;
            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building.GetIndoors() is not AnimalHouse house) continue;

                // Snapshot adult cows up front; we mutate `house.animals` inside the loop.
                var parents = house.animals.Values
                    .Where(a => a.isAdult() && a.type.Value.Contains("Cow"))
                    .ToList();

                foreach (FarmAnimal parent in parents)
                {
                    if (house.isFull())
                    {
                        Monitor.Log($"  {house.GetParentLocation()?.Name ?? "Barn"} is full; skipping {parent.Name}.", LogLevel.Warn);
                        continue;
                    }

                    long newId = Utility.RandomLong(Game1.random);
                    var baby = new FarmAnimal(ResolveCalfType(parent), newId, parent.ownerID.Value);
                    baby.parentId.Value = parent.myID.Value;
                    house.animals.Add(newId, baby);
                    house.animalsThatLiveHere.Add(newId);

                    Monitor.Log($"  {parent.Name} ({parent.type.Value}) → calf '{baby.Name}' ({baby.type.Value}, resolved from OriginalType)", LogLevel.Info);
                    count++;
                }
            }

            Monitor.Log(
                count > 0 ? $"Spawned {count} calf/calves. Sleep and wake to see them as adults, or check the barn now."
                          : "No adult cows found in any barn.",
                LogLevel.Info);
        }

        // Vanilla SDV creates calves with BirthType from Data/FarmAnimals, which is always
        // "White Cow" for special cows. That loses the parent's pre-transformation type.
        // We scan for age-0 animals (born this morning during dayUpdate) whose parent is a
        // special cow, and rewrite them to the parent's stored OriginalType.
        private void FixupNewbornCalves()
        {
            // Build a farm-wide ID → animal map so we can look up any parent.
            var allAnimals = new Dictionary<long, FarmAnimal>();
            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building.GetIndoors() is not AnimalHouse house) continue;
                foreach (FarmAnimal animal in house.animals.Values)
                    allAnimals[animal.myID.Value] = animal;
            }

            foreach (Building building in Game1.getFarm().buildings)
            {
                if (building.GetIndoors() is not AnimalHouse house) continue;
                foreach (FarmAnimal calf in house.animals.Values)
                {
                    if (calf.age.Value != 0) continue;
                    if (!allAnimals.TryGetValue(calf.parentId.Value, out FarmAnimal? parent)) continue;
                    if (!parent.modData.TryGetValue(OriginalTypeKey, out string? originalType)) continue;
                    if (!SpecialCowTypes.Contains(calf.type.Value)) continue;

                    Monitor.Log(
                        $"Fixing newborn calf '{calf.Name}': {calf.type.Value} → {originalType} " +
                        $"(parent '{parent.Name}' pre-transformation type)",
                        LogLevel.Debug);
                    calf.type.Value = originalType;
                    calf.ReloadTextureIfNeeded(forceReload: true);
                }
            }
        }

        // Resolves the correct calf type for a given parent:
        // - Transformed special cow → restores the pre-transformation (OriginalType) breed.
        // - Non-transformed special cow (shouldn't exist in normal play) → White Cow fallback.
        // - Vanilla cow → its own type (vanilla calves inherit parent type).
        private static string ResolveCalfType(FarmAnimal parent)
        {
            if (parent.modData.TryGetValue(OriginalTypeKey, out string? original))
                return original;
            if (SpecialCowTypes.Contains(parent.type.Value))
                return "White Cow";
            return parent.type.Value;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            var player = Game1.player;

            // Migrate legacy display-name recipe keys to their qualified-ID equivalents.
            // The Collections menu expects the recipe key to match the dish item's Name
            // field; older dev versions used display names, which left affected saves
            // showing a Torch fallback in Collections even when the Kitchen worked.
            foreach (var (oldName, newName) in LegacyCookingRecipeRenames)
            {
                if (!player.cookingRecipes.TryGetValue(oldName, out int timesCooked)) continue;
                player.cookingRecipes.Remove(oldName);
                if (!player.cookingRecipes.ContainsKey(newName))
                    player.cookingRecipes.Add(newName, timesCooked);
            }

            // Heal saves corrupted by the pre-fix duplicate-queuing bug. SDV's Letters
            // collection skips mailReceived entries that end with %, treating them as
            // invisible flags. Duplicate copies of the Demetrius letter racing through
            // mailForTomorrow could land as the % variant, making the letter unreadable
            // by the Collections menu. Strip the % variant and add the bare ID.
            string demetriusFlag = DemetriusMailId + "%";
            if (player.mailReceived.Contains(demetriusFlag) && !player.mailReceived.Contains(DemetriusMailId))
            {
                player.mailReceived.Remove(demetriusFlag);
                player.mailReceived.Add(DemetriusMailId);
                Monitor.Log("Migrated Demetrius mail flag to readable letter in mailReceived.", LogLevel.Debug);
            }

            // Broader fallback: if the player already has either ice cream recipe (proof
            // the letter was effectively received in some form) but the bare ID still
            // isn't in mailReceived, add it so the Letters collection can display it.
            if (!player.mailReceived.Contains(DemetriusMailId)
                && DemetriusCookingRecipes.Any(r => player.cookingRecipes.ContainsKey(r)))
            {
                player.mailReceived.Add(DemetriusMailId);
                Monitor.Log("Synthesised Demetrius mail entry in mailReceived from known recipes.", LogLevel.Debug);
            }

            // Restore the in-memory flag so OnInventoryChanged won't re-queue a letter
            // that's already been delivered or read in a prior session.
            _demetriusMailScheduled = HasOrWillReceiveMail(player, DemetriusMailId);

            // Guard against the mail token silently failing to teach recipes (e.g. if the
            // token referenced the wrong key). If the letter was received, ensure both
            // ice cream recipes are known regardless of how they were (or weren't) granted.
            if (!player.mailReceived.Contains(DemetriusMailId)) return;

            foreach (string recipe in DemetriusCookingRecipes)
            {
                if (!player.cookingRecipes.ContainsKey(recipe))
                    player.cookingRecipes.Add(recipe, 0);
            }
        }

        // mailReceived is only populated after the player opens the letter, so also check
        // mailbox (delivered but unread) and mailForTomorrow (queued but not yet delivered).
        private static bool HasOrWillReceiveMail(Farmer player, string mailId) =>
            player.mailReceived.Contains(mailId)
            || player.mailbox.Contains(mailId)
            || player.mailForTomorrow.Contains(mailId);

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            FixupNewbornCalves();

            var player = Game1.player;
            if (HasOrWillReceiveMail(player, MarnieTeaMailId)) return;

            bool carolineFriendship = player.getFriendshipHeartLevelForNPC("Caroline") >= 2;
            bool sunroomEventSeen = player.eventsSeen.Contains("719926");
            bool marnieFriendship = player.getFriendshipHeartLevelForNPC("Marnie") >= 2;

            if (carolineFriendship && sunroomEventSeen && marnieFriendship)
                player.mailForTomorrow.Add(MarnieTeaMailId);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // IsActionButton() covers right mouse button and the controller's
            // context-action button, matching how the player interacts with animals.
            if (!e.Button.IsActionButton()) return;

            // Don't run while a menu is open (e.g. cow status menu from a prior
            // interaction) or during a cutscene — the player can't meaningfully act.
            if (Game1.activeClickableMenu != null) return;
            if (Game1.CurrentEvent != null) return;

            // Controller: use the tile in front of the player.
            // Mouse/keyboard: use the cursor tile (where the player clicked).
            // Game1.currentCursorTile is a free-floating cursor on controller —
            // it only moves when the player explicitly repositions it with the
            // right stick, so it stays parked over the last interacted tile and
            // does not track the player's facing direction.
            Vector2 targetTile = e.Button.TryGetController(out _)
                ? GetFacingTile(Game1.player)
                : Game1.currentCursorTile;

            if (_handler.TryTransform(Game1.player, Game1.currentLocation, targetTile))
                this.Helper.Input.Suppress(e.Button);
        }

        private static Vector2 GetFacingTile(Farmer player)
        {
            var tile = player.Tile;
            return player.FacingDirection switch
            {
                0 => new Vector2(tile.X, tile.Y - 1), // up
                1 => new Vector2(tile.X + 1, tile.Y), // right
                2 => new Vector2(tile.X, tile.Y + 1), // down
                3 => new Vector2(tile.X - 1, tile.Y), // left
                _ => tile
            };
        }

        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !e.IsLocalPlayer) return;

            // _demetriusMailScheduled is the primary guard: it's set the moment we queue
            // the letter and restored from save state on load. This prevents rapid-fire
            // InventoryChanged calls from adding duplicate entries to mailForTomorrow,
            // which would cause the letter to appear on consecutive days.
            if (_demetriusMailScheduled) return;
            if (HasOrWillReceiveMail(Game1.player, DemetriusMailId))
            {
                _demetriusMailScheduled = true;
                return;
            }

            bool gotSpecialMilk = e.Added.Any(item => SpecialMilkIds.Contains(item.ItemId));
            if (gotSpecialMilk)
            {
                _demetriusMailScheduled = true;
                Game1.player.mailForTomorrow.Add(DemetriusMailId);
            }
        }
    }
}
