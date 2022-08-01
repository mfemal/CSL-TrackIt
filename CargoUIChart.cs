using System.Collections.Generic;
using System.Linq;
using ColossalFramework.UI;
using UnityEngine;
using TrackIt.API;

namespace TrackIt
{
    /// <summary>
    /// This class extends the base Radial Chart available with functionality to wrap setters based on type of cargo.
    /// Using fixed offset positions (slice count equal to number of standard list of categories) with 0 values for
    /// the resource category total causes odd run-time behaviour on first-time display. So setters manipulate the
    /// (protected) m_slices directly rather than using [Add|Get]Slice provided in UIRadialChart. This does prevent
    /// setting up the chart once (i.e. Start methods) and using offset indexes consistently to update resource
    /// category totals (i.e. using a total of 0 if that category is not relevant).
    /// </summary>
    public class CargoUIChart : UIRadialChart
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

        public CargoUIChart()
        {
            size = new Vector2(90, 90);
            spriteName = "PieChartBg";
        }

        /// <summary>
        /// Convenience method to initialize multiple values at once.
        /// </summary>
        /// <param name="sent">Whether the resource data is either sent, or received.</param>
        /// <param name="resourceDestinationType">Resource destination</param>
        /// <param name="totalLabel">The total value label, if set the numeric value is localized and units displayed.</param>
        internal void Initialize(bool sent, ResourceDestinationType resourceDestinationType, UILabel totalLabel)
        {
            Sent = sent;
            ResourceDestinationType = resourceDestinationType;
            TotalLabel = totalLabel;
        }

        /// <summary>
        /// Extract the data for the chart based on the provided paramters
        /// </summary>
        /// <param name="cargoStatistics">The cargo statistics determined from the index.</param>
        /// <param name="sent">Sent (true) or Received (false)</param>
        /// <param name="resourceDestinationType">The panel groups results by this value</param>
        internal int SetValues(CargoStatistics cargoStatistics)
        {
            m_Slices.Clear();

            IList<ResourceCategoryType> standardGroups = UIUtils.CargoBasicResourceGroups;
            List<float> values = new List<float>(standardGroups.Count);
            int total = Sent ? cargoStatistics.TotalResourcesSent(ResourceDestinationType) : cargoStatistics.TotalResourcesReceived(ResourceDestinationType);
            if (total > 0)
            {
                for (int i = 0; i < standardGroups.Count; i++)
                {
                    int categoryTotal = Sent ?
                            cargoStatistics.TotalResourcesSent(standardGroups[i], ResourceDestinationType) :
                            cargoStatistics.TotalResourcesReceived(standardGroups[i], ResourceDestinationType);
                    if (categoryTotal == 0)
                    {
                        continue;
                    }
                    m_Slices.Add(CreateSliceSettings(standardGroups[i]));
                    values.Add(Mathf.Clamp((float)categoryTotal / total , 0f, 1f));
                }
            }
            SetValues(values.ToArray());
            UpdateTotalText(total);

            return total;
        }

        internal int SetValues(IDictionary<ResourceCategoryType, int> dict)
        {
            m_Slices.Clear();

            IList<ResourceCategoryType> standardGroups = UIUtils.CargoBasicResourceGroups;
            List<float> values = new List<float>(standardGroups.Count);
            int total = dict.Select(kv => kv.Value).ToList().Sum();
            if (total > 0)
            {
                for (int i = 0; i < standardGroups.Count; i++)
                {
                    if (!dict.ContainsKey(standardGroups[i]))
                    {
                        continue;
                    }
                    m_Slices.Add(CreateSliceSettings(standardGroups[i]));
                    values.Add(Mathf.Clamp((float)dict[standardGroups[i]] / total, 0f, 1f));
                }
            }
            SetValues(values.ToArray());
            UpdateTotalText(total);

            return total;
        }

        private SliceSettings CreateSliceSettings(ResourceCategoryType resourceCategoryType)
        {
            SliceSettings settings = new SliceSettings();
            Color color = UIUtils.GetResourceCategoryColor(resourceCategoryType);
            switch (resourceCategoryType)
            {
                case ResourceCategoryType.Oil:
                    // oil color is very dark so make it a little lighter
                    settings.innerColor = settings.outterColor = color * 1.5f;
                    break;
                default:
                    settings.innerColor = settings.outterColor = color;
                    break;
            }
            return settings;
        }

        /// <summary>
        /// Show a localized value for either the tooltip (default) or within the TotalLabel (if set)
        /// </summary>
        /// <param name="total">Value calculated for the total.</param>
        private void UpdateTotalText(int total)
        {
            if (TotalLabel != null)
            {
                TotalLabel.text = UIUtils.FormatCargoValue(total);
            }
            else
            {
                tooltip = UIUtils.FormatCargoValue(total);
            }
        }
    }
}
