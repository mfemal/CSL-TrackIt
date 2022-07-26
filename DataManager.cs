using System;
using System.Collections.Generic;
using CargoInfoMod.Data;

namespace CargoInfoMod
{
    internal class DataManager
    {
        // The key for this mod in the savegame
        public const string PersistenceId = ModInfo.NamespacePrefix + "DataManager";

        private static readonly DataManager _instance = new DataManager();
        private static HashSet<int> _buildings;
        private static Dictionary<ushort, CargoStats2> _buildingsIndex;
        private static bool _initialized = false;

        static DataManager()
        {
            _buildings = new HashSet<int>();
            _buildingsIndex = new Dictionary<ushort, CargoStats2>();
        }

        internal static DataManager instance
        {
            get
            {
                return _instance;
            }
        }

        internal void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;

#if DEBUG
            LogUtil.LogInfo("Looking up building prefabs from BuildingInfo...");
#endif
            for (uint i = 0; i < PrefabCollection<BuildingInfo>.LoadedCount(); i++)
            {
                var prefab = PrefabCollection<BuildingInfo>.GetLoaded(i);
                if (prefab == null)
                {
                    LogUtil.LogWarning($"Uninitialized building prefab #{i}! (this should not happen)");
                    continue;
                }
                if (prefab.m_buildingAI is CargoStationAI)
                {
#if DEBUG
                    LogUtil.LogInfo($"Building prefab found: {prefab.name}, tracking enabled for it.");
#endif
                    _buildings.Add(prefab.m_prefabDataIndex);
                }
            }
#if DEBUG
            LogUtil.LogInfo($"Found {_buildings.Count} building prefabs.");
#endif
            for (ushort i = 0; i < BuildingManager.instance.m_buildings.m_size; i++)
            {
                AddBuildingID(i);
            }
        }

        /// <summary>
        /// Track the inbound cargo data transfer. If for some reason the building cannot be found internally, it is ignored.
        /// When buildings are added or removed within the game, this mod's listeners will normally update internal data
        /// structures appropriately.
        /// </summary>
        /// <param name="cargo">Cargo data transferred. If the building is not set (0), or no data is transferred it is ignored.</param>
        public void TrackIt(CargoDescriptor cargo)
        {
            if (cargo.building == 0 ||
                cargo.transferSize == 0 ||
                !(BuildingManager.instance.m_buildings.m_buffer[cargo.building].Info.m_buildingAI is CargoStationAI))
            {
                return;
            }

            if (_buildingsIndex.TryGetValue(cargo.building, out CargoStats2 stats))
            {
                DateTime ts = SimulationManager.instance.m_currentGameTime.Date; // Ignore the time component
                if (!cargo.incoming)
                {
                    stats.TrackResourceSent(ts, cargo.resourceDestinationType, GameEntityDataExtractor.ConvertTransferType(cargo.transferType), cargo.transferSize);
                }
                else
                {
                    stats.TrackResourceReceived(ts, cargo.resourceDestinationType, GameEntityDataExtractor.ConvertTransferType(cargo.transferType), cargo.transferSize);
                }
            }

#if DEBUG
            LogUtil.LogInfo($"Updated building: {cargo.building} stats: {{ {stats} }}");
#endif
        }

        internal bool TryGetEntry(ushort building, out CargoStats2 stats)
        {
            return _buildingsIndex.TryGetValue(building, out stats);
        }

        internal void AddBuildingID(ushort buildingID)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingID];
            if (_buildings.Contains(building.m_infoIndex) && !_buildingsIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                // Restoring previous values of truck statistics
                _buildingsIndex.Add(buildingID, new CargoStats2());
#if DEBUG
                LogUtil.LogInfo($"Cargo station buildingID:{buildingID} buildingName:{buildingName} added to index");
#endif
            }
        }

        internal void RemoveBuildingID(ushort buildingID)
        {
            if (_buildingsIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                _buildingsIndex.Remove(buildingID);
#if DEBUG
                LogUtil.LogInfo($"Cargo station buildingID:{buildingID} buildingName:{buildingName} removed from index");
#endif
            }
        }

        internal void Clear()
        {
            _buildings.Clear();
            _buildingsIndex.Clear();
 
            _initialized = false;
        }
    }
}
