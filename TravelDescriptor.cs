namespace TrackIt
{
    internal enum TravelVehicleType
    {
        CargoPlane,
        CargoShip,
        CargoTrain
    }

    internal enum TravelStatus
    {
        Arrival,
        Departure
    }

    internal struct TravelDescriptor
    {
        internal ushort BuildingID
        {
            get;
            private set;
        }

        internal ushort VehicleID
        {
            get;
            private set;
        }

        internal TravelVehicleType TravelVehicleType
        {
            get;
            private set;
        }

        internal TravelStatus TravelStatus
        {
            get;
            private set;
        }

        internal TravelDescriptor(ushort vehicleID, TravelVehicleType travelVehicleType, TravelStatus travelStatus, ushort buildingID)
        {
            VehicleID = vehicleID;
            TravelVehicleType = travelVehicleType;
            TravelStatus = travelStatus;
            BuildingID = buildingID;
        }
    }
}
