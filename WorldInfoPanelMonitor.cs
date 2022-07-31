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
        private CargoUIPanel _buildingCargoPanel;
        private UILabel _buildingCargoLabel;
        private UIPanel _rightPanel;

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
                else if (IsInitialized())
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
            if (_buildingCargoPanel != null)
            {
                Destroy(_buildingCargoPanel);
            }
            if (_buildingCargoLabel != null)
            {
                Destroy(_buildingCargoLabel);
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
            _cityServiceWorldInfoPanel = UIView.library.Get<CityServiceWorldInfoPanel>(typeof(CityServiceWorldInfoPanel).Name);
            if (_cityServiceWorldInfoPanel == null)
            {
                LogUtil.LogError("CityServiceWorldInfoPanel not found");
            }
            _rightPanel = _cityServiceWorldInfoPanel?.Find<UIPanel>("Right");
            if (_rightPanel == null)
            {
                LogUtil.LogError("CityServiceWorldInfoPanel.Right panel not found");
            }
            else
            {
                _buildingCargoLabel = UIUtils.CreateLabel(_rightPanel, "BuildingStatsTruckCounts", null);
                _buildingCargoLabel.anchor = UIAnchorStyle.Bottom;

                UILabel descPanel = _rightPanel?.Find<UILabel>("Desc");
                if (descPanel != null)
                {
                    UIUtils.CopyTextStyleAttributes(descPanel, _buildingCargoLabel);
                }
                else
                {
                    LogUtil.LogError("Unable to find 'Desc' in CityServiceWorldInfoPanel, style detail not found.");
                }

                UIPanel mainSectionPanel = _cityServiceWorldInfoPanel.Find<UIPanel>("MainSectionPanel");
                UIPanel mainBottom = _cityServiceWorldInfoPanel.Find<UIPanel>("MainBottom");
                if (mainSectionPanel != null && mainBottom != null)
                {
                    _buildingCargoPanel = mainSectionPanel.AddUIComponent<CargoUIPanel>();
                    _buildingCargoPanel.minimumSize = new Vector2(mainBottom.size.x, 380f);
                    _buildingCargoPanel.width = mainBottom.size.x;
                    _buildingCargoPanel.verticalSpacing = 20;
                    _buildingCargoPanel.anchor = UIAnchorStyle.Left | UIAnchorStyle.Right;
                    _buildingCargoPanel.zOrder = mainBottom.zOrder; // append this panel above the bottom one, all components are reordered in UIComponent
                }
                else
                {
                    LogUtil.LogError("Unable to find 'MainSectionPanel' or 'MainBottom' in CityServiceWorldInfoPanel, no charts available.");
                }
                DataManager.instance.CargoBuildingChanged += UpdateBuilding;
            }
        }

        private bool IsInitialized()
        {
            return _buildingCargoLabel != null && _buildingCargoPanel != null;
        }

        /// <summary>
        /// This method is for change detection so updates to the UI are done only when needed.
        /// </summary>
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
                        UpdateBuildingCargoStatistics(instanceID.Building);
                        break;
                    case InstanceType.Vehicle: // TODO: add feature
                    default: // skip all others
                        SetBuildingCargoVisible(false);
                        break;
                }
            }
        }

        private void SetBuildingCargoVisible(bool visible)
        {
            if (visible)
            {
                _buildingCargoLabel.Show();
                _buildingCargoPanel.Show();
            }
            else
            {
                _buildingCargoLabel.Hide();
                _buildingCargoPanel.Hide();
            }
        }

        private void UpdateBuildingCargoStatistics(ushort buildingID)
        {
            if (buildingID == _cachedBuildingID)
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
                _buildingCargoLabel.text = sb.ToString();
                _buildingCargoPanel.UpdateCargoValues(cargoStatistics);
                SetBuildingCargoVisible(true);
            }
            else
            {
                SetBuildingCargoVisible(false);
            }
            _cachedBuildingID = buildingID;
        }
    }
}
