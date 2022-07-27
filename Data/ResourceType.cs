namespace TrackIt.API
{
    /// <summary>
    /// Specific resource type extracted from the game reason.
    /// </summary>
    public enum ResourceType
    {
        // new DLC, changes in core game data structures, or simply not tracked
        None,

        // Oil
        Petrol,
        Oil,
        Coal,
        Plastics,
        Petroleum,

        // Forestry
        Logs,
        Lumber,
        PlanedTimber,
        Paper,

        // Agriculture
        Food,
        Grain,
        AnimalProducts,
        Flours,

        // Mail
        Mail,
        SortedMail,
        UnsortedMail,
        OutgoingMail,
        IncomingMail,

        // Ore
        Ore,
        Glass,
        Metals,

        // Goods
        Goods,
        LuxuryProducts,

        // Fish
        Fish
    }
}
