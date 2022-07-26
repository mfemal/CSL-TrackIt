using System.Collections.Generic;
using System.Linq;
using ColossalFramework.UI;
using UnityEngine;
using TrackIt.API;

namespace TrackIt
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

        public UICargoChart()
        {
            size = new Vector2(90, 90);
            IList<ResourceCategoryType> standardGroups = UIUtils.CargoBasicResourceGroups;
            for (int i = 0; i < standardGroups.Count; i++)
            {
                AddSlice();

                SliceSettings settings = GetSlice(i);
                switch (standardGroups[i])
                {
                    case ResourceCategoryType.Oil:
                        // oil color is very dark so make it a little lighter
                        settings.innerColor = settings.outterColor = UIUtils.GetResourceCategoryColor(ResourceCategoryType.Oil) * 1.5f;
                        break;
                    default:
                        settings.innerColor = settings.outterColor = UIUtils.GetResourceCategoryColor(standardGroups[i]);
                        break;
                }
            }
            SetValues(new float[standardGroups.Count]);
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
        public int SetValues(CargoStats2 stats)
        {
            IList<ResourceCategoryType> standardGroups = UIUtils.CargoBasicResourceGroups;
            float[] values = new float[standardGroups.Count];
            int total = Sent ? stats.TotalResourcesSent(ResourceDestinationType) : stats.TotalResourcesReceived(ResourceDestinationType);
            for (int i = 0; i < standardGroups.Count; i++) // preserve order for colors
            {
                if (total > 0)
                {
                    values[i] = Sent ? stats.TotalResourcesSent(standardGroups[i], ResourceDestinationType) :
                        stats.TotalResourcesReceived(standardGroups[i], ResourceDestinationType);
                    values[i] /= total;
                }
                else
                {
                    values[i] = 0;
                }
            }
            SetValues(values);
            UpdateTotalText(total);
            return total;
        }

        public int SetValues(IDictionary<ResourceCategoryType, int> dict)
        {
            IList<ResourceCategoryType> standardGroups = UIUtils.CargoBasicResourceGroups;
            float[] values = new float[standardGroups.Count];
            int total = dict.Select(kv => kv.Value).ToList().Sum();
            for (int i = 0; i < standardGroups.Count; i++) // preserve order for colors
            {
                if (total > 0 && dict.ContainsKey(standardGroups[i]))
                {
                    values[i] = dict[standardGroups[i]] / total;
                }
                else
                {
                    values[i] = 0;
                }
            }
            SetValues(values);
            UpdateTotalText(total);
            return total;
        }

        /// <summary>
        /// Show a localized value for either the tooltip (default) or within the TotalLabel (if set)
        /// </summary>
        /// <param name="total">Value calculated for the total.</param>
        private void UpdateTotalText(int total)
        {
            // Avoid showing a 0 integer, yet reflect data in the graph, if the total is "small" (perserve unit of measure kilo)
            string t = total > 0 && total < 1000 ?
                string.Format("{0:0.000#}{1}", total / 1000.0f, Localization.Get("KILO_UNITS")) :
                string.Format("{0:0}{1}", Mathf.Ceil(total / 1000.0f), Localization.Get("KILO_UNITS"));
            if (TotalLabel != null)
            {
                TotalLabel.text = t;
            }
            else
            {
                tooltip = t;
            }
        }
    }
}
