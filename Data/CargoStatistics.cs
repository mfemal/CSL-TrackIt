using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackIt.API
{
    /// <summary>
    /// Specific object locks are used for each tracked type as the Simulation and UI thread may be concurrently
    /// reading and writing to the same underlying data structures so this avoids indeterminate runtime problems.
    /// Since most manipulation and presentation of tracked types is by either sent or received, the data is
    /// additionally segmented according in this manner to minimize locking and other concerns.
    /// </summary>
    [Serializable]
    public class CargoStatistics
    {
        /// <summary>
        /// List of resources sent by an entity.
        /// </summary>
        private List<TrackedResource> _resourcesSent = new List<TrackedResource>();

        /// <summary>
        /// List of resources received by an entity.
        /// </summary>
        private List<TrackedResource> _resourcesReceived = new List<TrackedResource>();

        public int CountResources()
        {
            return CountResourcesSent() + CountResourcesReceived();
        }

        public int CountResourcesReceived()
        {
            return _resourcesReceived.Count; // atomic for our needs
        }

        public int CountResourcesSent()
        {
            return _resourcesSent.Count; // atomic for our needs
        }

        public int ExpungeOlderThan(DateTime oldestAllowedDate)
        {
            int expunged = 0;
            lock (_resourcesSent)
            {
                expunged = _resourcesSent.Count;
                List<TrackedResource> filteredList = _resourcesSent
                     .Where(r => r.ts >= oldestAllowedDate)
                     .ToList();
                expunged -= filteredList.Count;
                _resourcesSent.Clear();
                _resourcesSent.AddRange(filteredList);
            }
            lock (_resourcesReceived)
            {
                expunged += _resourcesReceived.Count;
                List<TrackedResource> filteredList = _resourcesReceived
                    .Where(r => r.ts >= oldestAllowedDate)
                    .ToList();
                expunged -= filteredList.Count;
                _resourcesReceived.Clear();
                _resourcesReceived.AddRange(filteredList);
            }
            return expunged;
        }

        public int TotalResources()
        {
            return TotalResourcesSent() + TotalResourcesReceived();
        }

        public int TotalResourcesSent()
        {
            return createResourceSnapshot(ref _resourcesSent).Sum(t => t._amount);
        }

        public int TotalResourcesSent(ResourceCategoryType resourceCategoryType)
        {
            return createResourceSnapshot(ref _resourcesSent)
                .Where(t => t._resourceCategoryType == resourceCategoryType)
                .Sum(t => t._amount);
        }

        public int TotalResourcesSent(ResourceDestinationType resourceDestinationType)
        {
            return createResourceSnapshot(ref _resourcesSent)
                .Where(t => t._resourceDestinationType == resourceDestinationType)
                .Sum(t => t._amount);
        }

        public int TotalResourcesSent(ResourceCategoryType resourceCategoryType, ResourceDestinationType resourceDestinationType)
        {
            return createResourceSnapshot(ref _resourcesSent)
                .Where(t => t._resourceCategoryType == resourceCategoryType && t._resourceDestinationType == resourceDestinationType)
                .Sum(t => t._amount);
        }

        public int TotalResourcesReceived()
        {
            return createResourceSnapshot(ref _resourcesReceived).Sum(t => t._amount);
        }

        public int TotalResourcesReceived(ResourceCategoryType resourceCategoryType)
        {
            return createResourceSnapshot(ref _resourcesReceived)
                .Where(t => t._resourceCategoryType == resourceCategoryType)
                .Sum(t => t._amount);
        }

        public int TotalResourcesReceived(ResourceDestinationType resourceDestinationType)
        {
            return createResourceSnapshot(ref _resourcesReceived)
                .Where(t => t._resourceDestinationType == resourceDestinationType)
                .Sum(t => t._amount);
        }

        public int TotalResourcesReceived(ResourceCategoryType resourceCategoryType, ResourceDestinationType resourceDestinationType)
        {
            return createResourceSnapshot(ref _resourcesReceived)
                .Where(t => t._resourceCategoryType == resourceCategoryType && t._resourceDestinationType == resourceDestinationType)
                .Sum(t => t._amount);
        }

        /// <summary>
        /// Using the provided lock (o) for thread safety, create a copy of the provided tracked resource list. This should be used
        /// when lists are manipulated.
        /// </summary>
        /// <param name="o">Object lock, must be non-null.</param>
        /// <param name="list">Source list to create a snapshot of.</param>
        /// <returns>New list containing references to the copied tracked values.</returns>
        private IList<TrackedResource> createResourceSnapshot(ref List<TrackedResource> list)
        {
            if (list == null || list.Count == 0)
            {
                return new List<TrackedResource>(0);
            }

            List<TrackedResource> l = null;
            lock (list)
            {
                l = list.Where(r => r._resourceType != ResourceType.None).ToList();
            }
            return l;
        }

        public void TrackResourceSent(DateTime ts, ResourceDestinationType resourceDestinationType, ResourceType resourceType, int amount)
        {
            lock (_resourcesSent)
            {
                _resourcesSent.Add(new TrackedResource(ts, resourceDestinationType, resourceType, amount));
            }
        }

        public void TrackResourceReceived(DateTime ts, ResourceDestinationType resourceDestinationType, ResourceType resourceType, int amount)
        {
            lock (_resourcesReceived)
            {
                _resourcesReceived.Add(new TrackedResource(ts, resourceDestinationType, resourceType, amount));
            }
        }

        public override string ToString()
        {
            return new StringBuilder()
                .Append($"Count: {{ Sent {CountResourcesSent()}, Received {CountResourcesReceived()} }}, Totals: [")
                .Append("Local: (")
                .AppendFormat("Oil {0} {1}", TotalResourcesSent(ResourceCategoryType.Oil, ResourceDestinationType.Local),
                    TotalResourcesReceived(ResourceCategoryType.Oil, ResourceDestinationType.Local))
                .AppendFormat(", Forestry {0} {1}", TotalResourcesSent(ResourceCategoryType.Forestry, ResourceDestinationType.Local),
                    TotalResourcesReceived(ResourceCategoryType.Forestry, ResourceDestinationType.Local))
                .AppendFormat(", Agriculture {0} {1}", TotalResourcesSent(ResourceCategoryType.Agriculture, ResourceDestinationType.Local),
                    TotalResourcesReceived(ResourceCategoryType.Agriculture, ResourceDestinationType.Local))
                .AppendFormat(", Mail {0} {1}", TotalResourcesSent(ResourceCategoryType.Mail, ResourceDestinationType.Local),
                    TotalResourcesReceived(ResourceCategoryType.Mail, ResourceDestinationType.Local))
                .AppendFormat(", Ore {0} {1}", TotalResourcesSent(ResourceCategoryType.Ore, ResourceDestinationType.Local),
                    TotalResourcesReceived(ResourceCategoryType.Ore, ResourceDestinationType.Local))
                .AppendFormat(", Goods {0} {1}", TotalResourcesSent(ResourceCategoryType.Goods, ResourceDestinationType.Local),
                    TotalResourcesReceived(ResourceCategoryType.Goods, ResourceDestinationType.Local))
                .AppendFormat(", Fish {0} {1}", TotalResourcesSent(ResourceCategoryType.Fish, ResourceDestinationType.Local),
                    TotalResourcesReceived(ResourceCategoryType.Fish, ResourceDestinationType.Local))
                .Append("), Import: (")
                .AppendFormat("Oil {0} {1}", TotalResourcesSent(ResourceCategoryType.Oil, ResourceDestinationType.Import),
                    TotalResourcesReceived(ResourceCategoryType.Oil, ResourceDestinationType.Import))
                .AppendFormat(", Forestry {0} {1}", TotalResourcesSent(ResourceCategoryType.Forestry, ResourceDestinationType.Import),
                    TotalResourcesReceived(ResourceCategoryType.Forestry, ResourceDestinationType.Import))
                .AppendFormat(", Agriculture {0} {1}", TotalResourcesSent(ResourceCategoryType.Agriculture, ResourceDestinationType.Import),
                    TotalResourcesReceived(ResourceCategoryType.Agriculture, ResourceDestinationType.Import))
                .AppendFormat(", Mail {0} {1}", TotalResourcesSent(ResourceCategoryType.Mail, ResourceDestinationType.Import),
                    TotalResourcesReceived(ResourceCategoryType.Mail, ResourceDestinationType.Import))
                .AppendFormat(", Ore {0} {1}", TotalResourcesSent(ResourceCategoryType.Ore, ResourceDestinationType.Import),
                    TotalResourcesReceived(ResourceCategoryType.Ore, ResourceDestinationType.Import))
                .AppendFormat(", Goods {0} {1}", TotalResourcesSent(ResourceCategoryType.Goods, ResourceDestinationType.Import),
                    TotalResourcesReceived(ResourceCategoryType.Goods, ResourceDestinationType.Import))
                .AppendFormat(", Fish {0} {1}", TotalResourcesSent(ResourceCategoryType.Fish, ResourceDestinationType.Import),
                    TotalResourcesReceived(ResourceCategoryType.Fish, ResourceDestinationType.Import))
                .Append("), Export: (")
                .AppendFormat("Oil {0} {1}", TotalResourcesSent(ResourceCategoryType.Oil, ResourceDestinationType.Export),
                    TotalResourcesReceived(ResourceCategoryType.Oil, ResourceDestinationType.Export))
                .AppendFormat(", Forestry {0} {1}", TotalResourcesSent(ResourceCategoryType.Forestry, ResourceDestinationType.Export),
                    TotalResourcesReceived(ResourceCategoryType.Forestry, ResourceDestinationType.Export))
                .AppendFormat(", Agriculture {0} {1}", TotalResourcesSent(ResourceCategoryType.Agriculture, ResourceDestinationType.Export),
                    TotalResourcesReceived(ResourceCategoryType.Agriculture, ResourceDestinationType.Export))
                .AppendFormat(", Mail {0} {1}", TotalResourcesSent(ResourceCategoryType.Mail, ResourceDestinationType.Export),
                    TotalResourcesReceived(ResourceCategoryType.Mail, ResourceDestinationType.Export))
                .AppendFormat(", Ore {0} {1}", TotalResourcesSent(ResourceCategoryType.Ore, ResourceDestinationType.Export),
                    TotalResourcesReceived(ResourceCategoryType.Ore, ResourceDestinationType.Export))
                .AppendFormat(", Goods {0} {1}", TotalResourcesSent(ResourceCategoryType.Goods, ResourceDestinationType.Export),
                    TotalResourcesReceived(ResourceCategoryType.Goods, ResourceDestinationType.Export))
                .AppendFormat(", Fish {0} {1}", TotalResourcesSent(ResourceCategoryType.Fish, ResourceDestinationType.Export),
                    TotalResourcesReceived(ResourceCategoryType.Fish, ResourceDestinationType.Export))
                .Append(")]")
                .ToString();
        }
    }
}
