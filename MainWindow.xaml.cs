using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using ModernWpf.Controls;

namespace WinGetGui
{

    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Window.DataContext = new ViewModel();
            
            LoadData();

            var installedRaw = Properties.Settings.Default.InstalledApps;
            try
            {
                var deserialized = JsonSerializer.Deserialize<List<ApplicationData>>(installedRaw);

                ((ViewModel)DataContext).InstalledApps.Clear();
                ((ViewModel)DataContext).InstalledApps = deserialized;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Couldn't load already installed apps, list is empty ({e.Message})");
            }
        }

        async void LoadData()
        {
            ((ViewModel)DataContext).AvailableApps.Clear();
            ((ViewModel)DataContext).AvailableAppsFiltered.Clear();

            LoadingRing.IsActive = true;

            SetStatusText("Fetching data");

            var sw = new Stopwatch();

            sw.Start();

            var apps = await DataManager.FetchData();

            sw.Stop();

            ((ViewModel)DataContext).AvailableApps = apps;
            ((ViewModel)DataContext).AvailableAppsFiltered = apps;

            LoadingRing.IsActive = false;

            SetStatusText($"Loaded {apps.Count} packages in {sw.Elapsed.TotalSeconds:####.###}s");
        }

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private async void InstallApp(ApplicationData selected, string version = "newest")
        {
            var ver = version == "newest" ? selected.NewestVersion : version;
            var appNameAndVersion = $"{selected.Name} {ver}";

            var dlg = new ContentDialog
            {
                Title = $"Install {selected.Name}?",
                Content = $"Version {ver}",
                PrimaryButtonText = "Install",
                CloseButtonText = "Cancel"
            };
            var result = await dlg.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                LoadingRing.IsActive = true;
                SetStatusText($"Installing {appNameAndVersion}");

                InstallButton.IsEnabled = false;

                Debug.Write($"Running winget install {selected.Id} {(version == "newest" ? "" : $"-v {version}")}");

                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = $"install {selected.Id} {(version == "newest" ? "" : $"-v {version}")}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };

                proc.OutputDataReceived += (o, args) =>
                {
                    var line = args.Data ?? "";

                    Debug.WriteLine(line);

                    if (line.Contains("Download")) SetStatusText($"Downloading {appNameAndVersion}");
                    if (line.Contains("MB")) SetStatusText($"Downloading {appNameAndVersion} ({line.Substring(94)})");
                    if (line.Contains("hash")) SetStatusText($"Validated hash for {appNameAndVersion}");
                    if (line.Contains("Installing")) SetStatusText($"Installing {appNameAndVersion}");
                    if (line.Contains("Failed")) SetStatusText($"Installation of {appNameAndVersion} failed");
                };

                proc.Exited += (o, args) =>
                {

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        bool installFailed = (o as Process).ExitCode != 0;
                        var text = installFailed
                            ? $"Installation of {appNameAndVersion} failed"
                            : $"Installed {appNameAndVersion}";

                        var flyout2 = new Flyout
                        {
                            Content = new TextBlock { Text = text }
                        };
                        flyout2.ShowAt(InstallButton);

                        SetStatusText(text);

                        if (!installFailed)
                        {
                            ((ViewModel)DataContext).InstalledApps.Add(new ApplicationData
                            {
                                Id = selected.Id,
                                Name = selected.Name,
                                Versions = { version }
                            });
                        }
                        
                        LoadingRing.IsActive = false;
                        InstallButton.IsEnabled = true;
                    });
                };

                proc.Start();
                proc.BeginOutputReadLine();
            }
        }

        private void SetStatusText(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
                ((ViewModel)DataContext).StatusText = text);
        }

        private void AppSelected(object sender, SelectionChangedEventArgs e)
        {

            VersionFlyout.Items.Clear();

            var selectedApp = (DataContext as ViewModel).SelectedApp;

            if (selectedApp == null || selectedApp.Versions.Count == 1 &&
                    ((ViewModel)DataContext).InstalledApps.Count(a =>
                       a.NewestVersion == selectedApp.NewestVersion) > 0)
            {
                InstallButton.IsEnabled = false;
                InstallButton.Opacity = 0.4;
                return;
            }

            InstallButton.Opacity = 1;
            InstallButton.IsEnabled = true;

            foreach (var version in selectedApp.Versions)
            {
                var isNewest = version == selectedApp.NewestVersion;

                var menuItem = new MenuItem
                {
                    Header = $"{version} {(isNewest ? "(latest)" : "")}",
                    FontWeight = isNewest ? FontWeight.FromOpenTypeWeight(600) : FontWeight.FromOpenTypeWeight(400)
                };

                var installedApps = ((ViewModel)DataContext).InstalledApps;

                var isVersionInstalled = installedApps.Count(a =>
                    a.NewestVersion == version && a.Id == selectedApp.Id) > 0;

                menuItem.IsEnabled = !isVersionInstalled;
                menuItem.Click += (o, args) =>
                {
                    InstallApp(selectedApp, version);
                };

                VersionFlyout.Items.Add(menuItem);
                if (version == selectedApp.NewestVersion && selectedApp.Versions.Count > 1)
                    VersionFlyout.Items.Add(new Separator());
            }
        }

        private void InstallLatest(SplitButton sender, SplitButtonClickEventArgs args)
        {
            var selectedApp = (DataContext as ViewModel).SelectedApp;

            InstallApp(selectedApp);
        }

        private void Filter(object sender, TextChangedEventArgs e)
        {
            ((ViewModel)DataContext).AvailableAppsFiltered =
                ((ViewModel)DataContext).AvailableApps.Where(a =>
                   a.Name.ToLower().Contains(((TextBox)sender).Text.ToLower())).ToList();
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default["InstalledApps"] = JsonSerializer.Serialize(((ViewModel)DataContext).InstalledApps);
            Properties.Settings.Default.Save();
        }
    }
}
