using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FlaUI.Core.WindowsAPI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Spheres;

public class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        AppInstance instance = AppInstance.FindOrRegisterForKey("Spheres_Package");
        if (instance == null)
        {
            Console.WriteLine("Failed to register app instance.");
            return 1;
        }

        if (instance.IsCurrent)
        {
            Application.Start((p) =>
            {
                var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                if (dispatcherQueue == null)
                {
                    Console.WriteLine("DispatcherQueue is null.");
                    return;
                }

                var context = new DispatcherQueueSynchronizationContext(dispatcherQueue);
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        else
        {
            var activationArgs = instance.GetActivatedEventArgs();
            instance.RedirectActivationToAsync(activationArgs).AsTask().Wait();

            IntPtr hwnd = GetMainWindowHandle(instance.ProcessId);
            if (hwnd != IntPtr.Zero)
            {
                User32.SetForegroundWindow(hwnd);
            }
        }

        return 0;
    }

    // Helper method to retrieve the main window handle of a process
    private static IntPtr GetMainWindowHandle(uint processId)
    {
        Process process = Process.GetProcessById((int)processId);
        return process.MainWindowHandle;
    }
}
