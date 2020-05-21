using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WinGetGui
{
    static class DataManager
    {
        public static Task<List<ApplicationData>> FetchData()
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
                }

                foreach (var line in lines.Skip(4))
                {
                    if (line.Trim().Length < 74) continue;

                    var newAppData = new ApplicationData
                    {
                        Name = line.Substring(0, 30).Trim(),
                        Id = line.Substring(29, 44).Trim()
                    };

                    apps.Add(newAppData);
                }

                Parallel.ForEach(apps,
                    currentElement =>
                    {
                        currentElement.Versions = GetVersions(currentElement.Id);
                    });

                return apps.OrderBy(x => x.Name).ToList();
            });
        }

        private static List<string> GetVersions(string id)
        {
            var versions = new List<string>();

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"show {id} --versions --exact",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                var line = proc.StandardOutput.ReadLine();
                versions.Add(line.Trim());
            }

            var output = versions.Skip(4).ToList();

            return output;
        }
    }
}