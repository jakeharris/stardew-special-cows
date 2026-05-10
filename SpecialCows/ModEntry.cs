using System.Collections.Generic;
using System.Linq;
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

        private static readonly string[] CookingRecipeNames =
        {
            "Strawberry Ice Cream",
            "Chocolate Ice Cream",
            "Hot Chocolate",
        };

        private TransformationHandler _handler = null!;

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
            // Guard against the mail token silently failing to teach recipes (e.g. if the
            // token referenced the wrong key). If the letter was received, ensure all three
            // cooking recipes are known regardless of how they were (or weren't) granted.
            var player = Game1.player;
            if (!player.mailReceived.Contains(DemetriusMailId)) return;

            foreach (string recipe in CookingRecipeNames)
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

            _handler.TryTransform(Game1.player, Game1.currentLocation);
        }

        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            if (!Context.IsWorldReady || !e.IsLocalPlayer) return;
            if (HasOrWillReceiveMail(Game1.player, DemetriusMailId)) return;

            bool gotSpecialMilk = e.Added.Any(item => SpecialMilkIds.Contains(item.ItemId));
            if (gotSpecialMilk)
                Game1.player.mailForTomorrow.Add(DemetriusMailId);
        }
    }
}
