// Copyright 2003-2023 by Autodesk, Inc.
// 
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
// 
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
// 
// Use, duplication, or disclosure by the U.S. Government is subject to
// restrictions set forth in FAR 52.227-19 (Commercial Computer
// Software - Restricted Rights) and DFAR 252.227-7013(c)(1)(ii)
// (Rights in Technical Data and Computer Software), as applicable.

using CastorPlugin.Core;
using CastorPlugin.Services.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Wpf.Ui;

namespace CastorPlugin.Services;

public sealed class CastorService : ICastorService
{
    private static Dispatcher _dispatcher;
    private CastorServiceImpl _castorService;

    static CastorService()
    {
        var uiThread = new Thread(Dispatcher.Run);
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();

        EnsureThreadStart(uiThread);
    }

    public CastorService(IServiceScopeFactory scopeFactory)
    {
        if (Thread.CurrentThread == _dispatcher.Thread)
        {
            _castorService = new CastorServiceImpl(scopeFactory);
        }
        else
        {
            _dispatcher.InvokeAsync(() => _castorService = new CastorServiceImpl(scopeFactory));
        }
    }

    //public ICastorServiceDependsStage Snoop(SnoopableType snoopableType)
    //{
    //    if (Thread.CurrentThread == _dispatcher.Thread)
    //    {
    //        _castorService.Snoop(snoopableType);
    //    }
    //    else
    //    {
    //        _dispatcher.InvokeAsync(() => _castorService.Snoop(snoopableType));
    //    }

    //    return this;
    //}

    //public ICastorServiceDependsStage Snoop(SnoopableObject snoopableObject)
    //{
    //    if (Thread.CurrentThread == _dispatcher.Thread)
    //    {
    //        _castorService.Snoop(snoopableObject);
    //    }
    //    else
    //    {
    //        _dispatcher.InvokeAsync(() => _castorService.Snoop(snoopableObject));
    //    }

    //    return this;
    //}

    //public ICastorServiceDependsStage Snoop(IReadOnlyCollection<SnoopableObject> snoopableObjects)
    //{
    //    if (Thread.CurrentThread == _dispatcher.Thread)
    //    {
    //        _castorService.Snoop(snoopableObjects);
    //    }
    //    else
    //    {
    //        _dispatcher.InvokeAsync(() => _castorService.Snoop(snoopableObjects));
    //    }

    //    return this;
    //}

    public ICastorServiceShowStage DependsOn(IServiceProvider provider)
    {
        if (Thread.CurrentThread == _dispatcher.Thread)
        {
            _castorService.DependsOn(provider);
        }
        else
        {
            _dispatcher.InvokeAsync(() => _castorService.DependsOn(provider));
        }

        return this;
    }

    public ICastorServiceExecuteStage Show<T>() where T : Page
    {
        if (Thread.CurrentThread == _dispatcher.Thread)
        {
            _castorService.Show<T>();
        }
        else
        {
            _dispatcher.InvokeAsync(() => _castorService.Show<T>());
        }

        return this;
    }

    public void Execute<T>(Action<T> handler) where T : class
    {
        if (Thread.CurrentThread == _dispatcher.Thread)
        {
            _castorService.Execute(handler);
        }
        else
        {
            _dispatcher.InvokeAsync(() => _castorService.Execute(handler));
        }
    }

    private static void EnsureThreadStart(Thread thread)
    {
        Dispatcher dispatcher = null;
        SpinWait spinWait = new();
        while (dispatcher is null)
        {
            spinWait.SpinOnce();
            dispatcher = Dispatcher.FromThread(thread);
        }

        _dispatcher = dispatcher;
    }

    private class CastorServiceImpl
    {
        private Window _owner;
        private Task _activeTask;
        private readonly IServiceScope _serviceScope;
        //private readonly ICastorVisualService _visualService;
        private readonly INavigationService _navigationService;
        private readonly Window _window;

        public CastorServiceImpl(IServiceScopeFactory scopeFactory)
        {
            _serviceScope = scopeFactory.CreateScope();

            _window = (Window) _serviceScope.ServiceProvider.GetRequiredService<IWindow>();
            //_visualService = _serviceScope.ServiceProvider.GetService<ISnoopVisualService>();
            _navigationService = _serviceScope.ServiceProvider.GetRequiredService<INavigationService>();

            _window.Closed += (_, _) => _serviceScope.Dispose();
        }

        //public void Snoop(SnoopableType snoopableType)
        //{
        //    _activeTask = _visualService!.SnoopAsync(snoopableType);
        //}

        //public void Snoop(SnoopableObject snoopableObject)
        //{
        //    _visualService.Snoop(snoopableObject);
        //}

        //public void Snoop(IReadOnlyCollection<SnoopableObject> snoopableObjects)
        //{
        //    _visualService.Snoop(snoopableObjects);
        //}

        public void DependsOn(IServiceProvider provider)
        {
            _owner = (Window) provider.GetRequiredService<IWindow>();
        }

        public void Show<T>() where T : Page
        {
            if (_activeTask is null)
            {
                ShowPage<T>();
            }
            else
            {
                _activeTask = _activeTask.ContinueWith(_ => _dispatcher.Invoke(() => ShowPage<T>()));
            }
        }

        public void Execute<T>(Action<T> handler) where T : class
        {
            if (_activeTask is null)
            {
                InvokeHandler(handler);
            }
            else
            {
                _activeTask = _activeTask.ContinueWith(_ => _dispatcher.Invoke(() => InvokeHandler(handler)));
            }
        }

        private void InvokeHandler<T>(Action<T> handler) where T : class
        {
            var service = _serviceScope.ServiceProvider.GetService<T>();
            handler.Invoke(service);
        }

        private void ShowPage<T>() where T : Page
        {
            if (_owner is null)
            {
                _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                _window.Left = _owner.Left + 47;
                _window.Top = _owner.Top + 49;
            }

            var uiApplication = RevitApi.UiApplication
                ?? throw new InvalidOperationException("Revit UI application is not initialized.");

            _window.Show(uiApplication.MainWindowHandle);
            _navigationService.Navigate(typeof(T));
        }
    }
}
