﻿using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Interop;
using CairoDesktop.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using System.IO;
using CairoDesktop.SupportingClasses;
using CairoDesktop.Configuration;
using CairoDesktop.Common;
using System.Windows.Threading;

namespace CairoDesktop
{
    /// <summary>
    /// Interaction logic for Desktop.xaml
    /// </summary>
    public partial class Desktop : Window
    {
        public Stack<string> PathHistory = new Stack<string>();
        private WindowInteropHelper helper;
        public DesktopIcons Icons;

        public DependencyProperty IsOverlayOpenProperty = DependencyProperty.Register("IsOverlayOpen", typeof(bool), typeof(Desktop), new PropertyMetadata(new bool()));
        public bool IsOverlayOpen
        {
            get { return (bool)GetValue(IsOverlayOpenProperty); }
            set
            {
                SetValue(IsOverlayOpenProperty, value);

                if (value)
                    showOverlay();
                else
                    closeOverlay();
            }
        }

        public Desktop()
        {
            InitializeComponent();
            
            this.Width = AppBarHelper.PrimaryMonitorSize.Width;
            this.Height = AppBarHelper.PrimaryMonitorSize.Height-1;

            if (Startup.IsCairoUserShell)
            {
                sepPersonalization.Visibility = Visibility.Collapsed;
                miPersonalization.Visibility = Visibility.Collapsed;
            }

            setGridPosition();

            setBackground();
        }

        private void setBackground()
        {
            if (Startup.IsCairoUserShell)
            {
                string regWallpaper = (string)Registry.GetValue("HKEY_CURRENT_USER\\Control Panel\\Desktop", "Wallpaper", "");

                if (regWallpaper != string.Empty && Shell.Exists(regWallpaper))
                {
                    // draw wallpaper
                    try
                    {
                        ImageBrush bgBrush = new ImageBrush();
                        bgBrush.ImageSource = new BitmapImage(new Uri(regWallpaper, UriKind.Absolute));

                        this.Background = bgBrush;
                    }
                    catch { }
                }
            }
        }
        
