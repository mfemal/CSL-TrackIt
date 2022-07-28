using System;
using System.Collections.Generic;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using UnityEngine;
using TrackIt.API;

namespace TrackIt
{
    class CargoUIPanel : UIPanel
    {
        private const string _namePrefix = "CargoPanel";
        private const int _panelWidth = 484;
        private const int _statPanelHeight = 30;

        private const int _chartColumnWidth = 90;
        private const int _rowLabelWidth = 120;
        private readonly Vector2 _labelSize = new Vector2(_chartColumnWidth, 20);
        private readonly Vector2 _chartSize = new Vector2(_chartColumnWidth, 90);
        private readonly RectOffset _padding = new RectOffset(10, 10, 10, 10);
        private readonly Color32 _unitColor = new Color32(206, 248, 0, 255);

        private List<CargoUIChart> _charts = new List<CargoUIChart>();
        private List<UILabel> _labels = new List<UILabel>();
        private UILabel _localLabel, _importLabel, _exportLabel, _rcvdLabel, _sentLabel;
        private UIPanel _sentPanel, _rcvdPanel;

        public void UpdateLocale()
        {
            _localLabel.text = Localization.Get("LOCAL");
            _importLabel.text = Localization.Get("IMPORT");
            _exportLabel.text = Localization.Get("EXPORT");
            _rcvdLabel.text = Localization.Get("RECEIVED");
            _sentLabel.text = Localization.Get("SENT");
        }

        public override void Start()
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

        public void UpdateCargoValues(ushort buildingID)
        {
            if (DataManager.instance.TryGetBuilding(buildingID, out CargoStatistics cargoStatistics) && _charts.Count > 0)
            {
                UpdateCargoValues(cargoStatistics);
            }
        }

        public void UpdateCargoValues(CargoStatistics cargoStatistics)
        {
            foreach (CargoUIChart chart in _charts)
            {
                chart.SetValues(cargoStatistics);
            }
        }

        private void CreateUI()
        {
            if (name == null)
            {
                name = UIUtils.ConstructModUIComponentName(_namePrefix);
            }

            width = _panelWidth;
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;

            UIPanel summaryPanelHeader = UIUtils.CreatePanel(this, _namePrefix + "Header");
            summaryPanelHeader.size = new Vector2(_panelWidth, _labelSize.y);
            summaryPanelHeader.autoLayout = true;
            summaryPanelHeader.autoLayoutDirection = LayoutDirection.Horizontal;
            summaryPanelHeader.autoLayoutStart = LayoutStart.TopRight;
            summaryPanelHeader.autoLayoutPadding = _padding;

            _localLabel = UIUtils.CreateLabel(summaryPanelHeader, _namePrefix + "LocalLabel", Localization.Get("LOCAL"));
            _localLabel.autoSize = false;
            _localLabel.size = _labelSize;
            _localLabel.textAlignment = UIHorizontalAlignment.Center;

            _importLabel = UIUtils.CreateLabel(summaryPanelHeader, _namePrefix + "ImportLabel", Localization.Get("IMPORT"));
            _importLabel.autoSize = false;
            _importLabel.size = _labelSize;
            _importLabel.textAlignment = UIHorizontalAlignment.Center;

            _exportLabel = UIUtils.CreateLabel(summaryPanelHeader, _namePrefix + "ExportLabel", Localization.Get("EXPORT"));
            _exportLabel.autoSize = false;
            _exportLabel.size = _labelSize;
            _exportLabel.textAlignment = UIHorizontalAlignment.Center;

            _rcvdPanel = UIUtils.CreatePanel(this, _namePrefix + "RcvdPanel");
            _rcvdPanel.size = new Vector2(_panelWidth, _chartSize.y);
            _rcvdPanel.autoLayout = true;
            _rcvdPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            _rcvdPanel.autoLayoutStart = LayoutStart.TopRight;
            _rcvdPanel.autoLayoutPadding = _padding;

            _rcvdLabel = UIUtils.CreateLabel(_rcvdPanel, _namePrefix + "RcvdLabel", null);
            _rcvdLabel.textAlignment = UIHorizontalAlignment.Right;
            _rcvdLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _rcvdLabel.autoSize = false;
            _rcvdLabel.size = new Vector2(_labelSize.x, _chartSize.y);

            UIPanel rcvdStatPanel = UIUtils.CreatePanel(this, _namePrefix + "RcvdStatPanel");
            rcvdStatPanel.size = new Vector2(_panelWidth, _statPanelHeight);
            rcvdStatPanel.autoLayout = true;
            rcvdStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            rcvdStatPanel.autoLayoutStart = LayoutStart.TopRight;
            rcvdStatPanel.autoLayoutPadding = _padding;

            _sentPanel = UIUtils.CreatePanel(this, _namePrefix + "SentPanel");
            _sentPanel.size = new Vector2(_panelWidth, _chartSize.y);
            _sentPanel.autoLayout = true;
            _sentPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            _sentPanel.autoLayoutStart = LayoutStart.TopRight;
            _sentPanel.autoLayoutPadding = _padding;

            _sentLabel = UIUtils.CreateLabel(_sentPanel, _namePrefix + "SentLabel", null);
            _sentLabel.textAlignment = UIHorizontalAlignment.Right;
            _sentLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _sentLabel.autoSize = false;
            _sentLabel.size = new Vector2(_labelSize.x, _chartSize.y);

            UIPanel sentStatPanel = UIUtils.CreatePanel(this, _namePrefix + "SentStatPanel");
            sentStatPanel.size = new Vector2(_panelWidth, _statPanelHeight);
            sentStatPanel.autoLayout = true;
            sentStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            sentStatPanel.autoLayoutStart = LayoutStart.TopRight;
            sentStatPanel.autoLayoutPadding = _padding;

            InitializeCharts(sentStatPanel, rcvdStatPanel);
            FitChildren(new Vector2(_padding.top, _padding.left));

            // Load the locale and update it if game locale changes
            UpdateLocale();
            LocaleManager.eventLocaleChanged += UpdateLocale;
        }

        private void InitializeCharts(UIPanel sentStatPanel, UIPanel rcvdStatPanel)
        {
            CargoUIChart chart;
            UILabel label;
            for (int i = 0; i < 6; i++)
            {
                chart = UIUtils.CreateCargoGroupedResourceChart(i > 2 ? _sentPanel : _rcvdPanel, _namePrefix + "GroupedResourceChart" + i);
                label = UIUtils.CreateLabel(i > 2 ? sentStatPanel : rcvdStatPanel, _namePrefix + "GroupedResourceLabel" + i, null);
                label.autoSize = false;
                label.size = new Vector2(chart.size.x, _statPanelHeight);
                label.textScale = 0.8f;
                label.textColor = _unitColor;
                label.textAlignment = UIHorizontalAlignment.Center;
                switch (i)
                {
                    case 0:
                        chart.Initialize(true, ResourceDestinationType.Local, label);
                        break;
                    case 1:
                        chart.Initialize(true, ResourceDestinationType.Import, label);
                        break;
                    case 2:
                        chart.Initialize(true, ResourceDestinationType.Export, label);
                        break;
                    case 3:
                        chart.Initialize(false, ResourceDestinationType.Local, label);
                        break;
                    case 4:
                        chart.Initialize(false, ResourceDestinationType.Import, label);
                        break;
                    case 5:
                        chart.Initialize(false, ResourceDestinationType.Export, label);
                        break;
                }

                _labels.Add(label);
                _charts.Add(chart);
            }
        }
    }
}
