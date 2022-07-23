using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using HarmonyLib;

namespace CargoInfoMod
{
    public static class Patcher
    {
        private const string HarmonyId = "yourname.CargoInfoMod";
        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched)
            {
                return;
            }

#if DEBUG
            Debug.Log("Patching..");
#endif

            patched = true;
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            patched = false;
#if DEBUG
            Debug.Log("Reverted...");
#endif
        }
    }

    [HarmonyPatch(typeof(CargoTruckAI), nameof(CargoTruckAI.SetSource))]
    public static class CargoTruckAISetSourcePatch
    {
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort sourceBuilding)
        {
            var parcel = new CargoParcel(sourceBuilding, false, data.m_transferType, data.m_transferSize, data.m_flags);
            CargoData.Instance.Count(parcel);
#if DEBUG
            Debug.Log(String.Format("SetSource Postfix vehicleID: {0} sourceBuilding {1}", vehicleID, sourceBuilding));
#endif
        }
    }

    [HarmonyPatch(typeof(CargoTruckAI))]
    [HarmonyPatch(nameof(CargoTruckAI.ChangeVehicleType))]
    [HarmonyPatch(
        new Type[] { typeof(VehicleInfo), typeof(ushort), typeof(Vehicle), typeof(PathUnit.Position), typeof(uint)},
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
    public static class CargoTruckAIChangeVehicleType
    // VehicleInfo vehicleInfo, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID
    {
        // Custom state between Prefix and Postfix, must use static var (see: https://harmony.pardeike.net/articles/patching.html)
        private static CargoParcel? __cargoParcel;

        public static void Prefix(ref VehicleInfo vehicleInfo, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            if ((vehicleData.m_flags & (Vehicle.Flags.TransferToSource | Vehicle.Flags.GoingBack)) != 0)
            {
                return;
            }

            Vector3 vector = NetManager.instance.m_lanes.m_buffer[laneID].CalculatePosition(0.5f);
            NetInfo info = NetManager.instance.m_segments.m_buffer[pathPos.m_segment].Info;
            ushort buildingID = BuildingManager.instance.FindBuilding(vector, 100f, info.m_class.m_service, ItemClass.SubService.None, Building.Flags.None, Building.Flags.None);

            __cargoParcel = new CargoParcel(buildingID, true, vehicleData.m_transferType, vehicleData.m_transferSize, vehicleData.m_flags);
        }

        public static void Postfix(ref VehicleInfo vehicleInfo, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            if (!CargoTruckAIChangeVehicleType.__cargoParcel.HasValue)
            {
                return;
            }
            CargoData.Instance.Count((CargoParcel)__cargoParcel);
            __cargoParcel = null;
#if DEBUG
            Debug.Log(String.Format("ChangeVehicleType Postfix"));
#endif
        }
    }
}
