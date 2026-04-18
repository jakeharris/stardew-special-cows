using StardewModdingAPI;
using StardewValley;

namespace SpecialCows
{
    public class TransformationHandler
    {
        private readonly IMonitor monitor;

        public TransformationHandler(IMonitor monitor)
        {
            this.monitor = monitor;
        }

        public void TryTransform(Farmer player, GameLocation location)
        {
            // Step 1: Check that the player's active item is one of the three teas.
            // TODO: compare player.ActiveItem?.ItemId against the qualified item IDs for
            //   "(O)jakeharris.SpecialCows.CP_StrawberryTransformationTea",
            //   "(O)jakeharris.SpecialCows.CP_ChocolateTransformationTea",
            //   "(O)jakeharris.SpecialCows.CP_ReversalTea"
            //   and determine which transformation to perform.

            // Step 2: Find the cow tile under the cursor.
            // TODO: iterate location.Animals.Values, check each FarmAnimal's
            //   GetBoundingBox() against the tile at Game1.currentCursorTile.
            //   If no match, return early without consuming the item.

            // Step 3: Validate that the animal is an adult cow of the correct base type.
            // TODO: confirm animal.age.Value >= animal.ageWhenMature.Value
            //   and that animal.type.Value matches the expected source type:
            //   "Cow" for strawberry/chocolate teas, variant type for reversal tea.
            //   Show an informational HUD message and return early if validation fails.

            // Step 4: Swap the animal's type and reload its data.
            // TODO: set animal.type.Value = "<TargetType>" (e.g., "StrawberryCow")
            //   then call animal.reloadData() to apply the new animal definition and
            //   update its sprite sheet.

            // Step 5: Consume one tea from the player's inventory.
            // TODO: call player.reduceActiveItemByOne() to consume a single use.

            // Step 6: Display a HUD confirmation message.
            // TODO: Game1.addHUDMessage(new HUDMessage("...", HUDMessage.achievement_type));
        }
    }
}
