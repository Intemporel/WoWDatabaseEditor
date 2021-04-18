﻿using System;
using System.IO;
using System.Reflection;
using Avalonia;
using Avalonia.ReactiveUI;
using WoWDatabaseEditorCore.Managers;

namespace WoWDatabaseEditorCore.Avalonia
{
    class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args)
        {
            FixCurrentDirectory();
            if (ProgramBootstrap.TryLaunchUpdaterIfNeeded())
                return;
            var app = BuildAvaloniaApp();
            try
            {
                app.StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                FatalErrorHandler.ExceptionOccured(e);
            }
        }

        private static void FixCurrentDirectory()
        {
            var exePath = new FileInfo(Assembly.GetExecutingAssembly().Location);
            if (exePath.Directory != null)
                Directory.SetCurrentDirectory(exePath.Directory.FullName);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new AvaloniaNativePlatformOptions { UseGpu = true })
                .UseReactiveUI()
                .LogToTrace();
    }
}