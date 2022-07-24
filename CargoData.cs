using System;
using CargoInfoMod.Data;
using ColossalFramework.Plugins;
using ICities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

using TransferType = TransferManager.TransferReason;

namespace CargoInfoMod
{
    public struct CargoParcel
    {
        public ushort building;
        public ushort transferSize;
        public CarFlags flags;
        internal byte transferType;

        public int ResourceType => ((flags & CarFlags.Resource) - CarFlags.Oil) / 0x10;

        public static readonly CarFlags[] ResourceTypes =
        {
            CarFlags.Oil,
            CarFlags.Petrol,
            CarFlags.Ore,
            CarFlags.Coal,
            CarFlags.Logs,
            CarFlags.Lumber,
            CarFlags.Grain,
            CarFlags.Food,
            CarFlags.Goods,
            CarFlags.Mail,
            CarFlags.UnsortedMail,
            CarFlags.SortedMail,
            CarFlags.OutgoingMail,
            CarFlags.IncomingMail,
            CarFlags.AnimalProducts,
            CarFlags.Flours,
            CarFlags.Paper,
            CarFlags.PlanedTimber,
            CarFlags.Petroleum,
            CarFlags.Plastics,
            CarFlags.Glass,
            CarFlags.Metals,
            CarFlags.LuxuryProducts
       };

