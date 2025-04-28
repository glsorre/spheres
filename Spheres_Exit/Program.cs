using System.Diagnostics;
using System.Management;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CommandLine;
using Label = System.Windows.Forms.Label;

namespace Spheres_Collect
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Spheres_Exit started.");
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(opts => RunWithOptions(opts))
                    .WithNotParsed(errors => Console.WriteLine(errors));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
        public class Options
        {
            [Option('t', "timeout", Required = false, HelpText = "Set timeout for application to exit")]
            public int Timeout { get; set; }

            [Option('k', "kill", Required = false, HelpText = "Kill application after exit timeout elapses")]
            public bool Kill { get; set; }

            [Value(0, Required = true, HelpText = "Processes to close")]
            public required IEnumerable<string> Processes { get; set; }
        }

        private static void RunWithOptions(Options opts)
        {
            foreach (string tuple in opts.Processes)
            {
                CloseProcess(opts, tuple);
            }
        }

        static void KillProcessDialog(Process process)
        {
            Form form = new Form
            {
                Text = "Spheres",
                Width = 300,
                Height = 140,
                StartPosition = FormStartPosition.CenterScreen,
            };

            Label label = new Label
            {
                Text = $"Spheres was unable to close {process.ProcessName} gracefully.\nWould you like to kill it?",
                Dock = DockStyle.Top,
                Width = 260,
                Height = 70,
                TextAlign = System.Drawing.ContentAlignment.TopLeft
            };
            form.Controls.Add(label);

            Button cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Left = 50,
                Top = 70,
                Width = 75
            };
            form.Controls.Add(cancelButton);

            Button killButton = new Button
            {
                Text = "Kill",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Left = 150,
                Top = 70,
                Width = 75
            };
            form.Controls.Add(killButton);

            cancelButton.Click += (sender, e) =>
            {
                form.Close();
            };
            killButton.Click += (sender, e) =>
            {
                process.Kill();
                form.Close();
            };

            form.ShowDialog();
        }

        private static void CloseProcess(Options opts, string tuple)
        {
            try
            {
                string[] split = tuple.Split("*");

                int pid = int.Parse(split[0]);
                string executable = split[1].Trim('"');

                Process process = Process.GetProcessById(pid);
                bool hasExited = false;

                if (opts.Kill)
                {
                    process.Kill();
                    return;
                }
                else
                {
                    hasExited = process.CloseMainWindow();
                }

                if (!hasExited && process.Responding)
                {
                    SelectQuery selectQuery = new("Win32_Process");
                    ManagementObjectSearcher searcher = new(selectQuery);
                    foreach (ManagementObject p in searcher.Get())
                    {
                        if (p["ExecutablePath"] != null && p["ExecutablePath"].ToString() == executable)
                        {
                            Process rightProcess = Process.GetProcessById(Convert.ToInt32(p["ProcessId"]));
                            KillProcessDialog(rightProcess);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }
    }
}
