using System;
using ColossalFramework;
using ICities;

namespace TrackIt
{
    public class ExpungeThreadingExtension : ThreadingExtensionBase
    {
        private DateTime _lastExpungeDate = DateTime.MinValue;
        private byte _historyRetentionDays = 14;

        /// <summary>
        /// Called once per rendered frame.
        /// </summary>
        /// <param name="realTimeDelta">Seconds since previous frame</param>
        /// <param name="simulationTimeDelta">smoothly interpolated to be used from main thread. On normal speed it is roughly same as realTimeDelta</param>
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            DateTime currentGameDate = Singleton<SimulationManager>.instance.m_currentGameTime.Date;
            if (_lastExpungeDate == DateTime.MinValue)
            {
                _lastExpungeDate = currentGameDate;
            }
            else if (_lastExpungeDate != currentGameDate)
            {
                if ((currentGameDate - _lastExpungeDate).TotalDays >= 1)
                {
                    DateTime expungeDate = currentGameDate.AddDays(-_historyRetentionDays);
                    LogUtil.LogWarning($"Updating currentGameDate: {currentGameDate} _historyRetentionDays: {_historyRetentionDays} expungeDate: {expungeDate}");
                    DataManager.instance.ExpungeOlderThan(expungeDate);
                    _lastExpungeDate = currentGameDate;
                }
            }
        }
    }
}
