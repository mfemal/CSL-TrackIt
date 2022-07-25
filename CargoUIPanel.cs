using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using UnityEngine;
using CargoInfoMod.Data;

namespace CargoInfoMod
{
    class CargoUIPanel : UIPanel
    {
        private const int _width = 384;
        private const int _handleHeight = 40;
        private readonly Vector2 _labelSize = new Vector2(90, 20);
        private const int _statPanelHeight = 30;
        private readonly Vector2 _exitButtonSize = new Vector2(32, 32);
        private readonly Vector2 _modeButtonSize = new Vector2(32, 10);
        private readonly Vector2 _chartSize = new Vector2(90, 90);
        private readonly RectOffset _padding = new RectOffset(2, 2, 2, 2);
        private readonly Color32 _unitColor = new Color32(206, 248, 0, 255);

        private ModInfo _mod;

        private bool _displayCurrent;
        private ushort _lastSelectedBuilding;

        private List<UICargoChart> _charts = new List<UICargoChart>();
        private List<UILabel> _labels = new List<UILabel>();
        private UILabel _windowLabel, _localLabel, _importLabel, _exportLabel, _rcvdLabel, _sentLabel;
        private UIButton _resetButton, _modeButton;
        private UIPanel _sentPanel, _rcvdPanel;

        public CargoUIPanel()
        {
            _mod = PluginManager.instance.FindPluginInfo(Assembly.GetExecutingAssembly()).userModInstance as ModInfo;
        }

        public override void Awake()
        {
            backgroundSprite = "MenuPanel2";
            opacity = 0.9f;

            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;

            var handle = AddUIComponent<UIDragHandle>();
            handle.size = new Vector2(_width, _handleHeight);

            _windowLabel = handle.AddUIComponent<UILabel>();
            _windowLabel.anchor = UIAnchorStyle.CenterVertical | UIAnchorStyle.CenterHorizontal;

            var closeButton = UIUtils.CreateButton(handle);
            closeButton.size = _exitButtonSize;
            closeButton.relativePosition = new Vector3(_width - _exitButtonSize.x, 0, 0);
            closeButton.anchor = UIAnchorStyle.Top | UIAnchorStyle.Right;
            closeButton.normalBgSprite = "buttonclose";
            closeButton.pressedBgSprite = "buttonclosepressed";
            closeButton.hoveredBgSprite = "buttonclosehover";
            closeButton.eventClicked += (sender, e) => Hide();

            var labelPanel = UIUtils.CreatePanel(this, "TrackItCargoUIPanelLabel");
            labelPanel.size = new Vector2(_width, _labelSize.y);
            labelPanel.autoLayout = true;
            labelPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            labelPanel.autoLayoutStart = LayoutStart.TopRight;
            labelPanel.autoLayoutPadding = _padding;

            _localLabel = UIUtils.CreateLabel(labelPanel, "TrackItCargoUIPanelLocalLabel", Localization.Get("LOCAL"));
            _localLabel.autoSize = false;
            _localLabel.size = _labelSize;
            _localLabel.textAlignment = UIHorizontalAlignment.Center;

            _importLabel = UIUtils.CreateLabel(labelPanel, "TrackItCargoUIPanelImportLabel", Localization.Get("IMPORT"));
            _importLabel.autoSize = false;
            _importLabel.size = _labelSize;
            _importLabel.textAlignment = UIHorizontalAlignment.Center;

            _exportLabel = UIUtils.CreateLabel(labelPanel, "TrackItCargoUIPanelExportLabel", Localization.Get("EXPORT"));
            _exportLabel.autoSize = false;
            _exportLabel.size = _labelSize;
            _exportLabel.textAlignment = UIHorizontalAlignment.Center;

            _rcvdPanel = UIUtils.CreatePanel(this, "TrackItCargoUIPanelReceivedPanel");
            _rcvdPanel.size = new Vector2(_width, _chartSize.y);
            _rcvdPanel.autoLayout = true;
            _rcvdPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            _rcvdPanel.autoLayoutStart = LayoutStart.TopRight;
            _rcvdPanel.autoLayoutPadding = _padding;

            _rcvdLabel = UIUtils.CreateLabel(_rcvdPanel, "TrackItCargoUIPanelRcvdLabel", null);
            _rcvdLabel.textAlignment = UIHorizontalAlignment.Right;
            _rcvdLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _rcvdLabel.autoSize = false;
            _rcvdLabel.size = new Vector2(_labelSize.x, _chartSize.y);

            var rcvdStatPanel = UIUtils.CreatePanel(this, "TrackItCargoUIPanelRcvdStatPanel");
            rcvdStatPanel.size = new Vector2(_width, _statPanelHeight);
            rcvdStatPanel.autoLayout = true;
            rcvdStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            rcvdStatPanel.autoLayoutStart = LayoutStart.TopRight;
            rcvdStatPanel.autoLayoutPadding = _padding;

            _sentPanel = UIUtils.CreatePanel(this, "TrackItCargoUIPanelSentPanel");
            _sentPanel.size = new Vector2(_width, _chartSize.y);
            _sentPanel.autoLayout = true;
            _sentPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            _sentPanel.autoLayoutStart = LayoutStart.TopRight;
            _sentPanel.autoLayoutPadding = _padding;

            _sentLabel = UIUtils.CreateLabel(_sentPanel, "TrackItCargoUIPanelSentLabel", null);
            _sentLabel.textAlignment = UIHorizontalAlignment.Right;
            _sentLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _sentLabel.autoSize = false;
            _sentLabel.size = new Vector2(_labelSize.x, _chartSize.y);

            var sentStatPanel = UIUtils.CreatePanel(this, "TrackItCargoUIPanelSentStatPanel");
            sentStatPanel.size = new Vector2(_width, _statPanelHeight);
            sentStatPanel.autoLayout = true;
            sentStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            sentStatPanel.autoLayoutStart = LayoutStart.TopRight;
            sentStatPanel.autoLayoutPadding = _padding;

            _resetButton = UIUtils.CreateButton(sentStatPanel);
            _resetButton.text = "Res";
            _resetButton.textScale = 0.6f;
            _resetButton.autoSize = false;
            _resetButton.size = _modeButtonSize;

            _resetButton.eventClicked += (sender, e) =>
            {
                if (!_mod.data.TryGetEntry(WorldInfoPanel.GetCurrentInstanceID().Building, out CargoStats2 stats)) return;
                Array.Clear(stats.CarsCounted, 0, stats.CarsCounted.Length);
            };

            _modeButton = UIUtils.CreateButton(sentStatPanel);
            _modeButton.text = "Prev";
            _modeButton.textScale = 0.6f;
            _modeButton.autoSize = false;
            _modeButton.size = _modeButtonSize;

            _modeButton.eventClicked += (sender, e) =>
            {
                _displayCurrent = !_displayCurrent;
                _modeButton.text = _displayCurrent ? "Cur" : "Prev";
                _modeButton.tooltip = _displayCurrent
                    ? Localization.Get("SWITCH_MODES_TOOLTIP_CUR")
                    : Localization.Get("SWITCH_MODES_TOOLTIP_PREV");
                _modeButton.RefreshTooltip();
            };

            InitializeCharts(sentStatPanel, rcvdStatPanel);
            FitChildren(new Vector2(_padding.top, _padding.left));

            // Load the locale and update it if game locale changes
            UpdateLocale();
            LocaleManager.eventLocaleChanged += UpdateLocale;

            base.Awake();
        }

