using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace TrackIt
{
    // Class and methods must be static
    public static class Patcher
    {
        private const string _harmonyId = ModInfo.NamespacePrefix + ".Patcher.HarmonyId";
        private static bool s_patched = false;

        public static void PatchAll()
        {
            if (s_patched)
            {
                return;
            }

#if DEBUG
            LogUtil.LogInfo("Patching..");
#endif

            s_patched = true;
            Harmony harmony = new Harmony(_harmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void UnpatchAll()
        {
            if (!s_patched)
            {
                return;
            }

            var harmony = new Harmony(_harmonyId);
            harmony.UnpatchAll(_harmonyId);
            s_patched = false;
#if DEBUG
            LogUtil.LogInfo("Reverted...");
#endif
        }
    }

    /// <summary>
    /// Via Harmony, track outbound (sent) cargo resource transfers in the CargoTruckAI.
    /// </summary>
    [HarmonyPatch(typeof(CargoTruckAI), nameof(CargoTruckAI.SetSource))]
    public static class CargoTruckAISetSourcePatch
    {

        /// <summary>
        /// Called by the CitiesHarmony wrapper (boformer).
        /// </summary>
        /// <param name="vehicleID">Vehicle ID.</param>
        /// <param name="data">Vehicle data.</param>
        /// <param name="sourceBuilding">Source building ID for the transfer.</param>
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort sourceBuilding)
        {
            var parcel = new CargoDescriptor(sourceBuilding, false, data.m_transferType, data.m_transferSize, data.m_flags);
            DataManager.instance.TrackIt(parcel);
#if DEBUG
            LogUtil.LogInfo($"SetSource Postfix vehicleID: {vehicleID} sourceBuilding: {sourceBuilding}");
#endif
        }
    }

    /// <summary>
    /// Via Harmony, track inbound (received) cargo resource transfers in the CargoTruckAI.
    /// </summary>
    [HarmonyPatch(typeof(CargoTruckAI))]
    [HarmonyPatch(nameof(CargoTruckAI.ChangeVehicleType))]
    [HarmonyPatch(
        new Type[] { typeof(VehicleInfo), typeof(ushort), typeof(Vehicle), typeof(PathUnit.Position), typeof(uint)},
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
    public static class CargoTruckAIChangeVehicleTypePatch
    {
        // Custom state between Prefix and Postfix, must use static var (see: https://harmony.pardeike.net/articles/patching.html)
        private static CargoDescriptor? s_cargoParcel;

        /// <summary>
        /// Called by the CitiesHarmony wrapper (boformer). The data for this transfer is tracked so it can be recorded in Postfix.
        /// </summary>
        /// <param name="vehicleInfo">Vehicle info reference.</param>
        /// <param name="vehicleID">Vehicle ID</param>
        /// <param name="vehicleData">Vehicle data</param>
        /// <param name="pathPos">Its position in the transfer path.</param>
        /// <param name="laneID">The lane ID.</param>
        public static void Prefix(ref VehicleInfo vehicleInfo, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            if ((vehicleData.m_flags & (Vehicle.Flags.TransferToSource | Vehicle.Flags.GoingBack)) != 0)
            {
                return;
            }

            Vector3 vector = NetManager.instance.m_lanes.m_buffer[laneID].CalculatePosition(0.5f);
            NetInfo info = NetManager.instance.m_segments.m_buffer[pathPos.m_segment].Info;
            ushort buildingID = BuildingManager.instance.FindBuilding(vector, 100f, info.m_class.m_service, ItemClass.SubService.None, Building.Flags.None, Building.Flags.None);

            s_cargoParcel = new CargoDescriptor(buildingID, true, vehicleData.m_transferType, vehicleData.m_transferSize, vehicleData.m_flags);
        }

        /// <summary>
        /// Track the data transferred internally. This is done conditionally based on checks in Prefix.
        /// </summary>
        /// <param name="vehicleInfo">Vehicle info reference.</param>
        /// <param name="vehicleID">Vehicle ID</param>
        /// <param name="vehicleData">Vehicle data</param>
        /// <param name="pathPos">Its position in the transfer path.</param>
        /// <param name="laneID">The lane ID.</param>
        public static void Postfix(ref VehicleInfo vehicleInfo, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            if (!s_cargoParcel.HasValue) // check if ignored (not set) in Prefix due to boundary conditions
            {
                return;
            }
            var parcel = (CargoDescriptor)s_cargoParcel;
            DataManager.instance.TrackIt(parcel);
#if DEBUG
            LogUtil.LogInfo($"ChangeVehicleType Postfix vehicleID: {vehicleID} sourceBuilding: {parcel.BuildingID}");
#endif
            s_cargoParcel = null;
        }
    }
}
