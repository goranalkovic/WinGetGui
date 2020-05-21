using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace WinGetGui
{
    public class ApplicationData
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public List<string> Versions { get; set; } = new List<string>();

        public string NewestVersion => Versions.FirstOrDefault();
        public string PastVersionCount => Versions.Count > 1 ? $"+ {Versions.Count - 1} more" : "";
        
        public override string ToString() => $"{Name} {NewestVersion} ({Id})";
    }

    
}
