using System;

namespace TrackIt.API
{
    /// <summary>
    /// In general, tracked resources should be considered immutable after creation, although some state may change during transit.
    /// </summary>
    public class TrackedResource
    {
        /// <summary>
        /// Game timestamp for a resource. This could change based on whether a resource is sent or received based on its state.
        /// </summary>
        public DateTime ts;

        internal int _amount;
        public int Amount
        {
            get
            {
                return _amount;
            }
        }

        internal ResourceType _resourceType;
        public ResourceType ResourceType
        {
            get
            {
                return _resourceType;
            }
        }

        internal ResourceCategoryType _resourceCategoryType;
        public ResourceCategoryType ResourceCategoryType
        {
            get
            {
                return _resourceCategoryType;
            }
        }

        internal ResourceDestinationType _resourceDestinationType;
        public ResourceDestinationType ResourceDestinationType
        {
            get
            {
                return _resourceDestinationType;
            }
        }

        /// <summary>
        /// Create an instance of a tracked cargo resource.
        /// </summary>
        /// <param name="ts">Current game timestamp for this resource.</param>
        /// <param name="resourceDestinationType">The destination type.</param>
        /// <param name="resourceType">Type of resource tracked.</param>
        /// <param name="amount">The amount of resource sent or received.</param>
        public TrackedResource(DateTime ts, ResourceDestinationType resourceDestinationType, ResourceType resourceType, int amount)
        {
            _resourceDestinationType = resourceDestinationType;
            _resourceType = resourceType;
            _amount = amount;
            _resourceCategoryType = resourceType.InferResourceCategoryType();
            this.ts = ts;
        }
    }
}
