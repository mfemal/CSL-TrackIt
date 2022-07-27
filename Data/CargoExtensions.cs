namespace TrackIt.API
{
    /// <summary>
    /// Various extensions used for cargo tracking.
    /// </summary>
    public static class CargoExtensions
    {
        /// <summary>
        /// Infer a category of data based on its resource type.
        /// </summary>
        /// <param name="resourceType">Resource type for conversion. These must be kept in-sync with the category options.</param>
        /// <returns>Resource category determined from resourceType, default set to 'None'.</returns>
        public static ResourceCategoryType InferResourceCategoryType(this ResourceType resourceType)
        {
            ResourceCategoryType resourceCategoryType;
            switch (resourceType)
            {
                case ResourceType.Petrol:
                case ResourceType.Petroleum:
                case ResourceType.Oil:
                case ResourceType.Plastics:
                    resourceCategoryType = ResourceCategoryType.Oil;
                    break;
                case ResourceType.Logs:
                case ResourceType.Lumber:
                case ResourceType.Paper:
                case ResourceType.PlanedTimber:
                    resourceCategoryType = ResourceCategoryType.Forestry;
                    break;
                case ResourceType.Grain:
                case ResourceType.Food:
                case ResourceType.AnimalProducts:
                case ResourceType.Flours:
                    resourceCategoryType = ResourceCategoryType.Agriculture;
                    break;
                case ResourceType.Goods:
                case ResourceType.LuxuryProducts:
                    resourceCategoryType = ResourceCategoryType.Goods;
                    break;
                case ResourceType.Mail:
                case ResourceType.UnsortedMail:
                case ResourceType.SortedMail:
                case ResourceType.OutgoingMail:
                case ResourceType.IncomingMail:
                    resourceCategoryType = ResourceCategoryType.Mail;
                    break;
                case ResourceType.Ore:
                case ResourceType.Glass:
                case ResourceType.Metals:
                    resourceCategoryType = ResourceCategoryType.Ore;
                    break;
                case ResourceType.Fish:
                    resourceCategoryType = ResourceCategoryType.Fish;
                    break;
                default:
                    resourceCategoryType = ResourceCategoryType.None;
                    break;
            }
            return resourceCategoryType;
        }
    }
}
