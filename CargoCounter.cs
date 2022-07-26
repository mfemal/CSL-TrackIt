 using System.Text;
using UnityEngine;
using ColossalFramework.UI;
using ICities;
using TrackIt.API;

namespace TrackIt
{
    public class CargoCounter : ThreadingExtensionBase
    {
        private CargoUIPanel cargoPanel;
        private UILabel statsLabel;
        private UIPanel rightPanel;

        public override void OnCreated(IThreading threading)
        {
            base.OnCreated(threading);

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
                    if (DataManager.instance.TryGetEntry(WorldInfoPanel.GetCurrentInstanceID().Building, out _))
                    {
                        cargoPanel.Show();
                    }
                };
                rightPanel.eventClicked += showDelegate;
            }

        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (!WorldInfoPanel.AnyWorldInfoPanelOpen())
                return;

            UpdateBuildingInfoPanel();

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }

        public void UpdateBuildingInfoPanel()
        {
            InstanceID instanceID = WorldInfoPanel.GetCurrentInstanceID();

            if (statsLabel != null)
            {
                CargoStats2 stats;
                if (instanceID.Building != 0 && DataManager.instance.TryGetEntry(instanceID.Building, out stats))
                {
                    var sb = new StringBuilder();
                    sb.AppendFormat(
                        "{0}: {1:0}",
                        Localization.Get("TRUCKS_RCVD"),
                        stats.CountResourcesReceived());
                    sb.AppendLine();
                    sb.AppendFormat(
                        "{0}: {1:0}",
                        Localization.Get("TRUCKS_SENT"),
                        stats.CountResourcesSent());
                    sb.AppendLine();
                    sb.Append(Localization.Get("CLICK_MORE"));
                    statsLabel.text = sb.ToString();
                }
            }
        }
    }
}
