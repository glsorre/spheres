using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Vanara.PInvoke;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.System;
using Windows.System.Diagnostics;

namespace Spheres.Models
{
    public class BooleanToIconElementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isRunning)
            {
                return isRunning ? Symbol.Play : Symbol.Stop;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Symbol symbol)
            {
                return symbol == Symbol.Play;
            }
            return false;
        }
    }

    public partial class Sphere : ObservableObject, IEquatable<Sphere?>
    {
        public Sphere()
        {
            Name = "";
            Facets = new List<JsonFacet>();
        }

        [JsonConstructor]
        public Sphere(string name, string? description, string icon, List<JsonFacet> facets)
        {
            Name = name;
            Description = description;
            Icon = icon;
            Facets = facets;
        }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        [JsonPropertyName("facets")]
        public List<JsonFacet> Facets { get; set; }

        [JsonIgnore]
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRunning))]
        public List<ValueTuple<int, string, string>> processes = new();

        [JsonIgnore]
        public bool IsRunning => Processes.Count > 0;

        public override bool Equals(object? obj)
        {
            return Equals(obj as Sphere);
        }

        public bool Equals(Sphere? other)
        {
            if (other == null)
                return false;

            return EqualityComparer<List<JsonFacet>>.Default.Equals(Facets, other.Facets);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Facets);
        }

        public static bool operator ==(Sphere? left, Sphere? right)
        {
            return EqualityComparer<Sphere>.Default.Equals(left, right);
        }

        public static bool operator !=(Sphere? left, Sphere? right)
        {
            return !(left == right);
        }

        public async Task<bool> Toggle()
        {
            if (IsRunning)
            {
                await Stop();
                return false;
            }
            else
            {
                await Start();
                return true;
            }
        }

        public async Task Start()
        {
            var processesArray = await Task.WhenAll(Facets.Select(facet => Task.FromResult(facet.Start())).ToList());
            Processes = processesArray.ToList();
        }

        public async Task Stop()
        {
            var appDiagnostics = new List<AppDiagnosticInfo>();
            List<string> notClosedPids = new();

            foreach (var process in Processes)
            {
                var (pid, executable, name) = process;
                bool success = false;

                if (name != "")
                {
                    var appInfos = await AppDiagnosticInfo.RequestInfoAsync();

                    if (appInfos != null)
                    {
                        foreach (var app in appInfos)
                        {
                            var displayName = app.AppInfo.DisplayInfo.DisplayName;
                            if (displayName == name)
                            {
                                Debug.WriteLine($"Process found: {pid} - {app.AppInfo.DisplayInfo.DisplayName} - {name}");
                                var resourceGroups = app.GetResourceGroups();

                                foreach (var resourceGroup in resourceGroups)
                                {
                                    await resourceGroup.StartTerminateAsync();
                                }
                            }
                        }
                    }
                }
                else
                {
                    notClosedPids.Add($"{pid.ToString()}*\"{executable}\"");
                }
            }

            if (notClosedPids.Count > 0)
            {
                var arguments = string.Join(" ", notClosedPids.ToArray());
                Debug.WriteLine($"Processes to close: {arguments}");
                Stop_WithSpheresExit(arguments);
            }

            Processes.Clear();
            OnPropertyChanged(nameof(IsRunning));
        }

        public static void Stop_WithSpheresExit(string arguments)
        {
            string parentFolderPath = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
            string exePath = Path.Combine(parentFolderPath, "Spheres_Exit/Spheres_Exit.exe");

            var processInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = false,
                Verb = "runas",
            };

            Process? exitProcess = Process.Start(processInfo);

            if (exitProcess != null)
            {
                Debug.WriteLine($"Spheres_Exit started: {processInfo.Arguments}");
            }
            else
            {
                Debug.WriteLine("Failed to start Spheres_Exit.");
            }
        }
    }
}
