﻿using CairoDesktop.Common.DesignPatterns;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CairoDesktop.Extensibility.ObjectModel
{
    public sealed class _CairoShell : SingletonObject<_CairoShell>
    {
        static _CairoShell()
        {
            OnStart += (a) => { };
            OnQuit += (a) => { };
        }

        private _CairoShell()
        {
            PlacesMenu = new List<MenuItem>();

            OnStart += (x) => { };
            OnQuit += (x) => { };
        }

        public static event Action<_CairoShell> OnStart;

        public static event Action<_CairoShell> OnQuit;

        public static void Start()
        {
            OnStart(Instance);
        }

        public static void Quit()
        {
            OnQuit(Instance);
        }

        /// <summary>
        /// Compatibility System.Windows.Forms.Application.DoEvents
        /// </summary>
        public static void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        private static object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;
            return null;
        }

        /// <summary>
        /// Compatibility System.Windows.Forms.Application.StartupPath
        /// </summary>
        public static string StartupPath { get { return Path.GetDirectoryName((Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location); } }

        public List<MenuItem> PlacesMenu { get; private set; }
    }
}