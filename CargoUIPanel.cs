﻿using System.Collections.Generic;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using UnityEngine;
using TrackIt.API;

namespace TrackIt
{
    class CargoUIPanel : UIPanel
    {
        private const int _width = 384;
        private const int _handleHeight = 40;
        private readonly Vector2 _labelSize = new Vector2(90, 20);
        private const int _statPanelHeight = 30;
        private readonly Vector2 _exitButtonSize = new Vector2(32, 32);
        private readonly Vector2 _chartSize = new Vector2(90, 90);
        private readonly RectOffset _padding = new RectOffset(2, 2, 2, 2);
        private readonly Color32 _unitColor = new Color32(206, 248, 0, 255);

        private ushort _lastSelectedBuilding;

        private List<CargoUIChart> _charts = new List<CargoUIChart>();
        private List<UILabel> _labels = new List<UILabel>();
        private UILabel _windowLabel, _localLabel, _importLabel, _exportLabel, _rcvdLabel, _sentLabel;
        private UIPanel _sentPanel, _rcvdPanel;

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

            var labelPanel = UIUtils.CreatePanel(this, "CargoUIPanelLabel");
            labelPanel.size = new Vector2(_width, _labelSize.y);
            labelPanel.autoLayout = true;
            labelPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            labelPanel.autoLayoutStart = LayoutStart.TopRight;
            labelPanel.autoLayoutPadding = _padding;

            _localLabel = UIUtils.CreateLabel(labelPanel, "CargoUIPanelLocalLabel", Localization.Get("LOCAL"));
            _localLabel.autoSize = false;
            _localLabel.size = _labelSize;
            _localLabel.textAlignment = UIHorizontalAlignment.Center;

            _importLabel = UIUtils.CreateLabel(labelPanel, "CargoUIPanelImportLabel", Localization.Get("IMPORT"));
            _importLabel.autoSize = false;
            _importLabel.size = _labelSize;
            _importLabel.textAlignment = UIHorizontalAlignment.Center;

            _exportLabel = UIUtils.CreateLabel(labelPanel, "CargoUIPanelExportLabel", Localization.Get("EXPORT"));
            _exportLabel.autoSize = false;
            _exportLabel.size = _labelSize;
            _exportLabel.textAlignment = UIHorizontalAlignment.Center;

            _rcvdPanel = UIUtils.CreatePanel(this, "CargoUIPanelReceivedPanel");
            _rcvdPanel.size = new Vector2(_width, _chartSize.y);
            _rcvdPanel.autoLayout = true;
            _rcvdPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            _rcvdPanel.autoLayoutStart = LayoutStart.TopRight;
            _rcvdPanel.autoLayoutPadding = _padding;

            _rcvdLabel = UIUtils.CreateLabel(_rcvdPanel, "CargoUIPanelRcvdLabel", null);
            _rcvdLabel.textAlignment = UIHorizontalAlignment.Right;
            _rcvdLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _rcvdLabel.autoSize = false;
            _rcvdLabel.size = new Vector2(_labelSize.x, _chartSize.y);

            var rcvdStatPanel = UIUtils.CreatePanel(this, "CargoUIPanelRcvdStatPanel");
            rcvdStatPanel.size = new Vector2(_width, _statPanelHeight);
            rcvdStatPanel.autoLayout = true;
            rcvdStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            rcvdStatPanel.autoLayoutStart = LayoutStart.TopRight;
            rcvdStatPanel.autoLayoutPadding = _padding;

            _sentPanel = UIUtils.CreatePanel(this, "CargoUIPanelSentPanel");
            _sentPanel.size = new Vector2(_width, _chartSize.y);
            _sentPanel.autoLayout = true;
            _sentPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            _sentPanel.autoLayoutStart = LayoutStart.TopRight;
            _sentPanel.autoLayoutPadding = _padding;

            _sentLabel = UIUtils.CreateLabel(_sentPanel, "CargoUIPanelSentLabel", null);
            _sentLabel.textAlignment = UIHorizontalAlignment.Right;
            _sentLabel.verticalAlignment = UIVerticalAlignment.Middle;
            _sentLabel.autoSize = false;
            _sentLabel.size = new Vector2(_labelSize.x, _chartSize.y);

            var sentStatPanel = UIUtils.CreatePanel(this, "CargoUIPanelSentStatPanel");
            sentStatPanel.size = new Vector2(_width, _statPanelHeight);
            sentStatPanel.autoLayout = true;
            sentStatPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            sentStatPanel.autoLayoutStart = LayoutStart.TopRight;
            sentStatPanel.autoLayoutPadding = _padding;

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

            UpdateCounterValues();
        }

        public void UpdateCounterValues()
        {
            if (DataManager.instance.TryGetBuilding(_lastSelectedBuilding, out CargoStats2 stats) && _charts.Count > 0)
            {
                foreach (CargoUIChart chart in _charts)
                {
                    chart.SetValues(stats);
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

            if (WorldInfoPanel.GetCurrentInstanceID().Building != 0)
                _lastSelectedBuilding = WorldInfoPanel.GetCurrentInstanceID().Building;

            UpdateCounterValues();
        }

        private void InitializeCharts(UIPanel sentStatPanel, UIPanel rcvdStatPanel)
        {
            CargoUIChart chart;
            UILabel label;
            for (int i = 0; i < 6; i++)
            {
                chart = UIUtils.CreateCargoGroupedResourceChart(i > 2 ? _sentPanel : _rcvdPanel, "CargoUIPanelResourceRadialChart" + i);
                label = UIUtils.CreateLabel(i > 2 ? sentStatPanel : rcvdStatPanel, "CargoUIPanelResourceLabel" + i, null);
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
