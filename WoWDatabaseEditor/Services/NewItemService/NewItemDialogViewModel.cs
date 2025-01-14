﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.NewItemService
{
    [AutoRegister]
    public class NewItemDialogViewModel : BindableBase, INewItemDialogViewModel
    {
        private string customName = "New folder";
        private NewItemPrototypeInfo? selectedPrototype;

        public NewItemDialogViewModel(ISolutionItemProvideService provider, ICurrentCoreVersion currentCore)
        {
            Dictionary<string, NewItemPrototypeGroup> groups = new();
            ItemPrototypes = new ObservableCollection<NewItemPrototypeGroup>();

            bool coreIsSpecific = currentCore.IsSpecified;
            foreach (var item in provider.All)
            {
                if (!groups.TryGetValue(item.GetGroupName(), out var group))
                {
                    group = new NewItemPrototypeGroup(item.GetGroupName());
                    groups[item.GetGroupName()] = group;
                    ItemPrototypes.Add(group);
                }

                bool isCompatible = item.IsCompatibleWithCore(currentCore.Current);
                
                if (!isCompatible && coreIsSpecific)
                    continue;

                var info = new NewItemPrototypeInfo(item, isCompatible);
                group.Add(info);
                FlatItemPrototypes.Add(info);
            }

            Accept = new DelegateCommand(() =>
            {
                CloseOk?.Invoke();
            });
            Cancel = new DelegateCommand(() =>
            {
                CloseCancel?.Invoke();
            });

            if (ItemPrototypes.Count > 0 && ItemPrototypes[0].Count > 0)
                SelectedPrototype = ItemPrototypes[0][0];
        }

        public void AllowFolders(bool showFolders)
        {
            if (!showFolders)
            {
                FlatItemPrototypes.Remove(FlatItemPrototypes.Where(i => i.IsContainer).ToList());
                foreach (var group in ItemPrototypes)
                    group.Remove(group.Where(i => i.IsContainer).ToList());
                ItemPrototypes.Remove(ItemPrototypes.Where(i => i.Count == 0).ToList());

                if (ItemPrototypes.Count > 0 && ItemPrototypes[0].Count > 0)
                    SelectedPrototype = ItemPrototypes[0][0];
            }
        }
        
        public ObservableCollection<NewItemPrototypeGroup> ItemPrototypes { get; }
        
        // for WPF: can be removed when WPF is removed
        public ObservableCollection<NewItemPrototypeInfo> FlatItemPrototypes { get; } = new();
        
        public NewItemPrototypeInfo? SelectedPrototype
        {
            get => selectedPrototype;
            set => SetProperty(ref selectedPrototype, value);
        }
        
        public string CustomName
        {
            get => customName;
            set => SetProperty(ref customName, value);
        }

        public async Task<ISolutionItem?> CreateSolutionItem()
        {
            if (SelectedPrototype == null)
                return null;

            return await SelectedPrototype.CreateSolutionItem(CustomName);
        }

        public ICommand Cancel { get; }
        public ICommand Accept { get; }
        public int DesiredWidth => 600;
        public int DesiredHeight => 430;
        public string Title => "New item";
        public bool Resizeable => false;
        public event Action? CloseCancel;
        public event Action? CloseOk;
    }

    public class NewItemPrototypeGroup : ObservableCollection<NewItemPrototypeInfo>, INotifyPropertyChanged
    {
        public NewItemPrototypeGroup(string groupName)
        {
            GroupName = groupName;
        }
        
        public string GroupName { get; }
    }
}