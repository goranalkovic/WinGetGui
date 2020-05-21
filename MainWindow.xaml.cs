using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ModernWpf.Controls;

namespace WinGetGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {


        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }

        Task<List<ApplicationData>> GetData()
        {
            return Task.Run(() =>
            {
                var apps = new List<ApplicationData>();

                var lines = new List<string>();

                var proc = new Process 
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "winget",
                        Arguments = "show",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                {
                    var line = proc.StandardOutput.ReadLine();
                    lines.Add(line);
                    // do something with line
                }

                foreach (var line in lines.Skip(4))
                {
                    //Debug.WriteLine(line);

                    var newAppData = new ApplicationData
                    {
                        Name = line.Substring(0, 30).Trim(),
                        Id = line.Substring(29, 44).Trim(),
                        Version = line.Substring(73).Trim()
                    };

                    apps.Add(newAppData);
                }

                return apps ;
            });
        }

        async void LoadData()
        {
            LoadingRing.IsActive = true;

            appList.ItemsSource = null;

            var apps = await GetData();
            
            appList.ItemsSource = apps;
            
            LoadingRing.IsActive = false;
        }

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private async void InstallApp(object sender, RoutedEventArgs e)
        {
            ApplicationData selected = appList.SelectedItem as ApplicationData;

            if (selected != null)
            {
                var dlg = new ContentDialog
                {
                    Title = $"Install {selected.Name}?",
                    Content = $"Version {selected.Version}",
                    PrimaryButtonText = "Install",
                    CloseButtonText = "Cancel"
                };
                var result = await dlg.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    DoInstall(selected);
                }
            }
            else
            {
                var flyout = new Flyout
                {
                    Content = new TextBlock{Text = "Select an app first!"}
                };

                flyout.ShowAt(sender as Button);
            }
        }

        private void DoInstall(ApplicationData selected)
        {
            var flyout = new Flyout
            {
                Content = new TextBlock{Text = $"Started installing {selected.Name} {selected.Version}"}
            };
                    
            flyout.ShowAt(InstallButton);

            StatusText.Text = "Install started";
            InstallButton.IsEnabled = false;

            var proc = new Process 
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"install {selected.Id}",
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (line.Contains("Download")) StatusText.Text = "Downloading";
                    if (line.Contains("MB")) StatusText.Text = $"Downloading {line.Substring(94)}";
                    if (line.Contains("hash")) StatusText.Text = "Hash valid";
                    if (line.Contains("Installing")) StatusText.Text = "Installing";
                    if (line.Contains("Failed")) StatusText.Text = "Install failed";
                });
            };

            proc.Exited += (o, args) =>
            {
         
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var text = (o as Process).ExitCode != 0
                        ? $"Installation of {selected.Name} failed"
                        : $"Installed {selected.Name} {selected.Version}";

                    var flyout2 = new Flyout
                    {
                        Content = new TextBlock { Text = text }
                    };
                    flyout2.ShowAt(InstallButton);

                    StatusText.Text = text;

                    InstallButton.IsEnabled = true;
                });
            };
                    
            proc.Start();
            proc.BeginOutputReadLine();
        }

        private void appList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InstallButton.IsEnabled = appList.SelectedIndex != -1;
        }
    }
}
