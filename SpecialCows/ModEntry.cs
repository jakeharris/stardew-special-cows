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
