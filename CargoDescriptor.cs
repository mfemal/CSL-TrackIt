using TrackIt.API;

namespace TrackIt
{
    /// <summary>
    /// Describes the summary of the contents of the cargo tracked. The details (i.e. bill of materials) are currently not tracked.
    /// </summary>
    public struct CargoDescriptor
    {
        public ResourceDestinationType resourceDestinationType;
        public bool incoming;
        public ushort building;
        public ushort transferSize;
        internal byte transferType;

        public CargoDescriptor(ushort buildingID, bool incoming, byte transferType, ushort transferSize, Vehicle.Flags flags)
        {
            this.transferType = transferType;
            this.transferSize = transferSize;
            this.building = buildingID;
            this.incoming = incoming;

            resourceDestinationType = GameEntityDataExtractor.GetVehicleResourceDestinationType(flags);
        }
    }
}
