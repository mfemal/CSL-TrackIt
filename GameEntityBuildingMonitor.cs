using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICities;
using UnityEngine;

namespace CargoInfoMod
{
    /// <summary>
    /// Since a standard exists to monitor changes in the UI with buildings in-game, tap into it for monitoring the data
    /// associated with them held in internal data structures as changes occur (i.e. gamer adds a new building).
    /// </summary>
    internal class GameEntityBuildingMonitor : BuildingExtensionBase
    {
        public override void OnBuildingReleased(ushort id)
        {
            DataManager.instance.RemoveBuildingID(id);
        }

        public override void OnBuildingCreated(ushort id)
        {
            DataManager.instance.RemoveBuildingID(id);
        }
    }
}
