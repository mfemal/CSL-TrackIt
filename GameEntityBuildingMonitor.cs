using ICities;

namespace CargoInfoMod
{
    /// <summary>
    /// Since a standard exists to monitor changes in the UI with buildings in-game, tap into it for monitoring the data
    /// associated with them held in internal data structures as changes occur (i.e. gamer adds a new building).
    /// </summary>
    public class GameEntityBuildingMonitor : BuildingExtensionBase
    {
        public override void OnBuildingCreated(ushort buildingID)
        {
            DataManager.instance.AddBuildingID(buildingID);
        }

        public override void OnBuildingReleased(ushort buildingID)
        {
            DataManager.instance.RemoveBuildingID(buildingID);
        }
    }
}
