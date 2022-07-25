using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework.UI;
using UnityEngine;
using CargoInfoMod.Data;

namespace CargoInfoMod
{
    class UICargoChart : UIRadialChart
    {
        public UILabel TotalLabel;

        /// <summary>
        /// Data is either sent (value is true) or received (value is false)
        /// </summary>
        public bool Sent;

        /// <summary>
        /// The resource destination type used for grouping
        /// </summary>
        public ResourceDestinationType ResourceDestinationType;

        // Resource categories to include in the radial chart
        private static readonly List<ResourceCategoryType> s_resourceCategories;

        static UICargoChart()
        {
            s_resourceCategories = Enum.GetValues(typeof(ResourceCategoryType))
                .Cast<ResourceCategoryType>()
                .Except(new List<ResourceCategoryType>() { ResourceCategoryType.None })
                .ToList();
        }

        public UICargoChart()
        {
            size = new Vector2(90, 90);

            for (int i = 0; i < s_resourceCategories.Count; i++)
            {
                AddSlice();

                SliceSettings settings = GetSlice(i);
                switch (s_resourceCategories[i])
                {
                    case ResourceCategoryType.Oil:
                        // oil color is very dark so make it a little lighter
                        settings.innerColor = settings.outterColor = UIUtils.GetResourceCategoryColor(ResourceCategoryType.Oil) * 1.5f;
                        break;
                    default:
                        settings.innerColor = settings.outterColor = UIUtils.GetResourceCategoryColor(s_resourceCategories[i]);
                        break;
                }
            }
            SetValues(new float[] { 0.0f });
        }

        /// <summary>
        /// Convenience method to initialize multiple values.
        /// </summary>
        /// <param name="sent">Whether the resource data is either sent, or received.</param>
        /// <param name="resourceDestinationType">Resource destination</param>
        /// <param name="totalLabel">The total value label, if set the numeric value is localized and units displayed.</param>
        public void Initialize(bool sent, ResourceDestinationType resourceDestinationType, UILabel totalLabel)
        {
            Sent = sent;
            ResourceDestinationType = resourceDestinationType;
            TotalLabel = totalLabel;
        }

        /// <summary>
        /// Extract the data for the chart based on the provided paramters
        /// </summary>
        /// <param name="stats">The cargo statistics</param>
        /// <param name="sent">Sent (true) or Received (false)</param>
        /// <param name="resourceDestinationType">The panel groups results by this value</param>
        public int UpdateValues(CargoStats2 stats)
        {
            float[] values = new float[s_resourceCategories.Count];
            int total = Sent ? stats.TotalResourcesSent(ResourceDestinationType) : stats.TotalResourcesReceived(ResourceDestinationType);
            for (int i = 0; i < s_resourceCategories.Count; i++)
            {
                if (total > 0)
                {
                    values[i] = Sent ? stats.TotalResourcesSent(s_resourceCategories[i], ResourceDestinationType) :
                        stats.TotalResourcesReceived(s_resourceCategories[i], ResourceDestinationType);
                    values[i] /= total;
                }
                else
                {
                    values[i] = 0;
                }
            }
            SetValues(values);
            if (TotalLabel != null)
            {
                // Avoid showing a 0 integer, yet reflect data in the graph, if the total is "small" (perserve unit of measure kilo)
                TotalLabel.text = total > 0 && total < 1000 ?
                    string.Format("{0:0.000#}{1}", total / 1000.0f, Localization.Get("KILO_UNITS")) :
                    string.Format("{0:0}{1}", Mathf.Ceil(total / 1000.0f), Localization.Get("KILO_UNITS"));
            }
            return total;
        }
    }
}
