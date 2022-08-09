using System;
using System.Text;

namespace TrackIt.API
{
    /// <summary>
    /// Cargo data is segmented into 3 different types based on destination. The classification of local is a
    /// transfer between two entities in the same city, the export designation denotes transfers to outside the
    /// city, and finally import denotes transfers from an external city. Cargo is further separated into either
    /// transfers that are sent and received to make a total of 6 different ways a transfer is classified.
    /// </summary>
    [Serializable]
    public class CargoStatistics
    {
        public uint TrucksLoadedCount
        {
            get;
            private set;
        }

        public uint TrucksUnloadedCount
        {
            get;
            private set;
        }

        public uint TrainsLoadedCount
        {
            get;
            private set;
        }

        public uint TrainsUnloadedCount
        {
            get;
            private set;
        }

        public uint PlanesLoadedCount
        {
            get;
            private set;
        }

        public uint PlanesUnloadedCount
        {
            get;
            private set;
        }

        public uint ShipsLoadedCount
        {
            get;
            private set;
        }

        public uint ShipsUnloadedCount
        {
            get;
            private set;
        }

        private CargoResourceData _sentlocalData;
        private CargoResourceData _receivedLocalData;
        private CargoResourceData _sentImportData;
        private CargoResourceData _receivedImportData;
        private CargoResourceData _sentExportData;
        private CargoResourceData _receivedExportData;

        /// <summary>
        /// An object lock is used as the Simulation and UI thread may be concurrently reading and writing to
        /// the same underlying data structures so this avoids indeterminate runtime problems.
        /// </summary>
        [NonSerialized]
        private readonly object _lockObject = new object();

        public CargoResourceData TotalResourcesReceived(ResourceDestinationType resourceDestinationType)
        {
            CargoResourceData cargoResourceData = new CargoResourceData();
            lock (_lockObject)
            {
                switch (resourceDestinationType)
                {
                    case ResourceDestinationType.Local:
                        cargoResourceData = _receivedLocalData;
                        break;
                    case ResourceDestinationType.Import:
                        cargoResourceData = _receivedImportData;
                        break;
                    case ResourceDestinationType.Export:
                        cargoResourceData = _receivedExportData;
                        break;
                }
            }
            return cargoResourceData;
        }

        public CargoResourceData TotalResourcesSent(ResourceDestinationType resourceDestinationType)
        {
            CargoResourceData cargoResourceData = new CargoResourceData();
            lock (_lockObject)
            {
                switch (resourceDestinationType)
                {
                    case ResourceDestinationType.Local:
                        cargoResourceData = _sentlocalData;
                        break;
                    case ResourceDestinationType.Import:
                        cargoResourceData = _sentImportData;
                        break;
                    case ResourceDestinationType.Export:
                        cargoResourceData = _sentExportData;
                        break;
                }
            }
            return cargoResourceData;
        }

        public void TrackResourceSent(TrackedResource trackedResource)
        {
            CargoResourceData cargoResourceData = CreateCargoResourceData(trackedResource.ResourceType, trackedResource.Amount);
            lock (_lockObject)
            {
                switch (trackedResource.ResourceDestinationType)
                {
                    case ResourceDestinationType.Local:
                        _sentlocalData.Add(ref cargoResourceData);
                        break;
                    case ResourceDestinationType.Import:
                        _sentImportData.Add(ref cargoResourceData);
                        break;
                    case ResourceDestinationType.Export:
                        _sentExportData.Add(ref cargoResourceData);
                        break;
                }
                TrucksLoadedCount++;
            }
        }

        public void TrackResourceReceived(TrackedResource trackedResource)
        {
            CargoResourceData cargoResourceData = CreateCargoResourceData(trackedResource.ResourceType, trackedResource.Amount);
            lock (_lockObject)
            {
                switch (trackedResource.ResourceDestinationType)
                {
                    case ResourceDestinationType.Local:
                        _receivedLocalData.Add(ref cargoResourceData);
                        break;
                    case ResourceDestinationType.Import:
                        _receivedImportData.Add(ref cargoResourceData);
                        break;
                    case ResourceDestinationType.Export:
                        _receivedExportData.Add(ref cargoResourceData);
                        break;
                }
                TrucksUnloadedCount++;
            }
        }

        public override string ToString()
        {
            return new StringBuilder()
                .Append($"Planes Loaded: {PlanesLoadedCount}, Unloaded: {PlanesUnloadedCount}, ")
                .Append($"Ships Loaded: {ShipsLoadedCount}, Unloaded: {ShipsUnloadedCount}, ")
                .Append($"Trains Loaded: {TrainsLoadedCount}, Unloaded: {TrainsUnloadedCount}, ")
                .Append($"Trucks Loaded: {TrucksLoadedCount}, Unloaded: {TrucksUnloadedCount}, ")
                .Append("Totals: [")
                .AppendFormat("Local: (Sent: {0}, Received {1}), ", _sentlocalData, _receivedLocalData)
                .AppendFormat("Import: (Sent: {0}, Received: {1}), ", _sentImportData, _receivedImportData)
                .AppendFormat("Export: (Sent: {0}, Received: {1})", _sentExportData, _receivedExportData)
                .Append("]")
                .ToString();
        }

        internal void TrackArrival(TravelVehicleType travelVehicleType)
        {
            lock (_lockObject)
            {
                switch (travelVehicleType)
                {
                    case TravelVehicleType.CargoPlane:
                        PlanesUnloadedCount++;
                        break;
                    case TravelVehicleType.CargoShip:
                        ShipsUnloadedCount++;
                        break;
                    case TravelVehicleType.CargoTrain:
                        TrainsUnloadedCount++;
                        break;
                }
            }
        }

        internal void TrackDeparture(TravelVehicleType travelVehicleType)
        {
            lock (_lockObject)
            {
                switch (travelVehicleType)
                {
                    case TravelVehicleType.CargoPlane:
                        PlanesLoadedCount++;
                        break;
                    case TravelVehicleType.CargoShip:
                        ShipsLoadedCount++;
                        break;
                    case TravelVehicleType.CargoTrain:
                        TrainsLoadedCount++;
                        break;
                }
            }
        }

        internal void Update()
        {
            lock (_lockObject)
            {
                _sentlocalData.Update();
                _receivedLocalData.Update();
                _sentImportData.Update();
                _receivedImportData.Update();
                _sentExportData.Update();
                _receivedExportData.Update();
            }
        }

        private CargoResourceData CreateCargoResourceData(ResourceType resourceType, uint amount)
        {
            CargoResourceData cargoResourceData = new CargoResourceData();
            switch (resourceType.InferResourceCategoryType())
            {
                case ResourceCategoryType.Agriculture:
                    cargoResourceData._tempAgriculture = amount;
                    break;
                case ResourceCategoryType.Fish:
                    cargoResourceData._tempFish = amount;
                    break;
                case ResourceCategoryType.Forestry:
                    cargoResourceData._tempForestry = amount;
                    break;
                case ResourceCategoryType.Goods:
                    cargoResourceData._tempGoods = amount;
                    break;
                case ResourceCategoryType.Mail:
                    cargoResourceData._tempMail = amount;
                    break;
                case ResourceCategoryType.Oil:
                    cargoResourceData._tempOil = amount;
                    break;
                case ResourceCategoryType.Ore:
                    cargoResourceData._tempOre = amount;
                    break;
            }
            return cargoResourceData;
        }
    }
}
