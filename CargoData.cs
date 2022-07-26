using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using ColossalFramework.Plugins;
using ICities;
using CargoInfoMod.Data;

using TransferType = TransferManager.TransferReason;

namespace CargoInfoMod
{
    public struct CargoParcel
    {
        public ResourceDestinationType resourceDestinationType;
        public bool incoming;
        public ushort building;
        public ushort transferSize;
        internal byte transferType;

        public CargoParcel(ushort buildingID, bool incoming, byte transferType, ushort transferSize, Vehicle.Flags flags)
        {
            this.transferType = transferType;
            this.transferSize = transferSize;
            this.building = buildingID;
            this.incoming = incoming;

            resourceDestinationType = GameEntityDataExtractor.GetVehicleResourceDestinationType(flags);
        }
    }

    public class CargoData : SerializableDataExtensionBase
    {
        public static CargoData Instance;

        public const float TruckCapacity = 8000f;

        private ModInfo mod;

        private Dictionary<ushort, CargoStats2> cargoStatIndex;
        private HashSet<int> cargoStations;

        public CargoData()
        {
            cargoStations = new HashSet<int>();
            cargoStatIndex = new Dictionary<ushort, CargoStats2>();
            Instance = this;
        }

        public override void OnCreated(ISerializableData serializedData)
        {
            base.OnCreated(serializedData);
            mod = PluginManager.instance.FindPluginInfo(Assembly.GetExecutingAssembly()).userModInstance as ModInfo;
            if (LoadingManager.instance.m_loadingComplete)
            {
                OnLoadData();
                Setup();
            }
            if (mod != null)
            {
                mod.data = this;
            }
            else
                LogUtil.LogError("Could not find parent IUserMod!");
        }

        public override void OnLoadData()
        {
#if DEBUG
            LogUtil.LogInfo("Restoring previous data...");
#endif
            var data = serializableDataManager.LoadData(ModInfo.NamespaceV1);
            if (data == null)
            {
#if DEBUG
                LogUtil.LogInfo("No previous data found");
#endif
                return;
            }

            var ms = new MemoryStream(data);
            try
            {
                // Try deserialize older data format first
                var binaryFormatter = new BinaryFormatter();
                var indexData = binaryFormatter.Deserialize(ms);
                //if (indexData is Dictionary<ushort, CargoStats>)
                //{
                //    Debug.LogWarning("Loaded v1 data, upgrading...");
                //    cargoStatIndex = ((Dictionary<ushort, CargoStats>)indexData).ToDictionary(kv => kv.Key, kv => new CargoStats2
                //    {
                //        CarsCounted =
                //        {
                //            [(int) (CarFlags.Previous | CarFlags.Goods | CarFlags.Sent)] = kv.Value.carsSentLastTime * (int)TruckCapacity,
                //            [(int) (CarFlags.Previous | CarFlags.Goods)] = kv.Value.carsReceivedLastTime * (int)TruckCapacity
                //        }
                //    });
                //}
                //else if (indexData is Dictionary<ushort, CargoStats2>)
                if (indexData is Dictionary<ushort, CargoStats2>)
                {
#if DEBUG
                    LogUtil.LogInfo("Loaded v2 data");
#endif
                    cargoStatIndex = (Dictionary<ushort, CargoStats2>)indexData;
                }
#if DEBUG
                else
                    LogUtil.LogInfo("Unknown data format");
#endif
            }
            catch (SerializationException e)
            {
                LogUtil.LogException(e);
            }
#if DEBUG
            LogUtil.LogInfo($"Loaded stats for {cargoStatIndex.Count} stations");
#endif
        }

        public void Setup()
        {
#if DEBUG
            LogUtil.LogInfo("Looking up cargo station prefabs...");
#endif
            cargoStations.Clear();
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
                    LogUtil.LogInfo($"Cargo station prefab found: {prefab.name}");
#endif
                    cargoStations.Add(prefab.m_prefabDataIndex);
                }
            }
#if DEBUG
            LogUtil.LogInfo($"Found {cargoStations.Count} cargo station prefabs");
#endif
            for (ushort i = 0; i < BuildingManager.instance.m_buildings.m_size; i++)
            {
                AddBuilding(i);
            }

            BuildingManager.instance.EventBuildingCreated += AddBuilding;
            BuildingManager.instance.EventBuildingReleased += RemoveBuilding;
        }

        public override void OnSaveData()
        {
#if DEBUG
            LogUtil.LogInfo("Saving data...");
#endif
            if (ModInfo.CleanCurrentData) return;

            try
            {
                var ms = new MemoryStream();
                var binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(ms, cargoStatIndex);
                serializableDataManager.SaveData(ModInfo.NamespaceV1, ms.ToArray());
#if DEBUG
                LogUtil.LogInfo($"Saved stats for {cargoStatIndex.Count} stations");
#endif
            }
            catch (SerializationException e)
            {
                LogUtil.LogException(e);
            }
        }

        public void RemoveData(string dataID)
        {
            if (SimulationManager.instance.m_serializableDataStorage.ContainsKey(dataID))
            {
                SimulationManager.instance.m_SerializableDataWrapper.EraseData(dataID);
#if DEBUG
                LogUtil.LogInfo($"'{dataID}' data removed from savegame file");
#endif
            }
#if DEBUG
            else
                LogUtil.LogInfo($"'{dataID}' data not present in savegame file");
#endif
        }

        public void AddBuilding(ushort buildingID)
        {
            var building = BuildingManager.instance.m_buildings.m_buffer[buildingID];
            if (cargoStations.Contains(building.m_infoIndex) && !cargoStatIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                // Restoring previous values of truck statistics
                cargoStatIndex.Add(buildingID, new CargoStats2());
#if DEBUG
                LogUtil.LogInfo($"Cargo station buildingID:{buildingID} buildingName:{buildingName} added to index");
#endif
            }
        }

        public void RemoveBuilding(ushort buildingID)
        {
            if (cargoStatIndex.ContainsKey(buildingID))
            {
                var buildingName = BuildingManager.instance.GetBuildingName(buildingID, InstanceID.Empty);
                cargoStatIndex.Remove(buildingID);
#if DEBUG
                LogUtil.LogInfo($"Cargo station buildingID:{buildingID} buildingName:{buildingName} removed from index");
#endif
            }
        }

        public bool TryGetEntry(ushort building, out CargoStats2 stats)
        {
            return cargoStatIndex.TryGetValue(building, out stats);
        }

        /// <summary>
        /// Track the inbound cargo data transfer. If for some reason the building cannot be found internally, it is ignored.
        /// When buildings are added or removed within the game, this mod's listeners will normally update internal data
        /// structures appropriately.
        /// </summary>
        /// <param name="cargo">Cargo data transferred. If the building is not set (0), or no data is transferred it is ignored.</param>
        public void TrackIt(CargoParcel cargo)
        {
            if (cargo.building == 0 ||
                cargo.transferSize == 0 ||
                !(BuildingManager.instance.m_buildings.m_buffer[cargo.building].Info.m_buildingAI is CargoStationAI))
            {
                return;
            }

            if (cargoStatIndex.TryGetValue(cargo.building, out CargoStats2 stats))
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
    }
}
