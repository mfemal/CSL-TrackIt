using System;
using System.Collections.Generic;
using ColossalFramework;
using TrackIt.API;

namespace TrackIt
{
    internal class DataManager
    {
        // The key for this mod in the savegame
        public const string PersistenceId = ModInfo.NamespacePrefix + "DataManager";

        /// <summary>
        /// Event Handler to attach to for receiving changes associated with cargo transfers with buildings
        /// </summary>
        public MonitoredDataEventHandler CargoBuildingChanged;

        /// <summary>
        /// Event Handler to attach to for receiving changes associated with cargo transfers with vehicles
        /// </summary>
        public MonitoredDataEventHandler CargoVehicleChanged;

        /// <summary>
        /// Buildings that are tracked. This set is initialized from the prefabs and as a game runs, additions or
        /// removals are done appropriately.
        /// </summary>
        private static HashSet<int> _trackedBuildingSet;

        /// <summary>
        /// Index to the data associated with a building whose cargo is tracked.
        /// </summary>
        private static IDictionary<ushort, CargoStatistics> _trackedBuildingIndex;
        private static bool _initialized = false;

        /// <summary>
        /// Singleton for data tracking. The real work is done in 'TrackIt' methods as tracked data changes occur.
        /// </summary>
        private static readonly DataManager _instance = new DataManager();
        private DataManager() {
            _trackedBuildingSet = new HashSet<int>();
            _trackedBuildingIndex = new Dictionary<ushort, CargoStatistics>();
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
                    _trackedBuildingSet.Add(prefab.m_prefabDataIndex);
                }
            }
#if DEBUG
            LogUtil.LogInfo($"Found {_trackedBuildingSet.Count} building prefabs.");
#endif
            for (ushort i = 0; i < BuildingManager.instance.m_buildings.m_size; i++)
            {
                AddBuildingID(i);
            }
        }

        /// <summary>
        /// Track a cargo data transfer. If for some reason the building cannot be found internally, it is ignored.
        /// When buildings are added or removed within the game, this mod's listeners will normally update internal data
        /// structures appropriately. If the vehicle is set, an event is triggered for an listeners.
        /// </summary>
        /// <param name="cargoDescriptor">Descriptor for the data transferred.</param>
        internal void TrackIt(CargoDescriptor cargoDescriptor)
        {
            // Ignore empty transefers, some internal game mechanics seem to make not doing this more complex in this mod
            if (cargoDescriptor.BuildingID == 0 || cargoDescriptor.TransferSize == 0)
            {
                return;
            }
            ushort buildingID = cargoDescriptor.BuildingID;
            BuildingManager buildingManager = Singleton<BuildingManager>.instance;
            if (!(buildingManager.m_buildings.m_buffer[buildingID].Info.m_buildingAI is CargoStationAI))
            {
                return;
            }

            if (_trackedBuildingIndex.TryGetValue(cargoDescriptor.BuildingID, out CargoStatistics cargoStatistics))
            {
                DateTime ts = SimulationManager.instance.m_currentGameTime.Date; // Ignore the time component
                if (!cargoDescriptor.Incoming)
                {
                    cargoStatistics.TrackResourceSent(ts,
                        cargoDescriptor.ResourceDestinationType,
                        GameEntityDataExtractor.ConvertTransferType(cargoDescriptor.TransferType),
                        cargoDescriptor.TransferSize);
                }
                else
                {
                    cargoStatistics.TrackResourceReceived(ts,
                        cargoDescriptor.ResourceDestinationType,
                        GameEntityDataExtractor.ConvertTransferType(cargoDescriptor.TransferType),
                        cargoDescriptor.TransferSize);
                }
                OnCargoBuildingChanged(cargoDescriptor.BuildingID);
#if DEBUG
                LogUtil.LogInfo($"Tracked cargo buildingID: {cargoDescriptor.BuildingID} statistics: {{ {cargoStatistics} }}");
#endif
            }
        }

        /// <summary>
        /// Track vehicle travel stage changes.
        /// </summary>
        /// <param name="travelDescriptor">Metadata associated with the travel waypoint change.</param>
        internal void TrackIt(TravelDescriptor travelDescriptor)
        {
            if (travelDescriptor.EntityType == EntityType.CargoTrain || travelDescriptor.EntityType == EntityType.CargoShip)
            {
                OnCargoVehicleChanged(travelDescriptor.VehicleID);
            }
        }

        /// <summary>
        /// Get the cargo statistics associated with a building.
        /// </summary>
        /// <param name="buildingID">Source building, 0 is considered invalid and no cargoStatistics (null) is set.</param>
        /// <param name="cargoStatistics">The statistics associated with the build (if found in the index).</param>
        /// <returns>True if the lookup occurred successfully based on the buildings tracked.</returns>
        internal bool TryGetBuilding(ushort buildingID, out CargoStatistics cargoStatistics)
        {
            if (buildingID == 0)
            {
                cargoStatistics = null;
                return false;
            }
            return _trackedBuildingIndex.TryGetValue(buildingID, out cargoStatistics);
        }

        internal void AddBuildingID(ushort buildingID)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingID];
            if (_trackedBuildingSet.Contains(building.m_infoIndex) && !_trackedBuildingIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                // Restoring previous values of truck statistics
                _trackedBuildingIndex.Add(buildingID, new CargoStatistics());
#if DEBUG
                LogUtil.LogInfo($"Cargo station buildingID:{buildingID} buildingName:{buildingName} added to index");
#endif
            }
        }

        internal void RemoveBuildingID(ushort buildingID)
        {
            if (_trackedBuildingIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                _trackedBuildingIndex.Remove(buildingID);
#if DEBUG
                LogUtil.LogInfo($"Cargo station buildingID:{buildingID} buildingName:{buildingName} removed from index");
#endif
            }
        }

        internal void Clear()
        {
            _trackedBuildingSet.Clear();
            _trackedBuildingIndex.Clear();
 
            _initialized = false;
        }

        private void OnCargoBuildingChanged(ushort buildingID)
        {
            try
            {
                CargoBuildingChanged?.Invoke(new MonitoredDataChanged(buildingID));
            }
            catch (Exception e)
            {
                LogUtil.LogException(e);
            }
        }

        private void OnCargoVehicleChanged(ushort vehicleID)
        {
            try
            {
                CargoVehicleChanged?.Invoke(new MonitoredDataChanged(vehicleID));
            }
            catch (Exception e)
            {
                LogUtil.LogException(e);
            }
        }
    }
}
