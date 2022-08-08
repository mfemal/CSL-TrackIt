namespace TrackIt.API
{
    /// <summary>
    /// Replicate the categorization semantics available on 'Outside Connections' as close as possible.
    /// </summary>
    public enum ResourceCategoryType
    {
        // new DLC or changes in game core data structures? this MUST be the only non-valid special value vs.
        // actual resource categories in the game to currently avoid changing other code in this module.
        None,

        Oil,
        Forestry,
        Agriculture,
        Mail,
        Ore,
        Goods,
        Fish
    }
}
