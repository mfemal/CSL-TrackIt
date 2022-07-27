using System;
using System.Text;
using UnityEngine;
using ColossalFramework.UI;
using TrackIt.API;

namespace TrackIt
{
    public class WorldInfoPanelMonitor : MonoBehaviour
    {
        private CityServiceWorldInfoPanel _cityServiceWorldInfoPanel;
        private ushort _cachedBuildingID;
        private CargoUIPanel _cargoPanel;
        private UILabel _buildingStatsLabel;
        private UIPanel _rightPanel;
        private MouseEventHandler showDelegate;

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
                if (!WorldInfoPanel.AnyWorldInfoPanelOpen())
                {
                    ResetCache();
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
            DataManager.instance.CargoBuildingChanged -= UpdateBuilding;
            if (_rightPanel != null)
            {
                _rightPanel.eventClicked -= showDelegate;
            }
            if (_cargoPanel != null)
            {
                Destroy(_cargoPanel);
            }
            if (_buildingStatsLabel != null)
            {
                Destroy(_buildingStatsLabel);
            }
        }

        public void UpdateBuilding(MonitoredDataChanged monitoredDataChanged)
        {
            if (monitoredDataChanged.sourceID == _cachedBuildingID)
            {
                ResetCache();
            }
        }

        private void CreateUI()
        {
#if DEBUG
            LogUtil.LogInfo("Setting up UI...");
#endif
            _cargoPanel = (CargoUIPanel)UIView.GetAView().AddUIComponent(typeof(CargoUIPanel));

            _cityServiceWorldInfoPanel = UIView.library.Get<CityServiceWorldInfoPanel>(typeof(CityServiceWorldInfoPanel).Name);
            if (_cityServiceWorldInfoPanel == null)
                LogUtil.LogError("CityServiceWorldInfoPanel not found");
            _rightPanel = _cityServiceWorldInfoPanel?.Find<UIPanel>("Right");
            if (_rightPanel == null)
                LogUtil.LogError("CityServiceWorldInfoPanel.Right panel not found");
            else
            {
                UILabel descPanel = _rightPanel?.Find<UILabel>("Desc");

                _buildingStatsLabel = UIUtils.CreateLabel(_rightPanel, "BuildingStats", null);
                UIUtils.CopyTextStyleAttributes(descPanel, _buildingStatsLabel);
                _buildingStatsLabel.anchor = UIAnchorStyle.Bottom;
                showDelegate = (sender, e) =>
                {
                    if (DataManager.instance.TryGetBuilding(WorldInfoPanel.GetCurrentInstanceID().Building, out _))
                    {
                        _cargoPanel.Show();
                    }
                };
                _rightPanel.eventClicked += showDelegate;
                DataManager.instance.CargoBuildingChanged += UpdateBuilding;
            }
        }

        private void ResetCache()
        {
            _cachedBuildingID = 0;
        }

        private void UpdateData()
        {
            if (!_cityServiceWorldInfoPanel.component.isVisible)
            {
                ResetCache();
            }
            else
            {
                InstanceID instanceID = WorldInfoPanel.GetCurrentInstanceID();
                switch (instanceID.Type)
                {
                    case InstanceType.Building:
                        UpdateBuildingCargoStats(instanceID.Building);
                        break;
                    case InstanceType.Vehicle: // TODO: add feature
                    default: // skip all others
                        SetBuildingCargoStatsVisibile(false);
                        break;
                }
            }
        }

        private void SetBuildingCargoStatsVisibile(bool visible)
        {
            if (_buildingStatsLabel == null)
            {
                return;
            }
            if (visible)
            {
                _buildingStatsLabel.Show();
            }
            else
            {
                _buildingStatsLabel.Hide();
            }
        }

        private void UpdateBuildingCargoStats(ushort buildingID)
        {
            if (buildingID == _cachedBuildingID || _buildingStatsLabel == null)
            {
                return;
            }
            CargoStatistics cargoStatistics;
            if (DataManager.instance.TryGetBuilding(buildingID, out cargoStatistics))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat(
                    "{0}: {1:0}",
                    Localization.Get("TRUCKS_RCVD"),
                    cargoStatistics.CountResourcesReceived());
                sb.AppendLine();
                sb.AppendFormat(
                    "{0}: {1:0}",
                    Localization.Get("TRUCKS_SENT"),
                    cargoStatistics.CountResourcesSent());
                sb.AppendLine();
                sb.Append(Localization.Get("CLICK_MORE"));
                _buildingStatsLabel.text = sb.ToString();
                _cachedBuildingID = buildingID;
                SetBuildingCargoStatsVisibile(true);
            }
            else
            {
                ResetCache();
                SetBuildingCargoStatsVisibile(false);
            }
        }
    }
}
