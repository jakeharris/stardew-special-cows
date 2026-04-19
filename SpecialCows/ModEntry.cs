using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SpecialCows
{
    public class ModEntry : Mod
    {
        private TransformationHandler _handler = null!;

        public override void Entry(IModHelper helper)
        {
            // TODO: subscribe to GameLaunched here if integration with other mods is needed

            _handler = new TransformationHandler(this.Monitor);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // IsActionButton() covers right mouse button and the controller's
            // context-action button, matching how the player interacts with animals.
            if (!e.Button.IsActionButton()) return;

            _handler.TryTransform(Game1.player, Game1.currentLocation);
        }
    }
}
