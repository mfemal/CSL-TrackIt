using System;
using ICities;
using UnityEngine;

namespace TrackIt
{
    /// <summary>
    /// Handle the game loading and unloading events.
    /// </summary>
    /// <remarks>Using pause in game does not use this extension.</remarks>
    public class LoadingExtension : LoadingExtensionBase
    {
        // Allowed modes for this mod, others are ignored and no initialization should be done.
        private LoadMode[] _allowedLoadModes = new LoadMode[] {
            LoadMode.NewGame,
            LoadMode.NewGameFromScenario,
            LoadMode.LoadGame,
            LoadMode.LoadScenario
        };
        private LoadMode _loadMode;      // track the state in OnLevelLoaded so cleanup can be done
        private GameObject _rootObject;  // easily found in debug tools and allows monitors to support unity script events

        /// <summary>
        /// Invoked when a level has completed the loading process. This mod only runs when games are loaded for playing.
        /// </summary>
        /// <param name="mode">defines what kind of level was just loaded</param>
        public override void OnLevelLoaded(LoadMode mode)
        {
            _loadMode = mode;

            try
            {
                if (!IsModeAllowed(mode))
                {
                    return;
                }
                DataManager.instance.Initialize();

                _rootObject = new GameObject(ModInfo.NamespacePrefix + "Manager");
                _rootObject.AddComponent<VehicleWorldInfoPanelMonitor>();
                _rootObject.AddComponent<WorldInfoPanelMonitor>();
            }
            catch (Exception e)
            {
                LogUtil.LogException(e);
            }
        }

        /// <summary>
        /// Invoked when the level is unloading (typically when going back to the main menu or prior to loading a new level)
        /// </summary>
        public override void OnLevelUnloading()
        {
            try
            {
                if (!IsModeAllowed(_loadMode))
                {
                    return;
                }
                DataManager.instance.Clear();
                if (_rootObject != null)
                {
                    foreach (Transform child in _rootObject.transform)
                    {
                        UnityEngine.Object.Destroy(child.gameObject);
                    }
                    UnityEngine.Object.Destroy(_rootObject);
                }
            }
            catch (Exception e)
            {
                LogUtil.LogException(e);
            }
        }

        private bool IsModeAllowed(LoadMode mode)
        {
            LoadMode? lm = Array.Find(_allowedLoadModes, m => mode == m);
            return lm.HasValue;
        }
    }
}
