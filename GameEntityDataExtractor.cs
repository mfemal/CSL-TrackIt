using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ColossalFramework;
using TrackIt.API;

using TransferType = TransferManager.TransferReason;

namespace TrackIt
{
    /// <summary>
    /// Various utlity methods for extracting relevant information from game objects.
    /// </summary>
    internal class GameEntityDataExtractor
    {
        /// <summary>
        /// Extract and group (based on ResourceCategoryType) the vehicle cargo.
        /// </summary>
        /// <param name="sourceVehicleID">The source vehicle id (i.e. selected from the UI) to obtain the cargo from. Must be a valid value.</param>
        /// <param name="leadingVehicleID">Leading vehicle ID (if found).</param>
        /// <returns>The grouped list of values (sum) based on ResourceCargoType.</returns>
        internal static IDictionary<ResourceCategoryType, uint> GetVehicleCargoBasicResourceTotals(ushort sourceVehicleID, out ushort leadingVehicleID)
        {
            IList<TrackedResource> cargoResourceList = GetVehicleCargoResources(sourceVehicleID, out leadingVehicleID);
            if (cargoResourceList.Count == 0)
            {
                return new Dictionary<ResourceCategoryType, uint>(0);
            }
            IList<ResourceCategoryType> resourceCategories = UIUtils.CargoBasicResourceGroups;
            IDictionary<ResourceCategoryType, uint> dict = new Dictionary<ResourceCategoryType, uint>();
            foreach (IGrouping<ResourceCategoryType, TrackedResource> resource in cargoResourceList.GroupBy(t => t.ResourceCategoryType))
            {
                dict.Add(resource.Key, (uint)resource.Sum(r => r.Amount));
            }
            return dict;
        }

        /// <summary>
        /// Extract all the resource details based on the lead vehicle. Currently, cyclic data is ignored (game framework assumed
        /// to provide good data).
        /// </summary>
        /// <param name="sourceVehicleID">The source vehicle id (i.e. selected from the UI) to obtain the cargo from. Must be a valid value.</param>
        /// <returns>Ordered list of tracked resources (starts with lead vehicle if appropriate - i.e. train). List may be empty but never null.</returns>
        internal static IList<TrackedResource> GetVehicleCargoResources(ushort sourceVehicleID, out ushort leadingVehicleID)
        {
            IList<TrackedResource> resourceList = null;
            VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
            leadingVehicleID = vehicleManager.m_vehicles.m_buffer[sourceVehicleID].GetFirstVehicle(sourceVehicleID);
            if (leadingVehicleID != 0)
            {
                Vehicle leadingVehicle = vehicleManager.m_vehicles.m_buffer[leadingVehicleID];
                VehicleAI vehicleAI = leadingVehicle.Info.m_vehicleAI;
                if (vehicleAI is CargoTrainAI || vehicleAI is CargoShipAI) // vehicleAI is CarTrailerAI || vehicleAI is CargoTruckAI
                {
                    resourceList = new List<TrackedResource>();
                    Vehicle cargoVehicle;
                    ushort c = leadingVehicle.m_firstCargo;
                    while (c != 0)
                    {
                        cargoVehicle = vehicleManager.m_vehicles.m_buffer[c];
                        resourceList.Add(new TrackedResource(ResourceDestinationType.Local,
                            ConvertTransferType(cargoVehicle.m_transferType),
                            cargoVehicle.m_transferSize));
                        c = DataManager.instance.IsVehicleIDInRange(cargoVehicle.m_nextCargo) ? cargoVehicle.m_nextCargo : (ushort)0;
                    }
                }
            }
            return resourceList ?? new List<TrackedResource>(0);
        }

        internal static bool IsLeadingVehicle(ushort vehicleID)
        {
            return DataManager.instance.IsVehicleIDInRange(vehicleID) ?
                Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID].m_leadingVehicle == 0 :
                false;
        }

        // TODO: enhance this, JSON? something printable
        internal static string Stringify(object o)
        {
            StringBuilder s = new StringBuilder();
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(o))
            {
                s.Append(descriptor.Name);
                s.Append(": ");
                s.Append(descriptor.GetValue(o));
            }
            return s.ToString();
        }

        internal static ResourceDestinationType GetVehicleResourceDestinationType(Vehicle.Flags flags)
        {
            return (flags & Vehicle.Flags.Exporting) != 0 ? ResourceDestinationType.Export :
                (flags & Vehicle.Flags.Importing) != 0 ? ResourceDestinationType.Import : ResourceDestinationType.Local;
        }

        /// <summary>
        /// Translate the game representation for the transfer type to the internal module version.
        /// </summary>
        /// <param name="transferType">The TransferManager.TransferReason byte value</param>
        /// <returns>Mod resource type to include 'None' if a valid cannot be determined from transferType</returns>
        internal static ResourceType ConvertTransferType(byte transferType)
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
                case TransferType.Fish:
                    resourceType = ResourceType.Fish;
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
