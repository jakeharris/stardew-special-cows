using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SpecialCows
{
    public class ModEntry : Mod
    {
        private const string DemetriusMailId = "StrawberryMilkMafia.SpecialCows_DemetriusCooking";
        private const string MarnieTeaMailId = "StrawberryMilkMafia.SpecialCows_MarnieTea";

        private static readonly HashSet<string> SpecialMilkIds = new()
        {
            "StrawberryMilkMafia.SpecialCows.CP_StrawberryMilk",
            "StrawberryMilkMafia.SpecialCows.CP_LargeStrawberryMilk",
            "StrawberryMilkMafia.SpecialCows.CP_ChocolateMilk",
            "StrawberryMilkMafia.SpecialCows.CP_LargeChocolateMilk",
        };

        private TransformationHandler _handler = null!;

        public override void Entry(IModHelper helper)
        {
            // TODO: subscribe to GameLaunched here if integration with other mods is needed

            _handler = new TransformationHandler(this.Monitor);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            var player = Game1.player;
            if (player.mailReceived.Contains(MarnieTeaMailId)) return;

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
            if (Game1.player.mailReceived.Contains(DemetriusMailId)) return;

            bool gotSpecialMilk = e.Added.Any(item => SpecialMilkIds.Contains(item.ItemId));
            if (gotSpecialMilk)
                Game1.player.mailForTomorrow.Add(DemetriusMailId);
        }
    }
}
