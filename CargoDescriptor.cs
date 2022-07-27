using TrackIt.API;

namespace TrackIt
{
    /// <summary>
    /// Describes the summary of the contents of the cargo tracked. The details (i.e. bill of materials) are currently not tracked.
    /// </summary>
    internal struct CargoDescriptor
    {
        internal ResourceDestinationType ResourceDestinationType
        {
            get;
            private set;
        }

        internal bool Incoming
        {
            get;
            private set;
        }

        internal ushort BuildingID
        {
            get;
            private set;
        }

        internal ushort TransferSize
        {
            get;
            private set;
        }

        internal byte TransferType
        {
            get;
            private set;
        }

        internal CargoDescriptor(ushort buildingID, bool incoming, byte transferType, ushort transferSize, Vehicle.Flags flags)
        {
            TransferType = transferType;
            TransferSize = transferSize;
            BuildingID = buildingID;
            Incoming = incoming;
            ResourceDestinationType = GameEntityDataExtractor.GetVehicleResourceDestinationType(flags);
        }
    }
}
