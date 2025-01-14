using Prism.Mvvm;
using WDE.Common;
using WDE.Common.Types;

namespace WoWDatabaseEditorCore.Services.QuickLoadService
{
    public class QuickLoadItem : BindableBase
    {
        private bool isVisible;
        public ImageUri Icon { get; }
        public string Name { get; }

        public bool IsVisible
        {
            get => isVisible;
            set => SetProperty(ref isVisible, value);
        }

        public QuickLoadItem(string name, ImageUri icon)
        {
            Name = name;
            Icon = icon;
            IsVisible = true;
        }

        public QuickLoadItem(ISolutionItemProvider item)
        {
            Icon = item.GetImage();
            Name = item.GetName();
            IsVisible = true;
        }
    }
}