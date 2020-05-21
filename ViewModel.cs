using System.Collections.Generic;
using System.ComponentModel;

namespace WinGetGui
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string StatusText { get; set; }
        public List<ApplicationData> AvailableApps { get; set; } = new List<ApplicationData>();
        public List<ApplicationData> AvailableAppsFiltered { get; set; } = new List<ApplicationData>();
        public List<ApplicationData> InstalledApps { get; set; } = new List<ApplicationData>();
        public ApplicationData SelectedApp { get; set; }
    }
}