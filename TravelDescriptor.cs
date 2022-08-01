namespace TrackIt
{
    internal enum EntityType
    {
        CargoTrain,
        CargoShip
    }

    internal enum TravelStatus
    {
        Arrive,
        Depart
    }

    internal struct TravelDescriptor
    {
        internal ushort VehicleID
        {
            get;
            private set;
        }

        internal EntityType EntityType
        {
            get;
            private set;
        }

        internal TravelStatus TravelStatus
        {
            get;
            private set;
        }

        internal TravelDescriptor(ushort vehicleID, EntityType entityType, TravelStatus travelStatus)
        {
            VehicleID = vehicleID;
            EntityType = entityType;
            TravelStatus = travelStatus;
        }
    }
}
