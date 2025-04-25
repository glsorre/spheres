using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
        AppInstance instance = AppInstance.FindOrRegisterForKey("Spheres");
        if (instance == null)
        {
            Console.WriteLine("Failed to register app instance.");
            return 1;
        }

        if (instance.Key == "Spheres")
        {
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        else
        {
            instance.RedirectActivationToAsync(null).AsTask().Wait();
        }

        return 0;
    }
}
