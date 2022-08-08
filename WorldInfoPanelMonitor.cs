using System;
using System.Text;
using UnityEngine;
using ColossalFramework.UI;
using TrackIt.API;

namespace TrackIt
{
    /// <summary>
    /// Unity behaviour to handle initial display and updates for the companion window WorldInfoPanel.
    /// </summary>
    /// <seealso cref="WorldInfoPanel" />
    public class WorldInfoPanelMonitor : MonoBehaviour
    {
        private const string _namePrefix = "WorldInfoPanel";
        private CityServiceWorldInfoPanel _cityServiceWorldInfoPanel;
        private ushort _cachedBuildingID;
        private UIPanel _containerPanel; // main top-level container
        private Vector2 _containerPadding = new Vector2(4, 4);
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
            if (_containerPanel != null)
            {
                DataManager.instance.CargoBuildingChanged -= UpdateBuilding;
                Destroy(_containerPanel);
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
                UIPanel mainSectionPanel = _cityServiceWorldInfoPanel.Find<UIPanel>("MainSectionPanel");
                UIPanel mainBottom = _cityServiceWorldInfoPanel.Find<UIPanel>("MainBottom");
                if (mainSectionPanel == null || mainBottom == null)
                {
                    LogUtil.LogError("Unable to find 'MainSectionPanel' or 'MainBottom' in CityServiceWorldInfoPanel, no charts available.");
                    return;
                }
                _containerPanel = UIUtils.CreateWorldInfoCompanionPanel(_cityServiceWorldInfoPanel.component, _namePrefix, "SubcategoriesPanel");
                _containerPanel.autoLayout = true;
                _containerPanel.autoLayoutDirection = LayoutDirection.Vertical;
                _containerPanel.autoLayoutPadding = new RectOffset(6, 6, 6, 6);
                _containerPanel.width = _containerPanel.parent.width;

                _buildingCargoLabel = UIUtils.CreateLabel(_containerPanel, "BuildingTruckTotals", null);
                UILabel descPanel = _rightPanel?.Find<UILabel>("Desc");
                if (descPanel != null)
                {
                    UIUtils.CopyTextStyleAttributes(descPanel, _buildingCargoLabel);
                }
                else
                {
                    LogUtil.LogError("Unable to find 'Desc' in CityServiceWorldInfoPanel, style detail not found (using defaults).");
                }

                _buildingCargoPanel = _containerPanel.AddUIComponent<CargoUIPanel>();
                _buildingCargoPanel.width = mainBottom.size.x - (_containerPanel.autoLayoutPadding.left + _containerPanel.autoLayoutPadding.right);
                _buildingCargoPanel.verticalSpacing = 20;

                // TODO: handle with configuration BottomLeft if conflicts from other mods
                // TODO: change width to either be same as panel width itself (bottom) or trimmed (right)
                _containerPanel.AlignTo(_cityServiceWorldInfoPanel.component, UIAlignAnchor.TopRight);
                _containerPanel.relativePosition = new Vector3(_containerPanel.parent.width + 5f,
                    _cityServiceWorldInfoPanel.Find<UIPanel>("CaptionPanel")?.height ?? 0f);
                _containerPanel.height = _buildingCargoLabel.height + _buildingCargoPanel.height +
                    _buildingCargoPanel.verticalSpacing +
                    _containerPanel.autoLayoutPadding.top + _containerPanel.autoLayoutPadding.bottom;

                DataManager.instance.CargoBuildingChanged += UpdateBuilding;
            }
        }

        private bool IsInitialized()
        {
            return _containerPanel != null;
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
                _containerPanel.Show();
            }
            else
            {
                _containerPanel.Hide();
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
                    cargoStatistics.TrucksUnloadedCount);
                sb.AppendLine();
                sb.AppendFormat(
                    "{0}: {1:0}",
                    Localization.Get("TRUCKS_SENT"),
                    cargoStatistics.TrucksLoadedCount);
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
