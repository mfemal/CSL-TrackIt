namespace TrackIt.API
{
    /// <summary>
    /// Track the destination type based on the source and destination of the transfer.
    /// </summary>
    public enum ResourceDestinationType
    {
        // Two entities within the same city
        Local,

        // Same semantics as Outside Connections
        Import,
        Export
    }

}
