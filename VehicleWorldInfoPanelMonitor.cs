using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ColossalFramework.UI;
using TrackIt.API;

namespace TrackIt
{
    /// <summary>
    /// Unity behaviour to handle initial display and updates for the companion window CityServiceVehicleWorldInfoPanel.
    /// </summary>
    /// <seealso cref="CityServiceVehicleWorldInfoPanel" />
    public class VehicleWorldInfoPanelMonitor : MonoBehaviour
    {
        internal struct TrackingRow
        {
            internal UIPanel Panel;
            internal UILabel Description;
            internal UIProgressBar ProgressBar;
            internal UILabel Percentage;
        }

        private CityServiceVehicleWorldInfoPanel _cityServiceVehicleWorldInfoPanel;
        private ushort _cachedVehicleID;
        private ushort _cachedLeadingVehicleID;

        private const string _namePrefix = "VehicleWorldInfoPanel";
        private const int _chartWidth = 60;
        private const int _chartHeight = 60;

        private UIPanel _containerPanel; // main top-level container
        private Vector2 _containerPadding = new Vector2(4, 4);
        private UIPanel _vehicleCargoPanel;
        private Vector2 _vehicleCargoPadding = new Vector2(8, 8);
        private CargoUIChart _vehicleCargoChart; // grouped category resource chart within _vehicleCargoPanel
        private IList<TrackingRow> _vehicleCargoContents; // internal map whose offset matches index in UIUtils.CargoBasicResourceGroups

        public void OnDestroy()
        {
            if (_containerPanel != null)
            {
                DataManager.instance.CargoVehicleChanged -= UpdateVehicle;
                Destroy(_containerPanel);
            }
        }

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

        public void UpdateVehicle(MonitoredDataChanged monitoredDataChanged)
        {
            if (monitoredDataChanged.sourceID == _cachedVehicleID || monitoredDataChanged.sourceID == _cachedLeadingVehicleID)
            {
                ResetCache();
            }
        }

        private void CreateUI()
        {
            _cityServiceVehicleWorldInfoPanel = UIView.library.Get<CityServiceVehicleWorldInfoPanel>(typeof(CityServiceVehicleWorldInfoPanel).Name);
            UIProgressBar loadProgressBar = _cityServiceVehicleWorldInfoPanel?.component.Find<UIProgressBar>("LoadBar");
            if (_cityServiceVehicleWorldInfoPanel == null)
            {
                LogUtil.LogError("Unable to find CityServiceVehicleWorldInfoPanel");
                return;
            }

            UILabel totalLoadInfoPercentageLabel = _cityServiceVehicleWorldInfoPanel?.Find<UILabel>("LoadInfo");

            // InfoBubbleVehicle used on main panel, but it contains a title area which is not suitable here (use next best option - GenericTab)
            _containerPanel = UIUtils.CreateWorldInfoCompanionPanel(_cityServiceVehicleWorldInfoPanel.component, _namePrefix, "GenericTab");
            _containerPanel.autoLayout = true;
            _containerPanel.autoLayoutDirection = LayoutDirection.Vertical;
            _containerPanel.autoLayoutPadding = new RectOffset(6, 6, 6, 6); // TODO: account for in resize!
            _containerPanel.width = _containerPanel.parent.width;

            _vehicleCargoContents = new List<TrackingRow>(UIUtils.CargoBasicResourceGroups.Count);

            _vehicleCargoPanel = UIUtils.CreatePanel(_containerPanel, _namePrefix + "Cargo");
            _vehicleCargoPanel.autoLayout = true;
            _vehicleCargoPanel.autoLayoutDirection = LayoutDirection.Vertical;

            _vehicleCargoChart = UIUtils.CreateCargoGroupedResourceChart(_vehicleCargoPanel, _namePrefix + "CargoGroupedResourceChart");
            _vehicleCargoChart.size = new Vector2(_chartWidth, _chartHeight);
            _vehicleCargoChart.anchor = UIAnchorStyle.CenterHorizontal;

            ResourceCategoryType resourceCategoryType;
            UIPanel vehicleCargoContentRow;
            for (int i = 0; i < UIUtils.CargoBasicResourceGroups.Count; i++)
            {
                resourceCategoryType = UIUtils.CargoBasicResourceGroups[i];

                vehicleCargoContentRow = UIUtils.CreatePanel(_vehicleCargoPanel, "VehicleCargo" + resourceCategoryType + "Row");
                vehicleCargoContentRow.autoLayout = true;
                vehicleCargoContentRow.autoLayoutDirection = LayoutDirection.Vertical;
                vehicleCargoContentRow.width = _vehicleCargoPanel.width;

                string localeID = UIUtils.GetLocaleID(resourceCategoryType);
                TrackingRow trackingRow = new TrackingRow
                {
                    Panel = vehicleCargoContentRow,
                    Description = UIUtils.CreateLabel(vehicleCargoContentRow, "VehicleCargo" + resourceCategoryType + "Label", localeID),
                    ProgressBar = UIUtils.CreateCargoProgressBar(vehicleCargoContentRow, "VehicleCargo" + resourceCategoryType + "Amount"),
                    Percentage = UIUtils.CreateLabel(vehicleCargoContentRow, "VehicleCargo" + resourceCategoryType + "Percent", null)
                };

                UIUtils.CopyTextStyleAttributes(totalLoadInfoPercentageLabel, trackingRow.Percentage);
                trackingRow.Percentage.AlignTo(trackingRow.ProgressBar, UIAlignAnchor.TopRight);
                trackingRow.Percentage.relativePosition = new Vector3(trackingRow.ProgressBar.width + 20f, 0f);

                trackingRow.Panel.width = _containerPanel.width;
                trackingRow.Panel.height = 40f;
                trackingRow.Description.textScale = 0.8125f;
                trackingRow.Description.width = 80;
                trackingRow.Description.autoHeight = true;
                trackingRow.ProgressBar.width = loadProgressBar?.width ?? 293;

                _vehicleCargoContents.Add(trackingRow);
            }

            // TODO: handle with configuration BottomLeft if conflicts from other mods
            // TODO: change width to either be same as panel width itself (bottom) or trimmed (right)
            _containerPanel.AlignTo(_cityServiceVehicleWorldInfoPanel.component, UIAlignAnchor.TopRight);
            _containerPanel.relativePosition = new Vector3(_containerPanel.parent.width + 5f, 0);

            DataManager.instance.CargoVehicleChanged += UpdateVehicle;
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
            _cachedVehicleID = 0;
            _cachedLeadingVehicleID = 0;
        }