        public CargoParcel(ushort buildingID, bool incoming, byte transferType, ushort transferSize, Vehicle.Flags flags)
        {
            this.transferType = transferType;
            this.transferSize = transferSize;
            this.building = buildingID;
            this.flags = incoming ? CarFlags.None : CarFlags.Sent;

            if ((flags & Vehicle.Flags.Exporting) != 0)
                this.flags |= CarFlags.Exported;
            else if ((flags & Vehicle.Flags.Importing) != 0)
                this.flags |= CarFlags.Imported;

            switch ((TransferType)transferType)
            {
                case TransferType.Oil:
                    this.flags |= CarFlags.Oil;
                    break;
                case TransferType.Ore:
                    this.flags |= CarFlags.Ore;
                    break;
                case TransferType.Logs:
                    this.flags |= CarFlags.Logs;
                    break;
                case TransferType.Grain:
                    this.flags |= CarFlags.Grain;
                    break;
                case TransferType.Petrol:
                    this.flags |= CarFlags.Petrol;
                    break;
                case TransferType.Coal:
                    this.flags |= CarFlags.Coal;
                    break;
                case TransferType.Lumber:
                    this.flags |= CarFlags.Lumber;
                    break;
                case TransferType.Food:
                    this.flags |= CarFlags.Food;
                    break;
                case TransferType.Goods:
                    this.flags |= CarFlags.Goods;
                    break;
                case TransferType.Mail:
                    this.flags |= CarFlags.Mail;
                    break;
                case TransferType.UnsortedMail:
                    this.flags |= CarFlags.UnsortedMail;
                    break;
                case TransferType.SortedMail:
                    this.flags |= CarFlags.SortedMail;
                    break;
                case TransferType.OutgoingMail:
                    this.flags |= CarFlags.OutgoingMail;
                    break;
                case TransferType.IncomingMail:
                    this.flags |= CarFlags.IncomingMail;
                    break;
                case TransferType.AnimalProducts:
                    this.flags |= CarFlags.AnimalProducts;
                    break;
                case TransferType.Flours:
                    this.flags |= CarFlags.Flours;
                    break;
                case TransferType.Paper:
                    this.flags |= CarFlags.Paper;
                    break;
                case TransferType.PlanedTimber:
                    this.flags |= CarFlags.PlanedTimber;
                    break;
                case TransferType.Petroleum:
                    this.flags |= CarFlags.Petroleum;
                    break;
                case TransferType.Plastics:
                    this.flags |= CarFlags.Plastics;
                    break;
                case TransferType.Glass:
                    this.flags |= CarFlags.Glass;
                    break;
                case TransferType.Metals:
                    this.flags |= CarFlags.Metals;
                    break;
                case TransferType.LuxuryProducts:
                    this.flags |= CarFlags.LuxuryProducts;
                    break;
                default:
                    string transferTypeName = Enum.GetName(typeof(TransferType), transferType);
                    LogUtil.LogError($"Unexpected transfer type: {transferTypeName}");
                    break;
            }
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

        public void UpdateCounters()
        {
            foreach (var pair in cargoStatIndex)
            {
                for (var i = 0; i < pair.Value.CarsCounted.Length; i++)
                {
                    var previ = (int)((CarFlags)i | CarFlags.Previous);
                    if (previ == i) continue;
                    pair.Value.CarsCounted[previ] = pair.Value.CarsCounted[i];
                    pair.Value.CarsCounted[i] = 0;
                }
            }
        }

        public bool TryGetEntry(ushort building, out CargoStats2 stats)
        {
            return cargoStatIndex.TryGetValue(building, out stats);
        }

        public void Count(CargoParcel cargo)
        {
            if (cargo.building == 0 ||
                !(BuildingManager.instance.m_buildings.m_buffer[cargo.building].Info.m_buildingAI is CargoStationAI))
                return;

            if (cargoStatIndex.TryGetValue(cargo.building, out CargoStats2 stats))
            {
                stats.CarsCounted[(int)cargo.flags] += cargo.transferSize;

                ResourceDestinationType resourceDestinationType =
                    (cargo.flags & CarFlags.Imported) != 0 ? ResourceDestinationType.Import :
                    (cargo.flags & CarFlags.Exported) != 0 ? ResourceDestinationType.Export :
                    ResourceDestinationType.Local;
                DateTime ts = SimulationManager.instance.m_currentGameTime.Date; // Ignore the time component
                if ((cargo.flags & CarFlags.Sent) != 0)
                {
                    stats.TrackResourceSent(ts, resourceDestinationType, convertTransferType(cargo.transferType), cargo.transferSize);
                }
                else
                {
                    stats.TrackResourceReceived(ts, resourceDestinationType, convertTransferType(cargo.transferType), cargo.transferSize);
                }
            }

#if DEBUG
            LogUtil.LogInfo($"Updated building: {cargo.building} stats: {{ {stats} }}");
#endif
        }

        /// <summary>
        /// Translate the game representation for the transfer type to the internal module version.
        /// </summary>
        /// <param name="transferType">The TransferManager.TransferReason byte value</param>
        /// <returns>Mod resource type to include 'None' if a valid cannot be determined from transferType</returns>
        private ResourceType convertTransferType(byte transferType)
        {
            ResourceType resourceType;

            switch ((TransferType)transferType)
            {
                case TransferType.Oil:
                    resourceType = ResourceType.Oil;
                    break;
                case TransferType.Ore:
                    resourceType = ResourceType.Ore;
                    break;
                case TransferType.Logs:
                    resourceType = ResourceType.Logs;
                    break;
                case TransferType.Grain:
                    resourceType = ResourceType.Grain;
                    break;
                case TransferType.Petrol:
                    resourceType = ResourceType.Petrol;
                    break;
                case TransferType.Coal:
                    resourceType = ResourceType.Coal;
                    break;
                case TransferType.Lumber:
                    resourceType = ResourceType.Lumber;
                    break;
                case TransferType.Food:
                    resourceType = ResourceType.Food;
                    break;
                case TransferType.Goods:
                    resourceType = ResourceType.Goods;
                    break;
                case TransferType.Mail:
                    resourceType = ResourceType.Mail;
                    break;
                case TransferType.UnsortedMail:
                    resourceType = ResourceType.UnsortedMail;
                    break;
                case TransferType.SortedMail:
                    resourceType = ResourceType.SortedMail;
                    break;
                case TransferType.OutgoingMail:
                    resourceType = ResourceType.OutgoingMail;
                    break;
                case TransferType.IncomingMail:
                    resourceType = ResourceType.IncomingMail;
                    break;
                case TransferType.AnimalProducts:
                    resourceType = ResourceType.AnimalProducts;
                    break;
                case TransferType.Flours:
                    resourceType = ResourceType.Flours;
                    break;
                case TransferType.Paper:
                    resourceType = ResourceType.Paper;
                    break;
                case TransferType.PlanedTimber:
                    resourceType = ResourceType.PlanedTimber;
                    break;
                case TransferType.Petroleum:
                    resourceType = ResourceType.Petroleum;
                    break;
                case TransferType.Plastics:
                    resourceType = ResourceType.Plastics;
                    break;
                case TransferType.Glass:
                    resourceType = ResourceType.Glass;
                    break;
                case TransferType.Metals:
                    resourceType = ResourceType.Metals;
                    break;
                case TransferType.LuxuryProducts:
                    resourceType = ResourceType.LuxuryProducts;
                    break;
                default:
                    string transferTypeName = Enum.GetName(typeof(TransferType), transferType);
                    LogUtil.LogWarning($"Unexpected transfer type: {transferTypeName}, cannot convert resource type.");
                    resourceType = ResourceType.None;
                    break;
            }
            return resourceType;
        }
    }
}
