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
using CastorPlugin.Services;
using Nice3point.Revit.Extensions;
using Revit.Async;
using CastorPlugin.Services.Contracts;
using CastorPlugin.ViewModels.Contracts;
using CastorPlugin.Views.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading;
using System.Windows;
using Wpf.Ui;

namespace CastorPlugin.ViewModels.Pages;

public sealed partial class DashboardViewModel : ObservableObject, IDashboardViewModel, IDisposable
{
    private readonly IAuthService _authService;
    private readonly IDigService _digService;
    private readonly INavigationService _navigationService;
    private readonly NotificationService _notificationService;
    private CancellationTokenSource _digCancellationTokenSource;

    public DashboardViewModel(
        IAuthService authService,
        IDigService digService,
        INavigationService navigationService,
        NotificationService notificationService)
    {
        _authService = authService;
        _digService = digService;
        _navigationService = navigationService;
        _notificationService = notificationService;

        // Listen to auth state changes
        _authService.OnAuthStateChanged += OnAuthStateChanged;

        // Listen to dig service events
        _digService.ProgressChanged += OnDigProgressChanged;
        _digService.DigCompleted += OnDigCompleted;
    }

    // ========== Login State ==========

    public bool IsLoggedIn => _authService.IsLoggedIn;

    public string UserPhone => _authService.CurrentUser?.Phone ?? "";

    public event Action AuthStateChanged;

    private void OnAuthStateChanged()
    {
        OnPropertyChanged(nameof(IsLoggedIn));
        OnPropertyChanged(nameof(UserPhone));
        AuthStateChanged?.Invoke();
    }

    // ========== Dig State ==========

    [ObservableProperty]
    private bool _isDigging;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _currentFamilyName = "";

    [ObservableProperty]
    private int _totalFamilies;

    [ObservableProperty]
    private int _scannedFamilies;

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private int _totalScanned;

    [ObservableProperty]
    private int _newRegistered;

    [ObservableProperty]
    private int _similarSkipped;

    [ObservableProperty]
    private string _resultMessage = "";

    private void OnDigProgressChanged(int scanned, int total, string familyName)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ScannedFamilies = scanned;
            TotalFamilies = total;
            CurrentFamilyName = familyName;
            Progress = total > 0 ? (double)scanned / total * 100 : 0;
        });
    }

    private void OnDigCompleted(int total, int newReg, int similar)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsDigging = false;
            TotalScanned = total;
            NewRegistered = newReg;
            SimilarSkipped = similar;
            HasResult = true;
            ResultMessage = $"扫描了 {total} 个族\n新增登记 {newReg} 个\n相似跳过 {similar} 个";
        });
    }

    // ========== Commands ==========

    [RelayCommand]
    private void ShowLogin()
    {
        var loginWindow = new LoginWindow(Host.GetService<LoginViewModel>());
        loginWindow.ShowDialog();
    }

    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
        _notificationService.ShowSuccess("已注销", "您已成功注销");
    }

    [RelayCommand]
    private async Task StartDigAsync()
    {
        if (!_authService.IsLoggedIn)
        {
            _notificationService.ShowWarning("请先登录", "登录后才能使用挖宝功能");
            return;
        }

        if (RevitApi.UiDocument is null)
        {
            _notificationService.ShowWarning("请先打开项目", "当前没有打开的 Revit 项目");
            return;
        }

        try
        {
            IsDigging = true;
            HasResult = false;
            Progress = 0;
            CurrentFamilyName = "准备开始...";
            _digCancellationTokenSource = new CancellationTokenSource();

            var result = await RevitTask.RunAsync(() => _digService.DigAsync(_digCancellationTokenSource.Token));

            // Update result
            TotalScanned = result.TotalChecked;
            NewRegistered = result.Posted;
            SimilarSkipped = result.TotalChecked - result.Posted;
            HasResult = true;
            ResultMessage = $"扫描了 {result.TotalChecked} 个族\n新增登记 {result.Posted} 个\n相似跳过 {result.TotalChecked - result.Posted} 个";
            _notificationService.ShowSuccess("挖宝完成", ResultMessage);
        }
        catch (OperationCanceledException)
        {
            _notificationService.ShowSuccess("已取消", "挖宝操作已取消");
        }
        catch (InvalidOperationException ex)
        {
            _notificationService.ShowWarning("操作无效", ex.Message);
        }
        catch (Exception ex)
        {
            _notificationService.ShowError("挖宝失败", ex.Message);
        }
        finally
        {
            IsDigging = false;
            _digCancellationTokenSource?.Dispose();
            _digCancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelDig()
    {
        _digCancellationTokenSource?.Cancel();
    }

    public IAsyncRelayCommand<string> NavigateSnoopPageCommand { get; }
    public IAsyncRelayCommand<string> OpenDialogCommand { get; }

    public void Dispose()
    {
        _authService.OnAuthStateChanged -= OnAuthStateChanged;
        _digService.ProgressChanged -= OnDigProgressChanged;
        _digService.DigCompleted -= OnDigCompleted;
        _digCancellationTokenSource?.Cancel();
        _digCancellationTokenSource?.Dispose();
    }
}
