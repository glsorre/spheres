using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Spheres.ViewModels;
using Windows.System.Diagnostics;
using User32 = Vanara.PInvoke.User32;

namespace Spheres.Models
{
    public enum FacetType
    {
        App,
        File,
        Folder,
        Url
    }

    public partial class JsonFacet : IEquatable<JsonFacet?>
    {
        [JsonPropertyName("type")]
        public FacetType Type { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonIgnore]
        public BitmapImage? Picon { get; set; }

        [JsonConstructor]
        public JsonFacet(FacetType type, string content)
        {
            Type = type;
            Content = content;
        }

        public JsonFacet(AddFacetViewModel viewModel)
        {
            Type = viewModel.Type;
            Content = viewModel.Arguments != null ? viewModel.Content : $"{viewModel.Content} {viewModel.Arguments}";
        }

        public async Task<BitmapImage> GetIconAsync()
        {
            try
            {
                Icon? icon = Icon.ExtractAssociatedIcon(Content);
                if (icon == null) return null;

                using (var bitmap = icon.ToBitmap())
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;

                    var imageSource = new BitmapImage();
                    var randomAccessStream = memory.AsRandomAccessStream();
                    await imageSource.SetSourceAsync(randomAccessStream);

                    return imageSource;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting icon: {ex.Message}");
                return null;
            }
        }

        public ValueTuple<int, string, string> Start()
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = Content,
                UseShellExecute = true,
            };

            var process = Process.Start(startInfo);

            if (process != null)
            {
                var processInfo = ProcessDiagnosticInfo.TryGetForProcessId((uint)process.Id);

                Debug.WriteLine($"Process started: {process.Id} - {User32.IsImmersiveProcess(process.Handle).ToString()} - {process.StartInfo.FileName}");

                if (processInfo.IsPackaged)
                {
                    var displayName = processInfo.GetAppDiagnosticInfos().FirstOrDefault()?.AppInfo.DisplayInfo.DisplayName ?? "";
                    return (process.Id, Content, displayName);
                }

                return (process.Id, process.StartInfo.FileName, "");
            }
            else
            {
                throw new Exception("Failed to start process.");
            }
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as JsonFacet);
        }

        public bool Equals(JsonFacet? other)
        {
            return other is not null &&
                   Type == other.Type &&
                   Content == other.Content;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Content);
        }

        public static bool operator ==(JsonFacet? left, JsonFacet? right)
        {
            return EqualityComparer<JsonFacet>.Default.Equals(left, right);
        }

        public static bool operator !=(JsonFacet? left, JsonFacet? right)
        {
            return !(left == right);
        }
    }
}
