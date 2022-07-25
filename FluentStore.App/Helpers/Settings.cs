﻿using CommunityToolkit.Mvvm.DependencyInjection;
using FluentStore.SDK;
using FluentStore.SDK.Helpers;
using FluentStore.Services;
using OwlCore.AbstractStorage;
using OwlCore.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace FluentStore.Helpers
{
    public class Settings : SettingsBase, ISettingsService
    {
        private const string KEY_PackageHandlerEnabled = "PackageHandlerEnabled";
        private static Settings s_settings;
        private static readonly DefaultSettingValues s_defVals = new();
        private readonly string m_defaultPluginDirectory;

        public static Settings Default => s_settings;

        public Settings(IFolderData settingsDirectory, IFolderData pluginDirectory)
            : base(settingsDirectory, new NewtonsoftStreamSerializer())
        {
            m_defaultPluginDirectory = pluginDirectory.Path;
            s_settings = this;
        }

        public string ExclusionFilter
        {
            get => GetSetting(s_defVals.ExclusionFilter);
            set => SetSetting(value);
        }

        public bool UseExclusionFilter
        {
            get => GetSetting(s_defVals.UseExclusionFilter);
            set => SetSetting(value);
        }

        public string PluginDirectory
        {
            get => GetSetting(GetDefaultPluginDirectory);
            set => SetSetting(value);
        }

        public Version LastLaunchedVersion
        {
            get => GetSetting(s_defVals.LastLaunchedVersion);
            set => SetSetting(value);
        }

        public bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
				return false;
#endif
            }
        }

        public string VersionString
        {
            get
            {
                PackageVersion ver = Package.Current.Id.Version;
                return $"{ver.Major}.{ver.Minor}.{ver.Build}";
            }
        }

        public event EventHandler<PackageHandlerEnabledStateChangedEventArgs> PackageHandlerEnabledStateChanged;

        public bool GetPackageHandlerEnabledState(string typeName)
        {
            return GetSetting(() => true, GetPackageHandlerEnabledKey(typeName));
        }

        public void SetPackageHandlerEnabledState(string typeName, bool enabled)
        {
            SetSetting(enabled, GetPackageHandlerEnabledKey(typeName));

            PackageHandlerEnabledStateChanged?.Invoke(this, new(typeName, enabled));
        }

        public AppUpdateStatus GetAppUpdateStatus()
        {
            if (LastLaunchedVersion == null)
                return AppUpdateStatus.NewlyInstalled;

            Version cur = Package.Current.Id.Version.ToVersion();
            if (LastLaunchedVersion == cur)
                return AppUpdateStatus.None;
            else if (LastLaunchedVersion < cur)
                return AppUpdateStatus.Updated;
            else if (LastLaunchedVersion > cur)
                return AppUpdateStatus.Downgraded;

            return AppUpdateStatus.None;
        }

        public Task ClearSettings() => Folder.RecursiveDelete();

        public async Task InstallDefaultPlugins(bool install = true, bool overwrite = false)
        {
            var appVersion = Package.Current.Id.Version.ToVersion();
            var arch = Win32Helper.GetSystemArchitecture().ToString();
            var fsApi = Ioc.Default.GetRequiredService<FluentStoreAPI.FluentStoreAPI>();
            
            var defaults = await fsApi.GetDefaultPlugins(appVersion, arch);
            await PluginLoader.InstallDefaultPlugins(this, defaults, install, overwrite);
        }

        private static string GetPackageHandlerEnabledKey(string typeName) => $"{KEY_PackageHandlerEnabled}_{typeName}";

        private string GetDefaultPluginDirectory() => m_defaultPluginDirectory;
    }
}
