﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using BookViewerApp.ViewModels;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BookViewerApp.Views
{
    public sealed partial class BookFixed2Page : UserControl,INotifyPropertyChanged
    {
        public void OnPropertyChanged(string name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }
        public event PropertyChangedEventHandler PropertyChanged;
        public float? ZoomFactor
        {
            get { return scrollViewer.ZoomFactor; }
            set
            {
                if(!value.HasValue)return;
                var resultFactor = Math.Max(Math.Min(scrollViewer.MaxZoomFactor, value.Value), scrollViewer.MinZoomFactor);
                if(value!=resultFactor) { throw new ArgumentOutOfRangeException();}
                scrollViewer.ChangeView(null, null, resultFactor);
                //changeViewWithKeepCurrentCenter(scrollViewer, resultFactor);
                OnPropertyChanged(nameof(ZoomFactor));
            }
        }

        public BookFixed2Page()
        {
            this.InitializeComponent();

            this.DataContextChanged += (s3, e3) =>
            {
                this.scrollViewer.ViewChanged += (s2, e2) =>
                {
                    if (this.DataContext is PageViewModel pvm)
                        pvm.ZoomFactor = scrollViewer.ZoomFactor;
                };
                if (this.DataContext is PageViewModel pv)
                {
                    pv.ZoomRequested += (s2, e2) =>
                    {
                        var value = e2.ZoomFactor;
                        var resultFactor = Math.Max(Math.Min(scrollViewer.MaxZoomFactor, value),
                            scrollViewer.MinZoomFactor);
                        var currentFactor = this.ZoomFactor;
                        changeViewWithKeepCurrentCenter(scrollViewer, resultFactor);
                    };
                    pv.PropertyChanged += (s2, e2) =>
                    {
                        if (e2.PropertyName == nameof(PageViewModel.ZoomFactor))
                        {
                            try
                            {
                                this.ZoomFactor = (this.DataContext as PageViewModel)?.ZoomFactor;
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                if (this.ZoomFactor.HasValue)
                                    pv.ZoomFactor = this.ZoomFactor.Value;
                            }
                        }
                    };
                }
            };
        }

        private void changeViewWithKeepCurrentCenter(ScrollViewer sv, float zoomFactor)
        {
            double originalWidth = sv.ExtentWidth / sv.ZoomFactor;
            double originalHeight = sv.ExtentHeight / sv.ZoomFactor;

            double originalCenterX = 0;
            double originalCenterY = 0;

            if (sv.ViewportWidth < sv.ExtentWidth)
            {
                double eCenterX = sv.HorizontalOffset + sv.ViewportWidth / 2;
                originalCenterX = eCenterX / sv.ZoomFactor;
            }
            else
            {
                double eCenterX = sv.HorizontalOffset + sv.ExtentWidth / 2;
                originalCenterX = eCenterX / sv.ZoomFactor;
            }

            if (sv.ViewportHeight < sv.ExtentHeight)
            {
                double eCenterY = sv.VerticalOffset + sv.ViewportHeight / 2;
                originalCenterY = eCenterY / sv.ZoomFactor;
            }
            else
            {
                double eCenterY = sv.VerticalOffset + sv.ExtentHeight / 2;
                originalCenterY = eCenterY / sv.ZoomFactor;
            }


            double newExtentCenterX = originalCenterX * zoomFactor;
            double newExtentCenterY = originalCenterY * zoomFactor;

            double newExtentWidth = originalWidth * zoomFactor;
            double newExtentHeight = originalHeight * zoomFactor;

            double newExtentOffsetX = newExtentCenterX - sv.ViewportWidth / 2;
            double newExtentOffsetY = newExtentCenterY - sv.ViewportHeight / 2;
            sv.ChangeView(newExtentOffsetX, newExtentOffsetY, zoomFactor, false);
        }


        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            grid.Width = this.ActualWidth;
            grid.Height = this.ActualHeight;

            spreadPanel.Width = this.ActualWidth;
            spreadPanel.Height = this.ActualHeight;
            //stack.Height = this.ActualHeight;
            //stack.MaxWidth = this.ActualWidth;

            (DataContext as ViewModels.PageViewModel)?.UpdateSource();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            scrollViewer.ChangeView(0, 0, 1.0f);

            (DataContext as ViewModels.PageViewModel)?.UpdateSource();
        }

        private void scrollViewer_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            (DataContext as ViewModels.PageViewModel)?.UpdateSource();
        }

        private double _initialHorizontalOffset;
        private double _initialVerticalOffset;
        private Windows.UI.Input.PointerPoint _initialPoint;
        private DateTime _lastClickedTime;

        private void scrollViewer_PointerInit(PointerRoutedEventArgs e)
        {
            _initialHorizontalOffset = scrollViewer.HorizontalOffset;
            _initialVerticalOffset = scrollViewer.VerticalOffset;
            _initialPoint = e.GetCurrentPoint(scrollViewer);
            _lastClickedTime = DateTime.Now;
        }

        private void scrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            scrollViewer_PointerInit(e);
        }

        private void scrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer?.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse &&
                e.Pointer?.IsInContact == true)
            {
                //Workaround. PointerPressed not fired in some case.
                if ((DateTime.Now - _lastClickedTime).TotalSeconds > 1) { scrollViewer_PointerInit(e); }

                var point = e.GetCurrentPoint(scrollViewer);
                if (point == null || _initialPoint == null) return;
                scrollViewer.ChangeView(_initialHorizontalOffset - (point.Position.X - _initialPoint.Position.X),
                    _initialVerticalOffset - (point.Position.Y - _initialPoint.Position.Y), null);
                e.Handled = true;

                _lastClickedTime = DateTime.Now;
            }
            else
            {
                e.Handled = false;
            }
        }

        private async void ScrollViewer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {

            if (e.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                await Task.Delay(100);
            }
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;

            var doubleTapPoint = e.GetPosition(scrollViewer);
            if (scrollViewer.ZoomFactor != 1.0)
            {
                scrollViewer.ChangeView(null, null, 1);
            }
            else
            {
                scrollViewer.ChangeView(doubleTapPoint.X, doubleTapPoint.Y, 2);
            }
            e.Handled = true;
        }
    }
}
