using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace CargoInfoMod.Data
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

    public class TrackedResource
    {
        /// <summary>
        /// Game timestamp for a resource. This could change based on whether a resource is sent or received based on its state.
        /// </summary>
        public DateTime ts;

        internal ResourceDestinationType resourceDestinationType;
        internal ResourceCategoryType resourceCategoryType;
        internal ResourceType resourceType;
        internal int amount;

        /// <summary>
        /// Create an instance of a tracked cargo resource.
        /// </summary>
        /// <param name="ts">Current game timestamp for this resource.</param>
        /// <param name="resourceDestinationType">The destination type.</param>
        /// <param name="resourceType">Type of resource tracked.</param>
        /// <param name="amount">The amount of resource sent or received.</param>
        public TrackedResource(DateTime ts, ResourceDestinationType resourceDestinationType, ResourceType resourceType, int amount)
        {
            this.resourceDestinationType = resourceDestinationType;
            this.resourceType = resourceType;
            this.amount = amount;
            this.ts = ts;

            resourceCategoryType = resourceType.InferResourceCategoryType();
        }
    }

    [Flags]
    public enum CarFlags
    {
        None                = 0x0000,
        Previous            = 0x0001,
        Sent                = 0x0002,
        Imported            = 0x0004,
        Exported            = 0x0008,

        Resource            = 0x01F0,

        Oil                 = 0x0000,
        Petrol              = 0x0010,
        Ore                 = 0x0020,
        Coal                = 0x0030,
        Logs                = 0x0040,
        Lumber              = 0x0050,
        Grain               = 0x0060,
        Food                = 0x0070,
        Goods               = 0x0080,
        Mail                = 0x0090,
        UnsortedMail        = 0x00A0,
        SortedMail          = 0x00B0,
        OutgoingMail        = 0x00C0,
        IncomingMail        = 0x00D0,
        AnimalProducts      = 0x00E0,
        Flours              = 0x00F0,
        Paper               = 0x0100,
        PlanedTimber        = 0x0110,
        Petroleum           = 0x0120,
        Plastics            = 0x0130,
        Glass               = 0x0140,
        Metals              = 0x0150,
        LuxuryProducts      = 0x0160
    }

    /// <summary>
    /// Specific object locks are used for each tracked type as the Simulation and UI thread may be concurrently
    /// reading and writing to the same underlying data structures so this avoids indeterminate runtime problems.
    /// Since most manipulation and presentation of tracked types is by either sent or received, the data is
    /// additionally segmented according in this manner to minimize locking and other concerns.
    /// </summary>
    [Serializable]
    public class CargoStats2
    {
        public int[] CarsCounted;

        /// <summary>
        /// List of resources sent by an entity.
        /// </summary>
        private List<TrackedResource> s_resourcesSent = new List<TrackedResource>();

        // Object lock for concurrent reading and writing to s_resourcesSent
        private object s_resourcesSentLock = new object();

        /// <summary>
        /// List of resources received by an entity.
        /// </summary>
        private List<TrackedResource> s_resourcesReceived = new List<TrackedResource>();

        // Object lock for concurrent reading and writing to s_resourcesReceived
        private object s_resourcesReceivedLock = new object();

        public CargoStats2()
        {
            CarsCounted = new int[(int)Enum.GetValues(typeof(CarFlags)).Cast<CarFlags>().Aggregate((v, agg) => agg | v) + 1];
        }

        public int GetTotalWhere(Func<CarFlags, bool> pred)
        {
            return CarsCounted.Where((t, idx) => pred((CarFlags)idx)).Sum();

        }

        public int CountResources()
        {
            return CountResourcesSent() + CountResourcesReceived();
        }

        public int CountResourcesReceived()
        {
            return s_resourcesReceived.Count; // atomic for our needs
        }

        public int CountResourcesSent()
        {
            return s_resourcesSent.Count; // atomic for our needs
        }

        public int TotalResources()
        {
            return TotalResourcesSent() + TotalResourcesReceived();
        }

        public int TotalResourcesSent()
        {
            return createSnapshot(s_resourcesSentLock, s_resourcesSent).Sum(t => t.amount);
        }

        public int TotalResourcesSent(ResourceCategoryType resourceCategoryType)
        {
            return createSnapshot(s_resourcesSentLock, s_resourcesSent)
                .Where(t => t.resourceCategoryType == resourceCategoryType)
                .Sum(t => t.amount);
        }

        public int TotalResourcesSent(ResourceDestinationType resourceDestinationType)
        {
            return createSnapshot(s_resourcesSentLock, s_resourcesSent)
                .Where(t => t.resourceDestinationType == resourceDestinationType)
                .Sum(t => t.amount);
        }

        public int TotalResourcesSent(ResourceCategoryType resourceCategoryType, ResourceDestinationType resourceDestinationType)
        {
            return createSnapshot(s_resourcesSentLock, s_resourcesSent)
                .Where(t => t.resourceCategoryType == resourceCategoryType && t.resourceDestinationType == resourceDestinationType)
                .Sum(t => t.amount);
        }

        public int TotalResourcesReceived()
        {
            return createSnapshot(s_resourcesReceivedLock, s_resourcesReceived).Sum(t => t.amount);
        }

        public int TotalResourcesReceived(ResourceCategoryType resourceCategoryType)
        {
            return createSnapshot(s_resourcesReceivedLock, s_resourcesReceived)
                .Where(t => t.resourceCategoryType == resourceCategoryType)
                .Sum(t => t.amount);
        }

        public int TotalResourcesReceived(ResourceDestinationType resourceDestinationType)
        {
            return createSnapshot(s_resourcesReceivedLock, s_resourcesReceived)
                .Where(t => t.resourceDestinationType == resourceDestinationType)
                .Sum(t => t.amount);
        }

        public int TotalResourcesReceived(ResourceCategoryType resourceCategoryType, ResourceDestinationType resourceDestinationType)
        {
            return createSnapshot(s_resourcesReceivedLock , s_resourcesReceived)
                .Where(t => t.resourceCategoryType == resourceCategoryType && t.resourceDestinationType == resourceDestinationType)
                .Sum(t => t.amount);
        }

        /// <summary>
        /// Using the provided lock (o) for thread safety, create a copy of the provided tracked resource list. This should be used
        /// when lists are manipulated.
        /// </summary>
        /// <param name="o">Object lock, must be non-null.</param>
        /// <param name="list">Source list to create a snapshot of.</param>
        /// <returns>New list containing references to the copied tracked values.</returns>
        private List<TrackedResource> createSnapshot(object o, List<TrackedResource> list)
        {
            if (list == null)
            {
                return null;
            }

            List<TrackedResource> l = null;
            lock (o)
            {
                l = new List<TrackedResource>(list.Count);
                list.ForEach(t => l.Add(t));
            }
            return l;
        }

        public int CarsSentLastTime => GetTotalWhere(f => (f & CarFlags.Sent) != 0 && (f & CarFlags.Previous) != 0);
        public int CarsReceivedLastTime => GetTotalWhere(f => (f & CarFlags.Sent) == 0 && (f & CarFlags.Previous) != 0);
        public int CarsSent => GetTotalWhere(f => (f & CarFlags.Sent) != 0 && (f & CarFlags.Previous) == 0);
        public int CarsReceived => GetTotalWhere(f => (f & CarFlags.Sent) == 0 && (f & CarFlags.Previous) == 0);

        public void TrackResourceSent(DateTime ts, ResourceDestinationType resourceDestinationType, ResourceType resourceType, int amount)
        {
            lock (s_resourcesSentLock)
            {
                s_resourcesSent.Add(new TrackedResource(ts, resourceDestinationType, resourceType, amount));
            }
        }

        public void TrackResourceReceived(DateTime ts, ResourceDestinationType resourceDestinationType, ResourceType resourceType, int amount)
        {
            lock (s_resourcesReceivedLock)
            {
                s_resourcesReceived.Add(new TrackedResource(ts, resourceDestinationType, resourceType, amount));
            }
        }

        // In case the number of flags changes between versions
        public CargoStats2 Upgrade()
        {
            var upgradedStats = new CargoStats2();
            CarsCounted.CopyTo(upgradedStats.CarsCounted, 0);
            return upgradedStats;
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

    // Used only in v1.1 and below
    //[Serializable]
    //public class CargoStats
    //{
    //    public int carsReceivedLastTime = 0;
    //    public int carsSentLastTime = 0;
    //}
}
