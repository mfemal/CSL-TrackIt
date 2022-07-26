using System.Runtime.Serialization;
using ICities;

namespace CargoInfoMod
{
    internal class PersistenceExtension : SerializableDataExtensionBase
    {
        public override void OnLoadData()
        {
#if DEBUG
            LogUtil.LogInfo("Restoring previous data...");
#endif
            var data = serializableDataManager.LoadData(DataManager.PersistenceId);
            if (data == null)
            {
#if DEBUG
                LogUtil.LogInfo("No previous data found, unable to load previously tracked data.");
#endif
                return;
            }

            try
            {
            }
            catch (SerializationException e)
            {
                LogUtil.LogException(e);
            }
        }
    }
}
