using System.Diagnostics;
using System.IO.Pipes;
using System.Management;
using System.Text.Json;
using Spheres_Lib;
using Vanara.Extensions.Reflection;
using Vanara.PInvoke;

namespace Spheres_Collect
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Spheres_Collect started.");
                FacetCollector collector = new FacetCollector();
                collector.CollectProcessInformation();
                var processTree = collector.BuildProcessTree();
                FacetCollector.SystemSetterPropertySetterRecursive(processTree);
                FacetCollector.LevelSetterRecursive(processTree, 0);
                Console.WriteLine("Process tree built");
                FacetCollector.PrintProcessTreeRecursive(processTree, 0);
                string json = JsonSerializer.Serialize(processTree);

                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("Spheres_Collect_Pipe", PipeDirection.Out))
                {
                    Console.WriteLine("Waiting for client connection...");
                    pipeServer.WaitForConnection();
                    Console.WriteLine("Client connected.");
                    using (StreamWriter writer = new StreamWriter(pipeServer))
                    {
                        writer.AutoFlush = true;
                        writer.WriteLine(json);
                    }
                    pipeServer.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }

    public class FacetCollector
    {
        private Dictionary<int, Facet> _processes = new Dictionary<int, Facet>();

        public static readonly string[] SystemProcesses = {
                "explorer.exe",
                "spheres.exe",
                "spheres_collect.exe",
                "explorer.exe",
                "textinputhost.exe",
                "immersivecontrolpanel\\systemsettings.exe",
                "system32\\ntoskrnl.exe",
                "system32\\WerFault.exe",
                "system32\\backgroundTaskHost.exe",
                "system32\\backgroundTransferHost.exe",
                "system32\\winlogon.exe",
                "system32\\wininit.exe",
                "system32\\csrss.exe",
                "system32\\lsass.exe",
                "system32\\smss.exe",
                "system32\\services.exe",
                "system32\taskeng.exe",
                "system32\taskhost.exe",
                "system32\\dwm.exe",
                "system32\\conhost.exe",
                "system32\\svchost.exe",
                "system32\\sihost.exe",
                "system32\\applicationframehost.exe",
            };

        public static void PrintProcessTreeRecursive(Facet processTree, int v)
        {
            Console.WriteLine($"{new string(' ', v * 2)}- {processTree.Name} - (PID: {processTree.ProcessId}, {processTree.ParentProcessId}, LEVEL: {processTree.Level})");
            foreach (var child in processTree.Children)
            {
                PrintProcessTreeRecursive(child, v + 1);
            }
        }

        public Facet BuildProcessTree()
        {
            var processTree = new Facet()
            {
                ProcessId = 0,
                Name = "root",
                Level = 0,
            };

            foreach (var process in _processes.Values)
            {
                if (_processes.TryGetValue(process.ParentProcessId, out var parent))
                {
                    parent.Children.Add(process);
                }
                else
                {
                    processTree.Children.Add(process);
                }
            }
            return processTree;
        }

        public static void SystemSetterPropertySetterRecursive(Facet processTree)
        {
            if (processTree == null) return;

            if (processTree.Name.ToLower() == "svchost")
            {
                processTree.SystemService = true;
                SystemServiceSetterSvchostRecursive(processTree);
            }

            if (processTree.Name?.ToLower() != "svchost")
            {
                processTree.SystemService = false;
                foreach (var child in processTree.Children)
                {
                    SystemSetterPropertySetterRecursive(child);
                }
            }
        }

        private static void SystemServiceSetterSvchostRecursive(Facet facet)
        {
            foreach (var child in facet.Children)
            {
                if (child.Handle != IntPtr.Zero) child.SystemService = false;
                else child.SystemService = true;

                SystemServiceSetterSvchostRecursive(child);
            }
        }

        public static void LevelSetterRecursive(Facet processTree, int level)
        {
            if (processTree == null) return;
            level++;
            foreach (var child in processTree.Children)
            {
                child.Level = level;
                LevelSetterRecursive(child, level);
            }
        }

        public void CollectProcessInformation()
        {
            SelectQuery selectQuery = new("Win32_Process");
            ManagementObjectSearcher searcher = new(selectQuery);
            foreach (ManagementObject process in searcher.Get())
            {
                try
                {
                    int pid = Convert.ToInt32(process["ProcessId"]);

                    var facet = new Facet()
                    {
                        ProcessId = pid,
                        ParentProcessId = Convert.ToInt32(process["ParentProcessId"]),
                        CommandLine = process["CommandLine"]?.ToString(),
                        StartTime = ManagementDateTimeConverter.ToDateTime(process["CreationDate"]?.ToString()),
                        ExecutablePath = process["ExecutablePath"]?.ToString(),
                    };

                    var proc = Process.GetProcessById(pid);
                    facet.Name = proc.ProcessName;
                    facet.Status = proc.Responding ? "Running" : "Not Responding";
                    facet.Handle = proc.MainWindowHandle;

                    Console.WriteLine($"Process: {facet.Name} (PID: {pid}, Parent PID: {facet.ParentProcessId}, Handle: {facet.Handle}");

                    if (facet.ExecutablePath != null)
                    {
                        bool blacklisted = SystemProcesses.Any(x => facet.ExecutablePath.ToLower().Contains(x.ToLower())) ? true : false;
                        facet.BlackListed = blacklisted;
                    }

                    _processes[pid] = facet;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing PID {process["ProcessId"]}: {ex.Message}");
                }
            }
        }
    }
}
