﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace kurema.FileExplorerControl.ViewModels
{
    public class ContentViewModel : INotifyPropertyChanged
    {

        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [System.Runtime.CompilerServices.CallerMemberName]string propertyName = "",
            System.Action onChanged = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion


        private ContentStyles _ContentStyle=ContentStyles.Icon;
        public ContentStyles ContentStyle { get => _ContentStyle; set
            {
                SetProperty(ref _ContentStyle, value);
                (SetContentStyleCommand as Helper.DelegateCommand)?.OnCanExecuteChanged();
                OnPropertyChanged(nameof(IsDataGrid));
            }
        }

        public enum ContentStyles
        {
            Detail, List, Icon ,IconWide
        }

        public bool IsDataGrid
        {
            get
            {
                switch (ContentStyle)
                {
                    case ContentStyles.Detail:
                        return true;
                    case ContentStyles.List:
                    case ContentStyles.Icon:
                    case ContentStyles.IconWide:
                        return false;
                    default:
                        return false;
                }
            }
        }

        public ObservableCollection<FileItemViewModel> History { get; } = new ObservableCollection<FileItemViewModel>();

        private int _SelectedHistory;
        public int SelectedHistory { get => _SelectedHistory; set
            {
                if (!SelectedHistoryWithinRange(value))
                {
                    throw new IndexOutOfRangeException();
                }
                SetProperty(ref _SelectedHistory, value);
                OnPropertyChanged(nameof(Item));

                (HistoryShiftCommand as Helper.DelegateCommand)?.OnCanExecuteChanged();
                (GoUpCommand as Helper.DelegateCommand)?.OnCanExecuteChanged();
            }
        }

        private bool SelectedHistoryWithinRange(int target)
        {
            return History != null && 0 <= target && target < History.Count;
        }

        private System.Windows.Input.ICommand _RefreshCommand;
        public ICommand RefreshCommand => _RefreshCommand = _RefreshCommand ?? new Helper.DelegateCommand(
            async (a) => await Item?.UpdateChildren(),
            (a) => Item != null
            );


        private System.Windows.Input.ICommand _HistoryShiftCommand;
        public System.Windows.Input.ICommand HistoryShiftCommand => _HistoryShiftCommand = _HistoryShiftCommand ?? new Helper.DelegateCommand(
            (a) => {
                this.SelectedHistory += int.Parse(a.ToString());
            },
            (a) =>
            {
                return SelectedHistoryWithinRange(this.SelectedHistory + int.Parse(a.ToString()));
            }
            );

        private System.Windows.Input.ICommand _SetContentStyleCommand;

        public System.Windows.Input.ICommand SetContentStyleCommand => _SetContentStyleCommand = _SetContentStyleCommand ?? new Helper.DelegateCommand(
            a =>
            {
                var atxt = a.ToString();
                foreach (ContentStyles item in Enum.GetValues(typeof(ContentStyles)))
                {
                    if (atxt == Enum.GetName(typeof(ContentStyles), item)){
                        this.ContentStyle = item;
                        return;
                    }
                }
            },
            a =>
            {
                if (a?.ToString() == this.ContentStyle.ToString()) return false;
                var atxt = a?.ToString();
                foreach (ContentStyles item in Enum.GetValues(typeof(ContentStyles)))
                {
                    if (atxt == Enum.GetName(typeof(ContentStyles), item))
                    {
                        return true;
                    }
                }
                return false;
            }
            );


        private System.Windows.Input.ICommand _GoUpCommand;
        public System.Windows.Input.ICommand GoUpCommand => _GoUpCommand = _GoUpCommand ?? new Helper.DelegateCommand(
            async (a) =>
            {
                if (Item.Parent.Children == null) await Item.Parent.UpdateChildren();
                Item = Item.Parent;
            },
            (a) =>
            {
                return Item?.Parent != null;
            }
            );

        private System.Windows.Input.ICommand _GoToCommand;
        public System.Windows.Input.ICommand GoToCommand => _GoToCommand = _GoToCommand ?? new Helper.DelegateCommand(
            async a =>
            {
                if(a is FileItemViewModel vm)
                {
                    if (vm.Children == null) await vm.UpdateChildren();
                    Item = vm;
                }
            }
            );


        public FileItemViewModel Item
        {
            get => SelectedHistoryWithinRange(SelectedHistory) ? History[SelectedHistory] : null;
            set
            {
                while (History.Count > SelectedHistory + 1)
                {
                    History.Remove(History.Last());
                }
                History.Add(value);
                SelectedHistory = History.Count - 1;

                (RefreshCommand as Helper.DelegateCommand)?.OnCanExecuteChanged();

                if (value.Children == null)
                {
                    Task.Run(
                        async () =>
                        {
                            await value.UpdateChildren();
                        }
                        );
                }

            }
        }

    }
}