        public void UpdateLocale()
        {
            _windowLabel.text = Localization.Get("STATS_WINDOW_LABEL");
            _localLabel.text = Localization.Get("LOCAL");
            _importLabel.text = Localization.Get("IMPORT");
            _exportLabel.text = Localization.Get("EXPORT");
            _rcvdLabel.text = Localization.Get("RECEIVED");
            _sentLabel.text = Localization.Get("SENT");
            _modeButton.tooltip = _displayCurrent ?
                Localization.Get("SWITCH_MODES_TOOLTIP_CUR") :
                Localization.Get("SWITCH_MODES_TOOLTIP_PREV");
            _resetButton.tooltip = Localization.Get("RESET_COUNTERS_TOOLTIP");

            UpdateCounterValues();
        }

        public void UpdateCounterValues()
        {
            if (_mod.data.TryGetEntry(_lastSelectedBuilding, out CargoStats2 stats) && _charts.Count > 0)
            {
                foreach (UICargoChart chart in _charts)
                {
                    chart.UpdateValues(stats);
                }
            }
        }

        public override void Start()
        {
            base.Start();

            canFocus = true;
            isInteractive = true;

            Hide();
        }

        public override void Update()
        {
            if (!isVisible) return;

            if (_mod?.data == null) return;

            if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                _lastSelectedBuilding = WorldInfoPanel.GetCurrentInstanceID().Building;

            UpdateCounterValues();
        }

        private void InitializeCharts(UIPanel sentStatPanel, UIPanel rcvdStatPanel)
        {
            UICargoChart chart;
            UILabel label;
            for (int i = 0; i < 6; i++)
            {
                chart = UIUtils.CreateResourceRadialChart(i > 2 ? _sentPanel : _rcvdPanel, "TrackItCargoUIPanelResourceRadialChart" + i);
                label = UIUtils.CreateLabel(i > 2 ? sentStatPanel : rcvdStatPanel, "TrackItCargoUIPanelResourceLabel" + i, null);
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
