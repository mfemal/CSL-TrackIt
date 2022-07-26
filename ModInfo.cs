using ICities;
using CitiesHarmony.API;

namespace TrackIt
{
    internal class Options
    {
        //internal SavedBool UseThousandthsUOM = new SavedBool("useThousandthsUOM", Settings.gameSettingsFile);
    }

    public class ModInfo : IUserMod
    {
        // Whether its a UI component or other named entity we need to find or use, its prefix should start with this.
        public const string NamespacePrefix = "TrackIt";

        public string Name => "Track It!";

        public string Description => "Track and display statistics for various game entities (i.e. Cargo Stations, Vehicles, etc.)";

        internal Options Options = new Options();

        public void OnSettingsUI(UIHelperBase helper)
        {
            //UIHelperBase trackingDisplayUnit = helper.AddGroup("Unit of Measure");
            //trackingDisplayUnit.AddCheckbox("Use thousandths", Options.UseThousandthsUOM, state => Options.UseThousandthsUOM.value = state);
        }

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patcher.UnpatchAll();
            }
        }
    }
}