        private void UpdateData()
        {
            InstanceID instanceID = WorldInfoPanel.GetCurrentInstanceID();
            ushort vehicleID = instanceID.Type == InstanceType.Vehicle ? instanceID.Vehicle : (ushort)0;
            if (_cachedVehicleID == vehicleID)
            {
                return;
            }
            ushort leadingVehicleID = 0;
            IDictionary<ResourceCategoryType, uint> vehicleCargoCategoryTotals = GameEntityDataExtractor.GetVehicleCargoBasicResourceTotals(
                vehicleID, out leadingVehicleID);
            if (_cachedLeadingVehicleID == leadingVehicleID)
            {
                return;
            }
            if (vehicleCargoCategoryTotals.Count != 0)
            {
                long grandTotal = vehicleCargoCategoryTotals.Values.Cast<long>().Sum();
#if DEBUG
                LogUtil.LogInfo($"Updating cargo based on vehicleID: {vehicleID} leadingVehicleID: {leadingVehicleID} grandTotal: {grandTotal}" + ", groups: {" +
                    vehicleCargoCategoryTotals.Select(kv => kv.Key + ": " +
                    kv.Value).Aggregate((p, c) => p + ": " + c) + "}");
#endif
                ResourceCategoryType resourceCategoryType;
                TrackingRow vehicleCargoContentRow;
                uint categoryTotal = 0;
                int j = 0, a = 0; // accumulator and index to ensure category totals always add to 100 (percentage for display is range for progress bar)
                for (int i = 0; i < UIUtils.CargoBasicResourceGroups.Count; i++)
                {
                    resourceCategoryType = UIUtils.CargoBasicResourceGroups[i];
                    vehicleCargoContentRow = _vehicleCargoContents[i];
                    if (vehicleCargoContentRow.Description.isLocalized && vehicleCargoCategoryTotals.TryGetValue(resourceCategoryType, out categoryTotal))
                    {
                        UpdateProgressBar(vehicleCargoContentRow.ProgressBar,
                            UIUtils.GetResourceCategoryColor(resourceCategoryType),
                            categoryTotal,
                            grandTotal);
                        int p = (int)(vehicleCargoContentRow.ProgressBar.value + 0.5);
                        if (++j == vehicleCargoCategoryTotals.Count)
                        {
                            p = 100 - a;
                        }
                        else
                        {
                            a += p;
                        }
                        vehicleCargoContentRow.Percentage.text = LocaleFormatter.FormatPercentage(p);
                        vehicleCargoContentRow.Panel.Show();
                    }
                    else
                    {
                        vehicleCargoContentRow.Panel.Hide();
                    }
                }
                _vehicleCargoChart.SetValues(vehicleCargoCategoryTotals);
                _vehicleCargoPanel.FitChildren(_vehicleCargoPadding);

                _containerPanel.FitChildren(_containerPadding);
                _containerPanel.Show();
            }
            else
            {
#if DEBUG
                LogUtil.LogInfo($"No cargo resources found for vehicleID: {vehicleID} leadingVehicleID: {leadingVehicleID}");
#endif
                _containerPanel.Hide();
            }
            _cachedVehicleID = vehicleID;
            _cachedLeadingVehicleID = leadingVehicleID;
        }

        private void UpdateProgressBar(UIProgressBar progressBar, Color color, uint amount, long total)
        {
            if (total > 0)
            {
                progressBar.tooltip = UIUtils.FormatCargoValue(amount);
                progressBar.value = Mathf.Clamp((amount * 100.0f / total), 0f, 100f);
            }
            else
            {
                progressBar.tooltip = null;
                progressBar.value = 0;
            }
            progressBar.progressColor = color;
        }
    }
}