        private void setupPostInit()
        {
            Shell.HideWindowFromTasks(helper.Handle);

            if (Settings.EnableDesktopOverlayHotKey)
                HotKeyManager.RegisterHotKey(Settings.DesktopOverlayHotKey, OnShowDesktop);
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(NativeMethods.MA_NOACTIVATE);
            }
            else if (msg == NativeMethods.WM_WINDOWPOSCHANGING)
            {
                /*// Extract the WINDOWPOS structure corresponding to this message
                NativeMethods.WINDOWPOS wndPos = NativeMethods.WINDOWPOS.FromMessage(lParam);

                // Determine if the z-order is changing (absence of SWP_NOZORDER flag)
                if (!((wndPos.flags & NativeMethods.SetWindowPosFlags.SWP_NOZORDER) == NativeMethods.SetWindowPosFlags.SWP_NOZORDER))
                {
                    // add the SWP_NOZORDER flag
                    wndPos.flags = wndPos.flags | NativeMethods.SetWindowPosFlags.SWP_NOZORDER;
                    wndPos.UpdateMessage(lParam);
                }*/

                handled = true;
                return new IntPtr(NativeMethods.MA_NOACTIVATE);
            }
            else if (msg == NativeMethods.WM_DISPLAYCHANGE && (Startup.IsCairoUserShell))
            {
                setPosition(((uint)lParam & 0xffff), ((uint)lParam >> 16));
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void setPosition(uint x, uint y)
        {
            this.Top = 0;
            this.Left = 0;

            this.Width = x;
            this.Height = y - 1;
            setGridPosition();
        }

        public void ResetPosition()
        {
            this.Top = 0;
            this.Left = 0;

            this.Width = AppBarHelper.PrimaryMonitorSize.Width;
            this.Height = AppBarHelper.PrimaryMonitorSize.Height - 1;
            setGridPosition();
        }

        private void setGridPosition()
        {
            grid.Width = AppBarHelper.PrimaryMonitorWorkArea.Width / Shell.DpiScale;
            grid.Height = AppBarHelper.PrimaryMonitorWorkArea.Height / Shell.DpiScale;
            grid.Margin = new Thickness(System.Windows.Forms.SystemInformation.WorkingArea.Left / Shell.DpiScale, System.Windows.Forms.SystemInformation.WorkingArea.Top / Shell.DpiScale, 0, 0);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if (!Topmost)
            {
                int result = NativeMethods.SetShellWindow(helper.Handle);
                Shell.ShowWindowBottomMost(helper.Handle);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Startup.IsShuttingDown)
            {
                // show the windows desktop
                Shell.ToggleDesktopIcons(true);
            }
            else
                e.Cancel = true;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            this.Top = 0;

            helper = new WindowInteropHelper(this);

            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(new HwndSourceHook(WndProc));

            if (Settings.EnableDesktop && Icons == null)
            {
                Icons = new DesktopIcons();
                grid.Children.Add(Icons);

                if (Settings.EnableDynamicDesktop)
                {
                    try
                    {
                        DesktopNavigationToolbar nav = new DesktopNavigationToolbar() { Owner = this };
                        nav.Show();
                    }
                    catch { }
                }
            }

            setupPostInit();
        }

        private void pasteFromClipboard()
        {
            IDataObject clipFiles = Clipboard.GetDataObject();

            if(clipFiles.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])clipFiles.GetData(DataFormats.FileDrop);

                foreach(string file in files)
                {
                    if(Shell.Exists(file))
                    {
                        try
                        {
                            FileAttributes attr = File.GetAttributes(file);
                            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                                FileSystem.CopyDirectory(file, Icons.Locations[0].FullName + "\\" + new DirectoryInfo(file).Name, UIOption.AllDialogs);
                            else
                                FileSystem.CopyFile(file, Icons.Locations[0].FullName + "\\" + Path.GetFileName(file), UIOption.AllDialogs);
                        }
                        catch { }
                    }
                }
            }
        }

        private void miPaste_Click(object sender, RoutedEventArgs e)
        {
            pasteFromClipboard();
        }

        private void miPersonalization_Click(object sender, RoutedEventArgs e)
        {
            // doesn't work when shell because Settings app requires Explorer :(
            if (!Shell.StartProcess("desk.cpl"))
            {
                CairoMessage.Show("Unable to open Personalization settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (IsOverlayOpen)
            {
                IsOverlayOpen = false;
            }
        }

        private void grid_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!Topmost)
                NativeMethods.SetForegroundWindow(helper.Handle);
        }

        public void Navigate(string newLocation)
        {
            PathHistory.Push(Icons.Locations[0].FullName);
            Icons.Locations[0] = new SystemDirectory(newLocation, Dispatcher.CurrentDispatcher);
        }

        private void CairoDesktopWindow_LocationChanged(object sender, EventArgs e)
        {
            ResetPosition();
        }

        private void OnShowDesktop(HotKey hotKey)
        {
            ToggleOverlay();
        }

        public void ToggleOverlay()
        {
            if (!IsOverlayOpen)
            {
                IsOverlayOpen = true;
            }
            else
            {
                IsOverlayOpen = false;
            }
        }

        private void showOverlay()
        {
            Topmost = true;
            NativeMethods.SetForegroundWindow(helper.Handle);
            grid.Background = new SolidColorBrush(Color.FromArgb(0x88, 0, 0, 0));
            this.Background = null;
        }

        private void closeOverlay()
        {
            Topmost = false;
            Shell.ShowWindowBottomMost(helper.Handle);
            grid.Background = Brushes.Transparent;
            setBackground();
        }

        private void grid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(System.Windows.Controls.ScrollViewer))
                IsOverlayOpen = false;
        }
    }
}
