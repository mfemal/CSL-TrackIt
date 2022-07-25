using CargoInfoMod.Data;
using ColossalFramework.UI;
using UnityEngine;

namespace CargoInfoMod
{
    // Some Create methods are re-used or modified from the Building Themes mod (boformer); although some credits were
    // also noted in that mod to SamSamTS and AcidFire.
    internal class UIUtils
    {
        public static UIButton CreateButton(UIComponent parent)
        {
            UIButton button = parent.AddUIComponent<UIButton>();

            button.size = new Vector2(90f, 30f);
            button.textScale = 0.9f;
            button.normalBgSprite = "ButtonMenu";
            button.hoveredBgSprite = "ButtonMenuHovered";
            button.pressedBgSprite = "ButtonMenuPressed";
            button.disabledBgSprite = "ButtonMenuDisabled";
            button.disabledTextColor = new Color32(128, 128, 128, 255);
            button.canFocus = false;

            return button;
        }

        /// <summary>
        /// Create a resource radial chart whose slices are depicted in the same manner as Outside Connections.
        /// </summary>
        /// <param name="parent">Parent component name to attach to.</param>
        /// <param name="name">Name of the UI component, useful for browsing the scene in the explorer.</param>
        /// <returns></returns>
        public static UICargoChart CreateResourceRadialChart(UIComponent parent, string name)
        {
            UICargoChart cargoChart = parent.AddUIComponent<UICargoChart>();
            cargoChart.name = name;
            cargoChart.spriteName = "PieChartBg";
            cargoChart.size = new Vector2(90, 90);
            return cargoChart;
        }

        public static UILabel CreateLabel(UIComponent parent, string name, string text)
        {
            UILabel label = parent.AddUIComponent<UILabel>();
            label.name = name;
            label.text = text;

            return label;
        }

        public static UIPanel CreatePanel(UIComponent parent, string name)
        {
            UIPanel panel = parent.AddUIComponent<UIPanel>();
            panel.name = name;

            return panel;
        }

        /// <summary>
        /// Get the resource color for the resource tracked category type. These are the same colors used for import and export.
        /// Original concept for this in EOCV from rcav8tr (Extended Outside Connections View) although this is adapted appropriately
        /// for usage in this mod.
        /// </summary>
        public static Color GetResourceCategoryColor(ResourceCategoryType type)
        {
            // translate resource type to transfer reason
            int reason;
            switch (type)
            {
                // these are the reasons used in OutsideConnectionsInfoViewPanel in SetupImportLegend and SetupExportLegend
                case ResourceCategoryType.Goods:
                    reason = (int)TransferManager.TransferReason.Goods;
                    break;
                case ResourceCategoryType.Forestry:
                    reason = (int)TransferManager.TransferReason.Logs;
                    break;
                case ResourceCategoryType.Agriculture:
                    reason = (int)TransferManager.TransferReason.Grain;
                    break;
                case ResourceCategoryType.Ore:
                    reason = (int)TransferManager.TransferReason.Ore;
                    break;
                case ResourceCategoryType.Oil:
                    reason = (int)TransferManager.TransferReason.Oil;
                    break;
                case ResourceCategoryType.Mail:
                    reason = (int)TransferManager.TransferReason.Mail;
                    break;
                case ResourceCategoryType.Fish:
                    reason = (int)TransferManager.TransferReason.Fish;
                    break;
                default:
                    LogUtil.LogError($"Unable to translate resource category type [{type}] to resource color.");
                    return Color.black;
            }

            // do not get colors from color sprites because they might not be initialized yet
            return TransferManager.instance.m_properties.m_resourceColors[reason];
        }
    }
}
