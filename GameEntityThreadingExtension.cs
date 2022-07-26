﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.UI;
using ICities;

namespace CargoInfoMod
{
    public class GameEntityThreadingExtension : ThreadingExtensionBase
    {

        /// <summary>
        /// Called once per rendered frame.
        /// </summary>
        /// <param name="realTimeDelta">Seconds since previous frame</param>
        /// <param name="simulationTimeDelta">smoothly interpolated to be used from main thread. On normal speed it is roughly same as realTimeDelta</param>
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {

        }
    }
}