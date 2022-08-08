using System;
using System.Collections.Generic;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using UnityEngine;
using TrackIt.API;

namespace TrackIt.UI
{
    class CargoUIPanel : UIPanel
    {
        private const string _namePrefix = "CargoPanel";
        private const int _panelWidth = 472;
        private const int _chartPanelHeight = 30;
        private const int _legendPanelHeight = 120;
        private const float _labelTextScale = 0.8125f; // Game constant somewhere? value pulled from mod tools

        private const int _chartWidth = 90;
        private readonly Vector2 _labelSize = new Vector2(_chartWidth, 20);
        private readonly RectOffset _padding = new RectOffset(10, 10, 10, 10);
        private readonly Color32 _unitColor = new Color32(206, 248, 0, 255);

        private List<CargoUIChart> _charts = new List<CargoUIChart>();
        private List<UILabel> _labels = new List<UILabel>();
        private UILabel _columnLocalLabel, _columnImportLabel, _columnExportLabel;
        private UILabel _rowReceivedLabel, _rowSentLabel;
        private UIPanel _sentPanel, _receivedPanel;

        public void UpdateLocale()
        {
            _columnLocalLabel.text = Localization.Get("LOCAL");
            _rowReceivedLabel.text = Localization.Get("RECEIVED");
            _rowSentLabel.text = Localization.Get("SENT");
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
            name = UIUtils.ConstructComponentName(_namePrefix);
            width = _panelWidth;
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(10, 10, 4, 4);

            UIPanel summaryPanelHeader = CreateUIHeaderPanel();
            _columnLocalLabel = CreateUIColumnLabel(summaryPanelHeader, "LocalLabel", "LOCAL");
            _columnImportLabel = CreateUIColumnLabel(summaryPanelHeader, "ImportLabel", "INFO_CONNECTIONS_IMPORT");
            _columnExportLabel = CreateUIColumnLabel(summaryPanelHeader, "ExportLabel", "INFO_CONNECTIONS_EXPORT");

            UIPanel receivedPanel = CreateUIRowPanel("ReceivedPanel", ref _receivedPanel, "ReceivedChartsPanel", ref _rowReceivedLabel, "ReceivedLabel");
            UIPanel sentPanel = CreateUIRowPanel("SentPanel", ref _sentPanel, "SentChartsPanel", ref _rowSentLabel, "SentLabel");

            InitializeCharts(sentPanel, receivedPanel);
            InitializeLegend();
            InitializeStyle();

            // Load the locale and update it if game locale changes
            UpdateLocale();
            LocaleManager.eventLocaleChanged += UpdateLocale;
        }

        private UILabel CreateUIColumnLabel(UIComponent parent, string name, string localeID)
        {
            UILabel label = UIUtils.CreateLabel(parent, _namePrefix + name, localeID);
            label.autoSize = false;
            label.size = _labelSize;
            label.textAlignment = UIHorizontalAlignment.Center;
            return label;
        }

        private UIPanel CreateUIHeaderPanel()
        {
            UIPanel summaryPanelHeader = UIUtils.CreatePanel(this, _namePrefix + "Header");
            summaryPanelHeader.size = new Vector2(_panelWidth, _labelSize.y);
            summaryPanelHeader.autoLayout = true;
            summaryPanelHeader.autoLayoutDirection = LayoutDirection.Horizontal;
            summaryPanelHeader.autoLayoutStart = LayoutStart.TopRight;
            summaryPanelHeader.autoLayoutPadding = _padding;
            return summaryPanelHeader;
        }

        private UIPanel CreateUIRowPanel(string name, ref UIPanel panel, string panelName, ref UILabel label, string labelName)
        {
            panel = UIUtils.CreatePanel(this, _namePrefix + panelName);
            panel.size = new Vector2(_panelWidth, _chartWidth);
            panel.autoLayout = true;
            panel.autoLayoutDirection = LayoutDirection.Horizontal;
            panel.autoLayoutStart = LayoutStart.TopRight;
            panel.autoLayoutPadding = _padding;

            label = UIUtils.CreateLabel(panel, _namePrefix + labelName, null);
            label.textAlignment = UIHorizontalAlignment.Right;
            label.verticalAlignment = UIVerticalAlignment.Middle;
            label.autoSize = false;
            label.size = new Vector2(_labelSize.x, _chartWidth);

            UIPanel rowPanel = UIUtils.CreatePanel(this, _namePrefix + name);
            rowPanel.size = new Vector2(_panelWidth, _chartPanelHeight);
            rowPanel.autoLayout = true;
            rowPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            rowPanel.autoLayoutStart = LayoutStart.TopRight;
            rowPanel.autoLayoutPadding = _padding;
            return rowPanel;
        }

        private void InitializeCharts(UIPanel sentStatPanel, UIPanel rcvdStatPanel)
        {
            CargoUIChart chart;
            UILabel label;
            UIPanel parentPanel, chartPanel;
            for (int i = 0; i < 6; i++)
            {
                parentPanel = i > 2 ? _sentPanel : _receivedPanel;
                chartPanel = i > 2 ? sentStatPanel : rcvdStatPanel;
                chart = UIUtils.CreateCargoGroupedResourceChart(parentPanel, _namePrefix + parentPanel.name + "Chart" + i);
                label = UIUtils.CreateLabel(chartPanel, chartPanel.name + "ChartLabel" + i, null);
                label.autoSize = false;
                label.size = new Vector2(chart.size.x, _chartPanelHeight);
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

        // TODO: resize this better based on non-English languages
        private void InitializeLegend()
        {
            UIPanel legendPanel = UIUtils.CreatePanel(this, _namePrefix + "Legend");
            legendPanel.width = _panelWidth - autoLayoutPadding.left - autoLayoutPadding.right;
            legendPanel.autoLayout = true;
            legendPanel.autoLayoutDirection = LayoutDirection.Vertical;
            legendPanel.backgroundSprite = "GenericPanel";
            legendPanel.height = _legendPanelHeight;
            legendPanel.padding = new RectOffset(0, 0, 4, 4);

            float legendPanelWidth = _panelWidth - autoLayoutPadding.left - autoLayoutPadding.right - legendPanel.padding.left - legendPanel.padding.right;
            UILabel legendTitle = UIUtils.CreateLabel(legendPanel, _namePrefix + "LegendTitle", "INFO_LEGEND");
            legendTitle.autoHeight = true;
            legendTitle.font = UIUtils.GetUIFont("OpenSans-Regular");
            legendTitle.textAlignment = UIHorizontalAlignment.Center;
            legendTitle.width = legendPanelWidth;
            legendTitle.textScale = 0.75f;

            CargoUILegend legend = legendPanel.AddUIComponent<CargoUILegend>();
            legend.autoLayoutPadding = _padding;
            legend.width = legendPanelWidth;
            legend.height = legendPanel.height - legendTitle.height - legendPanel.padding.top - legendPanel.padding.bottom;
            legend.wrapLayout = true;
        }

        private void InitializeStyle()
        {
            UIFont font = UIUtils.GetUIFont("OpenSans-Semibold");
            if (font == null)
            {
                return;
            }
            _columnLocalLabel.font = font;
            _columnLocalLabel.textScale = _labelTextScale;
            _columnImportLabel.font = font;
            _columnImportLabel.textScale = _labelTextScale;
            _columnExportLabel.font = font;
            _columnExportLabel.textScale = _labelTextScale;
            _rowReceivedLabel.font = font;
            _rowReceivedLabel.textScale = _labelTextScale;
            _rowSentLabel.font = font;
            _rowSentLabel.textScale = _labelTextScale;
        }
    }
}
