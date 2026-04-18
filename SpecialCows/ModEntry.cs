using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SpecialCows
{
    public class ModEntry : Mod
    {
        private TransformationHandler? transformationHandler;

        public override void Entry(IModHelper helper)
        {
            // TODO: subscribe to GameLaunched here if integration with other mods is needed

            transformationHandler = new TransformationHandler(this.Monitor);
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.MouseRight || e.Button == SButton.ControllerA)
            {
                transformationHandler?.TryTransform(Game1.player, Game1.currentLocation);
            }
        }
    }
}
