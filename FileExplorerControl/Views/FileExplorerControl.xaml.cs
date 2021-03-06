﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using winui = Microsoft.UI.Xaml.Controls;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace kurema.FileExplorerControl.Views
{
    public sealed partial class FileExplorerControl : UserControl
    {
        public FileExplorerControl()
        {
            this.InitializeComponent();

            if(this.DataContext is ViewModels.FileExplorerViewModel fvm && this.content.DataContext is ViewModels.ContentViewModel vm)
            {
                fvm.Content = vm;

                fvm.Content.PropertyChanged += async (s, e) => {
                    if (e.PropertyName == nameof(ViewModels.ContentViewModel.Item))
                    {
                        address.SetAddress(fvm.Content.Item);

                        await OpenTreeView(fvm.Content.Item);
                    }
                };
            }
        }

        public UIElementCollection MenuChildrens => this.pageMenuStack.Children;

        public async System.Threading.Tasks.Task OpenTreeView(ViewModels.FileItemViewModel fv)
        {
            var list = new Stack<ViewModels.FileItemViewModel>();
            var cfv = fv;
            while (cfv != null)
            {
                list.Push(cfv);
                cfv = cfv.Parent;
            }

            var cnode = treeview.RootNodes;
            winui.TreeViewNode ctreenode = null;
            foreach (var item in list)
            {
                ctreenode = cnode.FirstOrDefault(a => a.Content is ViewModels.FileItemViewModel fv2 && fv2.Path == item.Path);
                if (ctreenode == null) return;
                if (ctreenode.Content is ViewModels.FileItemViewModel fvm && fvm.Children == null) await fvm.UpdateChildren();
                ctreenode.IsExpanded = true;
                cnode = ctreenode.Children;
            }
            //You can't item in single selection mode before Microsoft.UI.Xaml v2.2.190731001-prerelease.
            //See.
            //https://github.com/microsoft/microsoft-ui-xaml/pull/243
            //So treeview is replaced by WinUI version.

            treeview.SelectedNode = ctreenode;

            //Maybe you can scroll...
            //See.
            //https://stackoverflow.com/questions/52015723/uwp-winui-treeview-programatically-scroll-to-item
            //But well... I dont like this.
        }

        public void SetTreeViewItem(params ViewModels.FileItemViewModel[] fileItemVMs)
        {
            foreach (var item in fileItemVMs)
            {
                treeview.RootNodes.Add(new winui.TreeViewNode()
                {
                    IsExpanded = false,
                    Content = item,
                    HasUnrealizedChildren = item.IsFolder,
                });
            }
        }

        private async void TreeView_Expanding(winui.TreeView sender, winui.TreeViewExpandingEventArgs args)
        {
            if (!args.Node.HasUnrealizedChildren) return;

            sender.IsEnabled = false;

            try
            {
                if (args.Item is ViewModels.FileItemViewModel vm)
                {
                    if (vm.Children == null) await vm.UpdateChildren();
                    args.Node.Children.Clear();
                    foreach (var item in vm.Folders)
                    {
                        args.Node.Children.Add(new winui.TreeViewNode()
                        {
                            IsExpanded = false,
                            Content = item,
                            HasUnrealizedChildren = item.IsFolder,
                        });
                    }
                }
            }
            finally
            {
                args.Node.HasUnrealizedChildren = false;
                sender.IsEnabled = true;
            }
        }

        public FileExplorerContentControl ContentControl => content;

        private async void treeview_ItemInvoked(winui.TreeView sender, winui.TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is winui.TreeViewNode tvn && tvn.Content is ViewModels.FileItemViewModel vm)
            {
                await content.SetFolder(vm);
                if (tvn.IsExpanded == false) tvn.IsExpanded = true;
            }
        }

        private void address_FocusLostRequested(object sender, EventArgs e)
        {
            address.Opacity = 0;
            address.IsHitTestVisible = false;
            address_text.Opacity = 1;
            address_text.IsHitTestVisible = true;
            address_text.Focus(FocusState.Programmatic);

            address_text.SelectAll();
        }

        private void address_text_FocusDisengaged(object sender, RoutedEventArgs e)
        {
            address.Opacity = 1;
            address.IsHitTestVisible = true;
            address_text.Opacity = 0;
            address_text.IsHitTestVisible = false;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rd)
            {
                if (this.DataContext is ViewModels.FileExplorerViewModel fevm && fevm?.Content?.Item.Order != null)
                {
                    fevm.Content.Item.Order = fevm.Content.Item.Order.GetBasicOrder(rd.Tag.ToString(), fevm.Content.Item.Order?.KeyIsAscending ?? true);
                }
            }
        }

        private void RadioButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton rd)
            {
                if (this.DataContext is ViewModels.FileExplorerViewModel fevm && fevm?.Content?.Item != null)
                {
                    fevm.Content.Item.Order = new ViewModels.FileItemViewModel.OrderStatus();
                }
            }
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            if(sender is ToggleButton tb)
            {
                if(this.DataContext is ViewModels.FileExplorerViewModel fevm && fevm?.Content?.Item.Order != null)
                {
                    fevm.Content.Item.Order = fevm.Content.Item.Order.GetBasicOrder(fevm.Content.Item.Order.Key, tb.Tag.ToString() == "Ascending");
                }
            }
        }

        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb)
            {
                if (this.DataContext is ViewModels.FileExplorerViewModel fevm && fevm?.Content?.Item.Order != null)
                {
                    fevm.Content.Item.Order = fevm.Content.Item.Order.GetBasicOrder(fevm.Content.Item.Order.Key, tb.Tag.ToString() != "Ascending");
                }
            }
        }
    }
}
