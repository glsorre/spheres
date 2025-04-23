using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Spheres_Lib
{
    public class IntPtrConverter : JsonConverter<IntPtr>
    {
        public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return new IntPtr(reader.GetInt64());
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                if (long.TryParse(value, out long result))
                {
                    return new IntPtr(result);
                }
            }
            throw new JsonException("Unable to convert value to IntPtr");
        }

        public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.ToInt64());
        }
    }


    public class Facet : IEquatable<Facet>
    {
        [JsonPropertyName("pid")]
        public int ProcessId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("parent_pid")]
        public int ParentProcessId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("start_time")]
        public DateTime StartTime { get; set; }

        [JsonPropertyName("exe")]
        public string ExecutablePath { get; set; }

        [JsonPropertyName("cmd")]
        public string CommandLine { get; set; }

        [JsonPropertyName("system_service")]
        public bool SystemService { get; set; }

        [JsonPropertyName("blacklisted")]
        public bool BlackListed { get; set; }

        [JsonConverter(typeof(IntPtrConverter))]
        public IntPtr Handle {  get; set; }

        [JsonPropertyName("children")]
        public List<Facet> Children { get; set; } = new List<Facet>();

        [JsonIgnore]
        public Bitmap? Picon { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonIgnore]
        public string Content
        {
            get
            {
                if (CommandLine != null && CommandLine != "")
                {
                    var pattern = @"(([a-z]:|\\\\[a-z0-9_.$]+\\[a-z0-9_.$]+)?(\\?(?:[^\\/:*?""<>|\r\n]+\\)+)[^\\/:*?""<>|\r\n]+)";
                    MatchCollection matches = Regex.Matches(CommandLine, pattern, RegexOptions.IgnoreCase);
                    List<string> matchesList = matches.Cast<Match>().Select(m => m.Value).ToList();

                    if (matches.Count > 1 && ExecutablePath != null)
                    {
                        matchesList.Remove(ExecutablePath);
                        bool isFolder = Directory.Exists(matchesList.FirstOrDefault());
                        return isFolder ? ExecutablePath : matchesList.FirstOrDefault();
                    }
                }
                return ExecutablePath;
            }
        }

        public Bitmap GetIconAsync()
        {
            Icon? icon = Icon.ExtractAssociatedIcon(ExecutablePath);
            if (icon == null) return null;
            return icon.ToBitmap();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Facet);
        }

        public bool Equals(Facet other)
        {
            return other is not null &&
                   ProcessId == other.ProcessId &&
                   Name == other.Name &&
                   ParentProcessId == other.ParentProcessId &&
                   ExecutablePath == other.ExecutablePath &&
                   CommandLine == other.CommandLine &&
                   Handle.Equals(other.Handle) &&
                   EqualityComparer<List<Facet>>.Default.Equals(Children, other.Children);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProcessId, Name, ParentProcessId, ExecutablePath, CommandLine, Handle, Children);
        }

        public static bool operator ==(Facet left, Facet right)
        {
            return EqualityComparer<Facet>.Default.Equals(left, right);
        }

        public static bool operator !=(Facet left, Facet right)
        {
            return !(left == right);
        }
    }
}
