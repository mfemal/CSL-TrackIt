using UnityEngine;
using ColossalFramework.UI;
using TrackIt.API;

namespace TrackIt
{
    /// <summary>
    /// Panel which can be used that restricts resource categories (if purchased as DLC) and constructs a legend
    /// with a swatch (colored and styled similar to 'Outside Connections') along with the resource name.
    /// </summary>
    internal class CargoUILegend : UIPanel
    {
        public UIPanel AgriculturePanel
        {
            get;
            private set;
        }

        public UIPanel FishPanel
        {
            get;
            private set;
        }

        public UIPanel ForestryPanel
        {
            get;
            private set;
        }

        public UIPanel GoodsPanel
        {
            get;
            private set;
        }

        public UIPanel MailPanel
        {
            get;
            private set;
        }

        public UIPanel OilPanel
        {
            get;
            private set;
        }

        public UIPanel OrePanel
        {
            get;
            private set;
        }

        private const string _namePrefix = "CargoLegend";
        private readonly RectOffset _resourcePadding = new RectOffset(2, 2, 2, 2);
        private float _textScale = 0.8125f;

        internal CargoUILegend()
        {
            name = UIUtils.ConstructComponentName(_namePrefix);
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Horizontal;

            foreach (ResourceCategoryType resourceCategoryType in UIUtils.CargoBasicResourceGroups)
            {
                string resourcePanelName = _namePrefix + resourceCategoryType;
                switch (resourceCategoryType)
                {
                    case ResourceCategoryType.Agriculture:
                        AgriculturePanel = UIUtils.CreatePanel(this, resourcePanelName);
                        CreateUICategory(resourceCategoryType, AgriculturePanel);
                        break;
                    case ResourceCategoryType.Fish:
                        FishPanel = UIUtils.CreatePanel(this, resourcePanelName);
                        CreateUICategory(resourceCategoryType, FishPanel);
                        break;
                    case ResourceCategoryType.Forestry:
                        ForestryPanel = UIUtils.CreatePanel(this, resourcePanelName);
                        CreateUICategory(resourceCategoryType, ForestryPanel);
                        break;
                    case ResourceCategoryType.Goods:
                        GoodsPanel = UIUtils.CreatePanel(this, resourcePanelName);
                        CreateUICategory(resourceCategoryType, GoodsPanel);
                        break;
                    case ResourceCategoryType.Mail:
                        MailPanel = UIUtils.CreatePanel(this, resourcePanelName);
                        CreateUICategory(resourceCategoryType, MailPanel);
                        break;
                    case ResourceCategoryType.Oil:
                        OilPanel = UIUtils.CreatePanel(this, resourcePanelName);
                        CreateUICategory(resourceCategoryType, OilPanel);
                        break;
                    case ResourceCategoryType.Ore:
                        OrePanel = UIUtils.CreatePanel(this, resourcePanelName);
                        CreateUICategory(resourceCategoryType, OrePanel);
                        break;
                }
            }
        }

        internal void CreateUICategory(ResourceCategoryType resourceCategoryType, UIPanel parentPanel)
        {
            string localeID = UIUtils.GetLocaleID(resourceCategoryType);
            if (localeID == null)
            {
                LogUtil.LogError("Unknown localeID for resource ${resourceCategoryType}");
                return;
            }

            parentPanel.autoLayout = true;
            parentPanel.autoLayoutDirection = LayoutDirection.Horizontal;
            parentPanel.autoLayoutStart = LayoutStart.TopLeft;
            parentPanel.autoSize = false;
            parentPanel.autoLayoutPadding = _resourcePadding;

            UISprite colorSwatch = UIUtils.CreateSprite(parentPanel, _namePrefix + resourceCategoryType + "Swatch", "EmptySprite");
            colorSwatch.size = new Vector2(17, 17); // from "Outside Connections"
            colorSwatch.color = UIUtils.GetResourceCategoryColor(resourceCategoryType);

            UILabel textLabel = UIUtils.CreateLabel(parentPanel, _namePrefix + resourceCategoryType + "Label", null);
            textLabel.wordWrap = true;
            textLabel.localeID = localeID;
            textLabel.isLocalized = true;
            textLabel.textScale = _textScale;

            parentPanel.anchor = UIAnchorStyle.Top | UIAnchorStyle.Left;
            parentPanel.FitChildren();
        }
    }
}
