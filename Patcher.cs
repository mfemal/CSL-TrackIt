using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using ColossalFramework;

namespace TrackIt
{
    // Class and methods must be static, uses the CitiesHarmony API (boformer)
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
    /// Overrides PlaneAI which does not call its parent AircraftAI class. This tracking is done after cargo is unloaded.
    /// This method may be called multiple times while a plane is on the taxiway to unload. Upon arrival, the cargo
    /// unloaded event is tracked.
    /// </summary>
    [HarmonyPatch(typeof(CargoPlaneAI), nameof(CargoPlaneAI.ArriveAtDestination))]
    [HarmonyPatch(
        new Type[] { typeof(ushort), typeof(Vehicle) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
    public static class CargoPlaneAIArriveAtDestinationPatch
    {
        public static void Prefix(ushort vehicleID, ref Vehicle vehicleData)
        {
            if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != 0 || // returning planes can be ignored
                (vehicleData.m_flags & Vehicle.Flags.WaitingTarget) != 0 || // still waiting for the target to not be busy
                (vehicleData.m_flags & Vehicle.Flags.WaitingLoading) != 0) // ensure plane has an available station
            {
                return;
            }

            // As long as the plane hasn't reached its destination yet, the target building hasn't switched and is set to an
            // airplane cargo station. Only planes importing cargo are tracked.
            ushort buildingID = vehicleData.m_targetBuilding;
            if (buildingID != 0 &&
                vehicleData.m_transferSize > 0 &&
                (vehicleData.m_flags & Vehicle.Flags.Importing) != 0 && // plane can switch from import to export in Postfix
                (vehicleData.m_sourceBuilding != vehicleData.m_targetBuilding))
            {
#if DEBUG_PLANE
                LogUtil.LogInfo($"CargoPlaneAI ArriveAtDestination Prefix vehicleID: {vehicleID}");
#endif
                DataManager.instance.TrackIt(new TravelDescriptor(vehicleID, TravelVehicleType.CargoPlane, TravelStatus.Arrival, buildingID));
            }
        }
    }

    /// <summary>
    /// Track outbound cargo resource transfers in CargoPlaneAI.
    /// </summary>
    [HarmonyPatch(typeof(CargoPlaneAI), nameof(CargoPlaneAI.SetTarget))]
    [HarmonyPatch(
        new Type[] { typeof(ushort), typeof(Vehicle), typeof(ushort) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public static class CargoPlaneAISetTargetPatch
    {
        private static ushort lastTargetBuildingID; // used in Postfix

        public static void Prefix(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            ushort sourceBuilding = data.m_sourceBuilding;
            BuildingInfo buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[sourceBuilding].Info;
            if (buildingInfo.m_buildingAI is OutsideConnectionAI && targetBuilding != data.m_targetBuilding)
            {
                lastTargetBuildingID = data.m_targetBuilding;
            }
        }

        /// <summary>
        /// Called by the game engine in <see cref="CargoPlaneAI.SimulationStep(ushort, Vehicle3, Vector3)" /> when cargo
        /// is transferred. A building must have Building.Flags.Active, the vehicle must not have Vehicle.Flags.GoingBack
        /// and the target building is not set already. Unfortunately, this method is also called from other locations so
        /// it needs to screen out other occurrences.
        /// </summary>
        /// <param name="vehicleID">Vehicle ID.</param>
        /// <param name="data">Vehicle data.</param>
        /// <param name="targetBuilding">Source building ID for the transfer.</param>
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            ushort sourceBuilding = data.m_sourceBuilding;
            BuildingInfo buildingInfo = Singleton<BuildingManager>.instance.m_buildings.m_buffer[sourceBuilding].Info;

            if (buildingInfo.m_buildingAI is OutsideConnectionAI)
            {
                if (lastTargetBuildingID == 0) // honor checks in Prefix
                {
                    return;
                }
                // Planes owned by external cities have external m_sourceBuilding IDs; so if a plane that previously did an import
                // is now returning, take the last target building from Prefix (before it is changed and not available in Postfix)
                // to use for the source building to track the transfer for.
                sourceBuilding = lastTargetBuildingID;
                lastTargetBuildingID = 0;

                if (Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID].m_firstCargo == 0) // Ensure there is cargo transferred
                {
                    return;
                }
            }

            if ((data.m_flags & Vehicle.Flags.Exporting) != 0 &&
                (data.m_flags & Vehicle.Flags.WaitingTarget) == 0 &&
                (targetBuilding != 0) &&
                (data.m_transferSize > 0) &&
                targetBuilding != sourceBuilding)
            {
#if DEBUG_PLANE
                LogUtil.LogInfo($"CargoPlaneAI SetTarget Postfix vehicleID: {vehicleID} sourceBuilding: {sourceBuilding} targetBuilding: {targetBuilding} size: {data.m_transferSize}");
#endif
                DataManager.instance.TrackIt(new TravelDescriptor(vehicleID, TravelVehicleType.CargoPlane, TravelStatus.Departure, sourceBuilding));
            }
        }
    }

    /// <summary>
    /// Patched source method overrides ShipAI and does not call base class. This tracking is done after cargo is unloaded at
    /// the harbor which may involve waiting for it to become free (method called multiple times for the same vehicle).
    /// </summary>
    [HarmonyPatch(typeof(CargoShipAI), nameof(CargoShipAI.ArriveAtDestination))]
    [HarmonyPatch(
        new Type[] { typeof(ushort), typeof(Vehicle) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
    public static class CargoShipAIArriveAtDestinationPatch
    {
        public static void Prefix(ushort vehicleID, ref Vehicle vehicleData)
        {
            if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != 0 || // returning ships can be ignored (normally despawned)
                (vehicleData.m_flags & Vehicle.Flags.WaitingTarget) != 0 || // still waiting for the harbor to not be busy
                (vehicleData.m_flags & Vehicle.Flags.WaitingLoading) != 0) // ensure ship is unloaded
            {
                return;
            }

            ushort buildingID = vehicleData.m_targetBuilding;
            if (buildingID != 0 && vehicleData.m_transferSize > 0)
            {
                DataManager.instance.TrackIt(new TravelDescriptor(vehicleID, TravelVehicleType.CargoShip, TravelStatus.Arrival, buildingID));
#if DEBUG_SHIP
                LogUtil.LogInfo($"CargoShipAI ArriveAtDestination Prefix vehicleID: {vehicleID} buildingID: {buildingID}");
#endif
            }
        }
    }

    /// <summary>
    /// Patched source method overrides TrainAI and does not call base class. This tracking is done after cargo is unloaded at
    /// the train station which may involve waiting for it to become free (method called multiple times for the same vehicle).
    /// </summary>
    [HarmonyPatch(typeof(CargoTrainAI), nameof(CargoTrainAI.ArriveAtDestination))]
    [HarmonyPatch(
        new Type[] { typeof(ushort), typeof(Vehicle) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref })]
    public static class CargoTrainAIArriveAtDestinationPatch
    {
        public static void Prefix(ushort vehicleID, ref Vehicle vehicleData)
        {
            if ((vehicleData.m_flags & Vehicle.Flags.GoingBack) != 0 || // returning trains can be ignored (normally despawned)
                (vehicleData.m_flags & Vehicle.Flags.WaitingTarget) != 0 || // still waiting for the train station to not be busy
                (vehicleData.m_flags & Vehicle.Flags.WaitingLoading) != 0) // ensure train is unloaded
            {
                return;
            }
            // As long as the plane hasn't reached its destination yet, the target building hasn't switched and is set to an
            // airplane cargo station. Only planes importing cargo are tracked.
            ushort buildingID = vehicleData.m_targetBuilding;
            if (buildingID != 0 && vehicleData.m_transferSize > 0)
            {
                DataManager.instance.TrackIt(new TravelDescriptor(vehicleID, TravelVehicleType.CargoTrain, TravelStatus.Arrival, buildingID));
#if DEBUG_TRAIN
                LogUtil.LogInfo($"CargoTrainAI ArriveAtDestination Prefix vehicleID: {vehicleID} buildingID: {buildingID}");
#endif
            }
        }
    }

    /// <summary>
    /// Track outbound (sent) mail cargo resource transfers in PostVanAI. Currently,
    /// <see cref="PostVanAI.ChangeVehicleType(VehicleInfo, ushort, Vehicle, PathUnit.Position, uint)" />
    /// uses CargoTruckAI.ChangeVehicleType. So this patch handles transfers such as planes arriving
    /// that import mail along with any Post Office that may process mail.
    /// </summary>
    [HarmonyPatch(typeof(PostVanAI), nameof(PostVanAI.SetSource))]
    [HarmonyPatch(
        new Type[] { typeof(ushort), typeof(Vehicle), typeof(ushort) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public static class PostVanAISetSourcePatch
    {
        /// <summary>
        /// Called after the source is set by the game engine when cargo is transferred.
        /// </summary>
        /// <param name="vehicleID">Vehicle ID.</param>
        /// <param name="data">Vehicle data of the details of the transfer.</param>
        /// <param name="sourceBuilding">Source building ID for the transfer (i.e. Cargo Aircraft Stand, Post Office, etc.).</param>
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort sourceBuilding)
        {
            DataManager.instance.TrackIt(new CargoDescriptor(sourceBuilding,
                false,
                data.m_transferType,
                data.m_transferSize,
                data.m_flags));
#if DEBUG_TRUCK
            LogUtil.LogInfo($"PostVanAI SetSource Postfix vehicleID: {vehicleID} sourceBuilding: {sourceBuilding}");
#endif
        }
    }

    /// <summary>
    /// Track outbound (sent) truck cargo resource transfers in CargoTruckAI.
    /// </summary>
    [HarmonyPatch(typeof(CargoTruckAI), nameof(CargoTruckAI.SetSource))]
    [HarmonyPatch(
        new Type[] { typeof(ushort), typeof(Vehicle), typeof(ushort) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public static class CargoTruckAISetSourcePatch
    {
        /// <summary>
        /// Called after the source is set by the game engine when cargo is transferred.
        /// </summary>
        /// <param name="vehicleID">Vehicle ID.</param>
        /// <param name="data">Vehicle data of the details of the transfer.</param>
        /// <param name="sourceBuilding">Source building ID for the transfer (i.e. Cargo Aircraft Stand, Cargo Train Station, etc.).</param>
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort sourceBuilding)
        {
            DataManager.instance.TrackIt(new CargoDescriptor(sourceBuilding,
                false,
                data.m_transferType,
                data.m_transferSize,
                data.m_flags));
#if DEBUG_TRUCK
            LogUtil.LogInfo($"CargoTruckAI SetSource Postfix vehicleID: {vehicleID} sourceBuilding: {sourceBuilding}");
#endif
        }
    }

    /// <summary>
    /// Track inbound (received) cargo resource transfers in the CargoTruckAI.
    /// </summary>
    [HarmonyPatch(typeof(CargoTruckAI))]
    [HarmonyPatch(nameof(CargoTruckAI.ChangeVehicleType))]
    [HarmonyPatch(
        new Type[] { typeof(VehicleInfo), typeof(ushort), typeof(Vehicle), typeof(PathUnit.Position), typeof(uint)},
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
    public static class CargoTruckAIChangeVehicleTypePatch
    {
        // Custom state between Prefix and Postfix, must use static var (see: https://harmony.pardeike.net/articles/patching.html)
        private static CargoDescriptor? s_cargoDescriptor;

        /// <summary>
        /// The data for this transfer is tracked so it can be recorded in Postfix.
        /// </summary>
        /// <param name="vehicleInfo">Vehicle info reference.</param>
        /// <param name="vehicleID">Vehicle ID</param>
        /// <param name="vehicleData">Vehicle data</param>
        /// <param name="pathPos">Its position in the transfer path.</param>
        /// <param name="laneID">The lane ID.</param>
        public static void Prefix(ref VehicleInfo vehicleInfo, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            if ((vehicleData.m_flags & (Vehicle.Flags.TransferToSource | Vehicle.Flags.GoingBack)) == 0)
            {
                Vector3 vector = NetManager.instance.m_lanes.m_buffer[laneID].CalculatePosition(0.5f);
                NetInfo info = NetManager.instance.m_segments.m_buffer[pathPos.m_segment].Info;
                ushort buildingID = Singleton<BuildingManager>.instance.FindBuilding(vector,
                    100f, /* maxDistance */
                    info.m_class.m_service,
                    ItemClass.SubService.None,
                    Building.Flags.None, /* required */
                    Building.Flags.None); /* forbidden */
                s_cargoDescriptor = new CargoDescriptor(buildingID, true, vehicleData.m_transferType, vehicleData.m_transferSize, vehicleData.m_flags);
            }
        }

        /// <summary>
        /// Track the data transferred internally. This is done conditionally based on checks in Prefix if the cargo descriptor is set.
        /// </summary>
        /// <param name="vehicleInfo">Vehicle info reference.</param>
        /// <param name="vehicleID">Vehicle ID</param>
        /// <param name="vehicleData">Vehicle data</param>
        /// <param name="pathPos">Its position in the transfer path.</param>
        /// <param name="laneID">The lane ID.</param>
        public static void Postfix(ref VehicleInfo vehicleInfo, ushort vehicleID, ref Vehicle vehicleData, PathUnit.Position pathPos, uint laneID)
        {
            if (!s_cargoDescriptor.HasValue) // check if ignored (not set) in Prefix due to boundary conditions
            {
                return;
            }
            CargoDescriptor cargoDescriptor = s_cargoDescriptor.Value;
            DataManager.instance.TrackIt(cargoDescriptor);
#if DEBUG_TRUCK
            LogUtil.LogInfo($"CargoTruckAI ChangeVehicleType Postfix vehicleID: {vehicleID} sourceBuilding: {cargoDescriptor.BuildingID}");
#endif
            s_cargoDescriptor = null;
        }
    }
}
