using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Spheres.ViewModels;
using Vanara.PInvoke;
using Windows.Storage;
using Windows.System;
using WinRT.Interop;


namespace Spheres.Models
{
    public class SphereJsonConverter : JsonConverter<Sphere>
    {
        public override Sphere Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string name = "";
            string? description = null;
            string icon = "";
            List<JsonFacet> facets = new();
            bool launchatboot = false;
            int launchinterval = 0;
            VirtualKey key = VirtualKey.None;
            VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    continue;
                }
                string propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "name":
                        name = reader.GetString();
                        break;
                    case "description":
                        description = reader.GetString();
                        break;
                    case "icon":
                        icon = reader.GetString();
                        break;
                    case "facets":
                        facets = JsonSerializer.Deserialize<List<JsonFacet>>(ref reader, options) ?? new List<JsonFacet>();
                        break;
                    case "launchatboot":
                        launchatboot = reader.GetBoolean();
                        break;
                    case "launchinterval":
                        launchinterval = reader.GetInt32();
                        break;
                    case "key":
                        var converter = new KeyToStringConverter();
                        key = (VirtualKey)converter.ConvertBack(reader.GetString(), typeof(VirtualKey), null, null);
                        break;
                    case "modifiers":
                        modifiers = (VirtualKeyModifiers)Enum.Parse(typeof(VirtualKeyModifiers), reader.GetString());
                        break;
                }
            }

            return new Sphere(name, description, icon, new ObservableCollection<JsonFacet>(facets), launchatboot, launchinterval, key, modifiers);
        }

        public override void Write(Utf8JsonWriter writer, Sphere value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("name", value.Name);
            writer.WriteString("description", value.Description);
            writer.WriteString("icon", value.Icon);
            writer.WritePropertyName("facets");
            JsonSerializer.Serialize(writer, value.Facets, options);
            writer.WriteBoolean("launchatboot", value.LaunchAtBoot);
            writer.WriteNumber("launchinterval", value.LaunchInterval);
            writer.WriteString("key", value.Key.ToString());
            writer.WriteString("modifiers", value.Modifiers.ToString());
            writer.WriteEndObject();
        }
    }

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
        }

        public Sphere(
            string name,
            string? description,
            string icon, 
            ObservableCollection<JsonFacet> facets,
            bool launchatboot = false,
            int launchinterval = 0,
            VirtualKey key = VirtualKey.None,
            VirtualKeyModifiers modifiers = VirtualKeyModifiers.None
            )
        {
            Name = name;
            Description = description;
            Icon = icon;
            Facets = facets;
            LaunchAtBoot = launchatboot;
            LaunchInterval = launchinterval;
            Key = key;
            Modifiers = modifiers;
            Processes = [];
        }

        public string Name { get; set; }

        public string? Description { get; set; }

        public string Icon { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<JsonFacet> Facets { get; set; }

        public bool LaunchAtBoot { get; set; }

        public int LaunchInterval { get; set; }

        public VirtualKey Key { get; set; }

        public  VirtualKeyModifiers Modifiers { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRunning))]
        public partial ObservableCollection<ValueTuple<int, string, string>> Processes { get; set; }

        public bool IsRunning => Processes.Count > 0;

        public bool HotKeyRegistered = false;

        public async Task Init()
        {
            var tasks = Facets.Select(facet => facet.GetIconAsync()).ToList();
            var icons = await Task.WhenAll(tasks);
            for (int i = 0; i < Facets.Count; i++)
            {
                Facets[i].Picon = icons[i];
            }
        }

        public async Task Load()
        {
            if (LaunchAtBoot)
            {
                await Start();
            }

            RegisterHotKey();
        }

        public void RegisterHotKey()
        {
            if (Key != VirtualKey.None)
            {
                IntPtr windowHandle = WindowNative.GetWindowHandle(App.m_window);
                HotKeyRegistered = User32.RegisterHotKey(windowHandle, GetHashCode(), GetModifiers(Modifiers), (uint)Key);

                if (HotKeyRegistered)
                {
                    Debug.WriteLine($"Hotkey registered: {GetHashCode()} - {Key} - {Modifiers}");
                }
            }
        }

        public void UnregisterHotKey()
        {
            if (HotKeyRegistered)
            {
                IntPtr windowHandle = WindowNative.GetWindowHandle(App.m_window);
                User32.UnregisterHotKey(windowHandle, GetHashCode());
                HotKeyRegistered = false;
            }
        }

        public void UpdateProcesses(List<ValueTuple<int, string, string>> processes)
        {
            Processes.Clear();
            Processes = [..processes];
            OnPropertyChanged(nameof(IsRunning));
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
            if (LaunchInterval > 0 && Facets.Count > 1)
            {
                foreach (var facet in Facets)
                {
                    Processes.Add(facet.Start());
                    await Task.Delay(LaunchInterval);
                }
            }
            else
            {
                var ProcessesArray = await Task.WhenAll(Facets.Select(facet => Task.FromResult(facet.Start())).ToList());
                Processes = [.. ProcessesArray];
            }
        }

        public async Task Stop()
        {
            var appDiagnostics = new List<AppDiagnosticInfo>();
            List<string> notClosedPids = new();

            foreach (var process in Processes)
            {
                var (pid, executable, name) = process;

                Debug.WriteLine($"Process to find: {pid} - {executable} {name}");

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
                Stop_WithSpheresExit(arguments);
            }

            Processes.Clear();
            OnPropertyChanged(nameof(IsRunning));
        }

        public async Task AddFacet(JsonFacet facet)
        {
            if (facet is not null)
            {
                facet.Picon = await facet.GetIconAsync();
                Facets.Add(facet);
                OnPropertyChanged(nameof(Facets));
            }
        }

        public void RemoveFacet(JsonFacet facet)
        {
            if (facet is not null)
            {
                Facets.Remove(facet);
                OnPropertyChanged(nameof(Facets));
            }
        }

        public void SetKey(VirtualKey key)
        {
            Key = key;
            OnPropertyChanged(nameof(Key));
        }

        public void SetModifiers(VirtualKeyModifiers modifiers)
        {
            Modifiers = modifiers;
            OnPropertyChanged(nameof(Modifiers));
        }

        public static Process? Stop_WithSpheresExit(string arguments)
        {
            Debug.WriteLine($"Stop_WithSpheresExit: {arguments}");

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

            return Process.Start(processInfo);
        }

        public async Task Save()
        {
            UnregisterHotKey();

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;

            StorageFile? file = await localFolder.TryGetItemAsync($"sphere_{JsonNamingPolicy.CamelCase.ConvertName(Name)}.json") as StorageFile;
            bool newFile = false;


            if (file == null)
            {
                file = await localFolder.CreateFileAsync($"sphere_{JsonNamingPolicy.CamelCase.ConvertName(Name)}.tmp", CreationCollisionOption.ReplaceExisting);
                newFile = true;
            }

            using (var stream = await file.OpenStreamForWriteAsync())
            {
                SphereJsonConverter converter = new SphereJsonConverter();
                var options = new JsonSerializerOptions();
                stream.SetLength(0);

                using (var writer = new Utf8JsonWriter(stream))
                {
                    converter.Write(writer, this, options);
                }
            }

            if (newFile)
            {
                await file.RenameAsync($"sphere_{JsonNamingPolicy.CamelCase.ConvertName(Name)}.json", NameCollisionOption.ReplaceExisting);
            }

            RegisterHotKey();
        }

        public async Task Delete()
        {
            UnregisterHotKey();
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile? file = await localFolder.TryGetItemAsync($"sphere_{JsonNamingPolicy.CamelCase.ConvertName(Name)}.json") as StorageFile;
            if (file != null)
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Sphere);
        }

        public bool Equals(Sphere? other)
        {
            return other is not null &&
                   Name == other.Name &&
                   Description == other.Description &&
                   Facets.SequenceEqual(other.Facets);
        }

        public override int GetHashCode()
        {
            int facetsHashCode = Facets.Aggregate(0, (hash, facet) => HashCode.Combine(hash, facet.GetHashCode()));
            return HashCode.Combine(Name, Description, facetsHashCode);
        }

        public static bool operator ==(Sphere? left, Sphere? right)
        {
            return EqualityComparer<Sphere>.Default.Equals(left, right);
        }

        public static bool operator !=(Sphere? left, Sphere? right)
        {
            return !(left == right);
        }

        public static User32.HotKeyModifiers GetModifiers(VirtualKeyModifiers modifiers)
        {
            User32.HotKeyModifiers result = 0;
            if (modifiers.HasFlag(VirtualKeyModifiers.Control))
                result |= User32.HotKeyModifiers.MOD_CONTROL;
            if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
                result |= User32.HotKeyModifiers.MOD_SHIFT;
            if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
                result |= User32.HotKeyModifiers.MOD_ALT;
            if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
                result |= User32.HotKeyModifiers.MOD_WIN;
            return result;
        }
    }
}
