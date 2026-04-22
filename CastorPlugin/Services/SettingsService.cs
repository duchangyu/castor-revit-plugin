using Autodesk.Internal.InfoCenter;
using CastorPlugin.Services.Contracts;
using CastorPlugin.Services.DTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel; // Add this line
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CastorPlugin.Services
{

    /// <summary>
    ///     Settings options saved to disk
    /// </summary>
    [Serializable]
    internal sealed class Settings
    {
        [JsonPropertyName("Theme")] public ApplicationTheme Theme { get; set; } = ApplicationTheme.Light;
        [JsonPropertyName("Background")] public WindowBackdropType Background { get; set; } = WindowBackdropType.None;
        [JsonPropertyName("TransitionDuration")] public int TransitionDuration { get; set; } //= SettingsService.DefaultTransitionDuration;
        //[JsonPropertyName("IsHardwareRenderingAllowed")] public bool UseHardwareRendering { get; set; } = true;
        //[JsonPropertyName("IsTimeColumnAllowed")] public bool ShowTimeColumn { get; set; }
        [JsonPropertyName("UseSizeRestoring")] public bool UseSizeRestoring { get; set; }
        [JsonPropertyName("WindowWidth")] public double WindowWidth { get; set; }
        [JsonPropertyName("WindowHeight")] public double WindowHeight { get; set; }
        [JsonPropertyName("ApiUrl")] public string ApiUrl {get;set;}
        [JsonPropertyName("AccessToken")] public string AccessToken { get; set; }
        [JsonPropertyName("RefreshToken")] public string RefreshToken { get; set; }
        [JsonPropertyName("TokenExpiry")] public DateTime? TokenExpiry { get; set; }
        [JsonPropertyName("CurrentUser")] public UserDto CurrentUser { get; set; }
        //[JsonPropertyName("IsUnsupportedAllowed")] public bool IncludeUnsupported { get; set; }
        //[JsonPropertyName("IsPrivateAllowed")] public bool IncludePrivate { get; set; }
        //[JsonPropertyName("IsStaticAllowed")] public bool IncludeStatic { get; set; }
        //[JsonPropertyName("IsFieldsAllowed")] public bool IncludeFields { get; set; }
        //[JsonPropertyName("IsEventsAllowed")] public bool IncludeEvents { get; set; }
        //[JsonPropertyName("IsExtensionsAllowed")] public bool IncludeExtensions { get; set; }
        //[JsonPropertyName("IsRootHierarchyAllowed")] public bool IncludeRootHierarchy { get; set; }
        //[JsonPropertyName("IsModifyTabAllowed")] public bool UseModifyTab { get; set; }
    }

    public sealed class SettingsService : ISettingsService, INotifyPropertyChanged // Implement INotifyPropertyChanged
    {
        private const int DefaultTransitionDuration = 200;
        private const string DefaultAssetPlazaUrl = "http://192.168.1.142:5173/";
        private readonly Settings _settings;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(IConfiguration configuration, ILogger<SettingsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _settings = LoadSettings();

        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ApplicationTheme Theme
        {
            get => _settings.Theme;
            set => _settings.Theme = value;
        }

        public WindowBackdropType Background
        {
            get => _settings.Background;
            set => _settings.Background = value;
        }

        public int TransitionDuration
        {
            get => _settings.TransitionDuration;
            private set => _settings.TransitionDuration = value;
        }

        public bool UseSizeRestoring
        {
            get => _settings.UseSizeRestoring;
            set => _settings.UseSizeRestoring = value;
        }

        public double WindowWidth
        {
            get => _settings.WindowWidth;
            set => _settings.WindowWidth = value;
        }

        public double WindowHeight
        {
            get => _settings.WindowHeight;
            set => _settings.WindowHeight = value;
        }

        public string ApiUrl
        {
            get => _settings.ApiUrl;
            set => _settings.ApiUrl = value;
        }

        public string AccessToken
        {
            get => _settings.AccessToken;
            set => _settings.AccessToken = value;
        }

        public string RefreshToken
        {
            get => _settings.RefreshToken;
            set => _settings.RefreshToken = value;
        }

        public DateTime? TokenExpiry
        {
            get => _settings.TokenExpiry;
            set => _settings.TokenExpiry = value;
        }

        public UserDto CurrentUser
        {
            get => _settings.CurrentUser;
            set => _settings.CurrentUser = value;
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(_settings.AccessToken);

        public void ClearAuth()
        {
            _settings.AccessToken = null;
            _settings.RefreshToken = null;
            _settings.TokenExpiry = null;
            _settings.CurrentUser = null;
        }

        public int ApplyTransition(bool value)
        {
            return TransitionDuration = value ? DefaultTransitionDuration : 0;
        }

        public void Save()
        {
            var settingsFile = _configuration.GetValue<string>("SettingsPath");

            Directory.CreateDirectory(Path.GetDirectoryName(settingsFile)!);

            var jsonSerializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
            {
                new JsonStringEnumConverter()
            }
            };

            var json = JsonSerializer.Serialize(_settings, jsonSerializerOptions);

            File.WriteAllText(settingsFile, json);
        }

        private Settings LoadSettings()
        {
            var settingsFile = _configuration.GetValue<string>("SettingsPath");
            if (!File.Exists(settingsFile)) return new Settings();

            try
            {
                using var config = File.OpenRead(settingsFile);
                var jsonSerializerOptions = new JsonSerializerOptions
                {
                    Converters =
                {
                    new JsonStringEnumConverter()
                }
                };

                return JsonSerializer.Deserialize<Settings>(config, jsonSerializerOptions);
            }
            catch
            {
                _logger.LogInformation("Settings deserializing error");
            }

            return new Settings();

        }

        public string GetLandingPageUrl()
        {
            var landingPageUrl = _configuration["LandingPageUrl"];
            return string.IsNullOrWhiteSpace(landingPageUrl)
                ? DefaultAssetPlazaUrl
                : landingPageUrl;
        }

        public string GetAssetPlazaUrl()
        {
            var assetPlazaUrl = _configuration["AssetPlazaUrl"];
            return string.IsNullOrWhiteSpace(assetPlazaUrl)
                ? GetLandingPageUrl()
                : assetPlazaUrl;
        }

    }
}
