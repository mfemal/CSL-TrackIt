using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using CargoInfoMod.Data;
using System.Collections.Generic;

namespace CargoInfoMod
{
    public class CargoCounter : ThreadingExtensionBase
    {
        private ModInfo mod;

        private UICargoChart vehicleCargoChart;
        private CargoUIPanel cargoPanel;
        private UILabel statsLabel;
        private UIPanel rightPanel;

        private ushort _cachedVehicleID;

        public override void OnCreated(IThreading threading)
        {
            base.OnCreated(threading);
            mod = PluginManager.instance.FindPluginInfo(Assembly.GetExecutingAssembly()).userModInstance as ModInfo;

            if (LoadingManager.instance.m_loadingComplete)
            {
                // Mod reloaded while running the game, should set up again
#if DEBUG
                LogUtil.LogInfo("ThreadingExtension created while running");
#endif
                OnLevelLoaded(SimulationManager.UpdateMode.LoadGame);
            }
            LoadingManager.instance.m_levelLoaded += OnLevelLoaded;
            LoadingManager.instance.m_levelPreUnloaded += OnLevelUnloaded;
        }

        private MouseEventHandler showDelegate;

        public override void OnReleased()
        {
            OnLevelUnloaded();
            // TODO: Unapply Harmony patches once the feature is available
            base.OnReleased();
        }

        private void OnLevelLoaded(SimulationManager.UpdateMode updateMode)
        {
            OnLevelUnloaded();

            if (updateMode != SimulationManager.UpdateMode.NewGameFromMap &&
                updateMode != SimulationManager.UpdateMode.NewGameFromScenario &&
                updateMode != SimulationManager.UpdateMode.LoadGame)
                return;

            mod.data.Setup();

            SetupUIBindings();
        }

        private void OnLevelUnloaded()
        {
#if DEBUG
            LogUtil.LogInfo("Cleaning up UI...");
#endif
            if (rightPanel != null)
                rightPanel.eventClicked -= showDelegate;
            if (cargoPanel != null)
                GameObject.Destroy(cargoPanel);
            if (vehicleCargoChart != null)
                GameObject.Destroy(vehicleCargoChart);
        }

        private void SetupUIBindings()
        {
#if DEBUG
            LogUtil.LogInfo("Setting up UI...");
#endif
            cargoPanel = (CargoUIPanel)UIView.GetAView().AddUIComponent(typeof(CargoUIPanel));

            UIPanel cityServiceWorldInfoPanel = UIHelper.GetPanel("(Library) CityServiceWorldInfoPanel"); //UIUtils.GetGameUIPanel<CityServiceWorldInfoPanel>("(Library) CityServiceWorldInfoPanel");
            statsLabel = cityServiceWorldInfoPanel?.Find<UILabel>("Desc");
            rightPanel = cityServiceWorldInfoPanel?.Find<UIPanel>("Right");
            if (cityServiceWorldInfoPanel == null)
                LogUtil.LogError("CityServiceWorldInfoPanel not found");
            if (rightPanel == null)
                LogUtil.LogError("ServicePanel.Right panel not found");
            else
            {
#if DEBUG
                LogUtil.LogInfo("ServicePanel.Right panel found!");
#endif
                showDelegate = (sender, e) =>
                {
                    if (mod.data.TryGetEntry(WorldInfoPanel.GetCurrentInstanceID().Building, out _))
                    {
                        cargoPanel.Show();
                    }
                };
                rightPanel.eventClicked += showDelegate;
            }

            UIPanel cityServiceVehicleWorldInfoPanel = UIHelper.GetPanel("(Library) CityServiceVehicleWorldInfoPanel"); // UIUtils.GetGameUIPanel<CityServiceVehicleWorldInfoPanel>("(Library) CityServiceVehicleWorldInfoPanel");
            var vehiclePanel = cityServiceVehicleWorldInfoPanel?.Find<UIPanel>("Panel");
            if (vehiclePanel != null)
            {
                vehiclePanel.autoLayout = false;
                vehicleCargoChart = UIUtils.CreateCargoGroupedResourceChart(vehiclePanel, "TrackItCargoUIPanelResourceRadialChart");
                vehicleCargoChart.size = new Vector2(60, 60);
                vehicleCargoChart.relativePosition = new Vector3(330, 0);
            }
            else
            {
                LogUtil.LogError("CityServiceVehicleWorldInfoPanel not found!");
            }
        }

        private DateTime lastReset = DateTime.MinValue;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            DateTime tempDateTime = SimulationManager.instance.m_currentGameTime;
            if ((mod.Options.UpdateHourly && lastReset.Hour < tempDateTime.Hour) ||
                (!mod.Options.UpdateHourly && lastReset < tempDateTime && tempDateTime.Day == 1))
            {
                lastReset = tempDateTime;
                //mod.data.UpdateCounters();
#if DEBUG
                LogUtil.LogInfo("Monthly counter values updated");
#endif
            }

            if (!WorldInfoPanel.AnyWorldInfoPanelOpen())
                return;

            UpdateBuildingInfoPanel();
            UpdateVehicleInfoPanel();

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }

        private void UpdateVehicleInfoPanel()
        {
            var vehicleID = WorldInfoPanel.GetCurrentInstanceID().Vehicle;
            if (vehicleID != 0 && vehicleCargoChart != null && _cachedVehicleID != vehicleID)
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
                    vehicleCargoChart.SetValues(vehicleCargoCategoryTotals);
                    vehicleCargoChart.isVisible = true;
                }
                else
                {
                    vehicleCargoChart.isVisible = false;
                }
            }
        }

        public void UpdateBuildingInfoPanel()
        {
            InstanceID instanceID = WorldInfoPanel.GetCurrentInstanceID();

            if (statsLabel != null)
            {
                CargoStats2 stats;
                if (instanceID.Building != 0 && mod.data.TryGetEntry(instanceID.Building, out stats))
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat(
                        "{0}: {1:0}",
                        Localization.Get(mod.Options.UseMonthlyValues ? "TRUCKS_RCVD_LAST_MONTH" : "TRUCKS_RCVD_LAST_WEEK"),
                        stats.CountResourcesReceived());
                    sb.AppendLine();
                    sb.AppendFormat(
                        "{0}: {1:0}",
                        Localization.Get(mod.Options.UseMonthlyValues ? "TRUCKS_SENT_LAST_MONTH" : "TRUCKS_SENT_LAST_WEEK"),
                        stats.CountResourcesSent());
                    sb.AppendLine();
                    sb.Append(Localization.Get("CLICK_MORE"));
                    statsLabel.text = sb.ToString();
                }
            }
        }
    }
}
