using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ColossalFramework.UI;
using TrackIt.API;

namespace TrackIt
{
    /// <summary>
    /// Unity behaviour to handle intial display and updates for the CityServiceVehicleWorldInfoPanel.
    /// </summary>
    public class VehicleWorldInfoPanelMonitor : MonoBehaviour
    {
        private CityServiceVehicleWorldInfoPanel _cityServiceVehicleWorldInfoPanel;
        private ushort _cachedVehicleID;
        private CargoUIChart _vehicleCargoChart;

        public void Start()
        {
            try
            {
                CreateUI();
            }
            catch (Exception e)
            {
                LogUtil.LogException(e);
            }
        }

        public void Update()
        {
            try
            {
                if (!_cityServiceVehicleWorldInfoPanel.component.isVisible)
                {
                    _cachedVehicleID = 0;
                }
                else
                {
                    UpdateData();
                }
            }
            catch (Exception e)
            {
                LogUtil.LogException(e);
            }
        }

        public void OnDestroy()
        {
            if (_vehicleCargoChart != null)
            {
                Destroy(_vehicleCargoChart);
            }
        }

        private void CreateUI()
        {
            _cityServiceVehicleWorldInfoPanel = UIView.library.Get<CityServiceVehicleWorldInfoPanel>(typeof(CityServiceVehicleWorldInfoPanel).Name);
            if (_cityServiceVehicleWorldInfoPanel == null)
            {
                LogUtil.LogError("Unable to find CityServiceVehicleWorldInfoPanel");
                return;
            }
            UIPanel vehiclePanel = _cityServiceVehicleWorldInfoPanel?.Find<UIPanel>("Panel");
            if (vehiclePanel != null)
            {
                vehiclePanel.autoLayout = false;
                _vehicleCargoChart = UIUtils.CreateCargoGroupedResourceChart(vehiclePanel, "VehicleCargoGroupedResourceChart");
                _vehicleCargoChart.size = new Vector2(60, 60);
                _vehicleCargoChart.relativePosition = new Vector3(330, 0);
            }
            else
            {
                LogUtil.LogError("CityServiceVehicleWorldInfoPanel not found!");
            }
        }

        private void UpdateData()
        {
            var vehicleID = WorldInfoPanel.GetCurrentInstanceID().Vehicle;
            if (vehicleID != 0 && _vehicleCargoChart != null && _cachedVehicleID != vehicleID)
            {
                _cachedVehicleID = vehicleID;

                IDictionary<ResourceCategoryType, int> vehicleCargoCategoryTotals = GameEntityDataExtractor.GetVehicleCargoBasicResourceTotals(vehicleID);
                if (vehicleCargoCategoryTotals.Count != 0)
                {
#if DEBUG
                    LogUtil.LogInfo("Vehicle Cargo Total: " +
                        vehicleCargoCategoryTotals.Select(kv => kv.Value).ToList().Sum() +
                        ", Groups: {" + vehicleCargoCategoryTotals.Select(kv => kv.Key + ": " + kv.Value).Aggregate((p, c) => p + ": " + c) + "}");
#endif
                    _vehicleCargoChart.SetValues(vehicleCargoCategoryTotals);
                    _vehicleCargoChart.Show();
                }
                else
                {
#if DEBUG
                    LogUtil.LogInfo("No cargo resources found for vehicle ${vehicleID}");
#endif
                    _vehicleCargoChart.Hide();
                }
            }
        }
    }
}
