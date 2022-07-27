using System;

namespace TrackIt.API
{
    public class MonitoredDataChanged : EventArgs
    {
        public ushort sourceID;

        public MonitoredDataChanged(ushort sourceID)
        {
            this.sourceID = sourceID;
        }
    }

    /// <summary>
    /// Generic event handler class for processing changes associated with tracked data.
    /// </summary>
    /// <remarks>
    /// Most entities in the game use an InstanceID value (e.g. ushort) which is set. This is the only
    /// data that is used in the event to minimize data passing and changes to this API. Currently,
    /// it is easy to lookup the current data for game objects using this value and its InstanceType. This
    /// InstanceType is not used in the API to avoid complexity and have the 'event' include that in its name
    /// for more declarative usage and readability.
    /// </remarks>
    public delegate void MonitoredDataEventHandler(MonitoredDataChanged e);
}
