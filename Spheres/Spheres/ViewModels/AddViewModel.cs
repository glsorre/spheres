﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using FlaUI.Core.AutomationElements;
using FuzzySharp;
using FuzzySharp.PreProcess;
using Microsoft.UI.Xaml.Controls;
using Spheres.Models;
using Spheres_Lib;
using Vanara.PInvoke;
using Process = System.Diagnostics.Process;
using User32 = Vanara.PInvoke.User32;

namespace Spheres.ViewModels;

public partial class AddViewModel : ObservableObject
{
    [ObservableProperty]
    public partial bool areRunningFacetsLoaded { get; set; }

    [ObservableProperty]
    public partial AdvancedCollectionView runningFacets { get; set; }

    [ObservableProperty]
    public partial AdvancedCollectionView sphereFacets { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCreateButtonEnabled))]
    public partial string sphereName { get; set; }

    [ObservableProperty]
    public partial string sphereDescription { get; set; }

    [ObservableProperty]
    public partial bool collectTrayFacets { get; set; }

    [ObservableProperty]
    public partial FontIcon[] icons { get; set; }

    [ObservableProperty]
    public partial FontIcon sphereIcon { get; set; }

    public bool IsCreateButtonEnabled => sphereFacets.Count > 0 & sphereName != "";

    public AddViewModel()
    {
        areRunningFacetsLoaded = false;
        collectTrayFacets = false;
        sphereName = string.Empty;
        sphereDescription = string.Empty;
        sphereDescription = string.Empty;

        icons = new FontIcon[]
        {
           new FontIcon { Glyph = "\uE80F" }, // Home  
           new FontIcon { Glyph = "\uE713" }, // Settings  
           new FontIcon { Glyph = "\uE72A" }, // Forward  
           new FontIcon { Glyph = "\uE72C" }, // Refresh  
           new FontIcon { Glyph = "\uE8B7" }, // Folder  
           new FontIcon { Glyph = "\uE753" }, // Cloud  
           new FontIcon { Glyph = "\uE77B" }, // People  
           new FontIcon { Glyph = "\uE721" }, // Search  
           new FontIcon { Glyph = "\uE72D" }, // Share  
           new FontIcon { Glyph = "\uE787" }, // Calendar  
           new FontIcon { Glyph = "\uE715" }, // Mail  
           new FontIcon { Glyph = "\uE8BD" }, // Chat  
           new FontIcon { Glyph = "\uE8A1" }, // Link  
           new FontIcon { Glyph = "\uE8EC" }, // Tag  
           new FontIcon { Glyph = "\uE707" }, // Location  
           new FontIcon { Glyph = "\uE718" }, // Pin  
           new FontIcon { Glyph = "\uEB51" }, // Heart  
           new FontIcon { Glyph = "\uE734" }, // Star  
           new FontIcon { Glyph = "\uE7C1" }, // Flag  
           new FontIcon { Glyph = "\uE722" }, // Camera  
           new FontIcon { Glyph = "\uE714" }, // Video  
           new FontIcon { Glyph = "\uE8D6" }, // Music  
           new FontIcon { Glyph = "\uE82D" }, // Lightbulb  
           new FontIcon { Glyph = "\uE91E" }, // Trophy  
           new FontIcon { Glyph = "\uE8E1" }, // Medal  
           new FontIcon { Glyph = "\uE8F0" }, // Puzzle  
           new FontIcon { Glyph = "\uE7AF" }, // Rocket  
        };

        sphereIcon = icons[0];

        runningFacets = new AdvancedCollectionView(new ObservableCollection<JsonFacet>());
        sphereFacets = new AdvancedCollectionView(new ObservableCollection<JsonFacet>());

        runningFacets.SortDescriptions.Add(new(nameof(JsonFacet.Content), SortDirection.Ascending));
        sphereFacets.SortDescriptions.Add(new(nameof(JsonFacet.Content), SortDirection.Ascending));
    }

    [RelayCommand]
    public async Task Load()
    {
        await RunCollector(collectTrayFacets);
        foreach (JsonFacet facet in runningFacets)
        {
            facet.Picon = await facet.GetIconAsync();
        }
        areRunningFacetsLoaded = true;
    }

    [RelayCommand]
    public async Task Refresh()
    {
        areRunningFacetsLoaded = false;
        runningFacets.Clear();
        sphereFacets.Clear();
        await Load();
    }

    public void AddToSphere(JsonFacet facet)
    {
        if (facet != null && !sphereFacets.Contains(facet))
        {
            sphereFacets.Add(facet);
            OnPropertyChanged(nameof(IsCreateButtonEnabled));
        }
    }

    public void AddToRunning(JsonFacet facet)
    {
        if (facet != null && !sphereFacets.Contains(facet))
        {
            sphereFacets.Add(facet);
            OnPropertyChanged(nameof(IsCreateButtonEnabled));
        }
    }

    public void RemoveFromSphere(JsonFacet facet)
    {
        if (facet != null && sphereFacets.Contains(facet))
        {
            sphereFacets.Remove(facet);
            OnPropertyChanged(nameof(IsCreateButtonEnabled));
        }
    }

    public void RemoveFromRunning(JsonFacet facet)
    {
        if (facet != null && runningFacets.Contains(facet))
        {
            runningFacets.Remove(facet);
            OnPropertyChanged(nameof(IsCreateButtonEnabled));
        }
    }

    public void swapRunningToSphere(JsonFacet facet)
    {
        if (facet != null && runningFacets.Contains(facet))
        {
            runningFacets.Remove(facet);
            sphereFacets.Add(facet);
            OnPropertyChanged(nameof(IsCreateButtonEnabled));
        }
    }

    public void swapSphereToRunning(JsonFacet facet)
    {
        if (facet != null && sphereFacets.Contains(facet))
        {
            sphereFacets.Remove(facet);
            runningFacets.Add(facet);
            OnPropertyChanged(nameof(IsCreateButtonEnabled));
        }
    }

    public async Task RunCollector(bool withTrayFacets)
    {
        try
        {
            string parentFolderPath = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
            string exePath = Path.Combine(parentFolderPath, "Spheres_Collect/Spheres_Collect.exe");

            var processInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                CreateNoWindow = false,
                Verb = "runas"
            };

            await Task.Run(() =>
            {
                try
                {
                    var process = Process.Start(processInfo);
                } catch
                {
                    Debug.WriteLine("Failed to start Spheres_Collect process.");
                    return;
                }

                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "Spheres_Collect_Pipe", PipeDirection.In))
                {
                    pipeClient.Connect();
                    using (StreamReader reader = new StreamReader(pipeClient))
                    {
                        string? data = reader.ReadLine();
                        if (data != null)
                        {
                            Facet processTree = JsonSerializer.Deserialize<Facet>(data);

                            if (processTree != null)
                            {
                                PrintProcessTreeRecursive(processTree, 0);
                                InspectProcessTreeRecursive(processTree).Wait();
                                if (withTrayFacets) InspectTrayFacets(processTree).Wait();
                            }
                            else
                            {
                                Debug.WriteLine("Failed to deserialize data from the pipe.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Received null data from the pipe.");
                        }
                    }
                    pipeClient.Dispose();
                    pipeClient.Close();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error starting collector process: {ex.Message}");
        }
    }

    public async Task InspectProcessTreeRecursive(Facet tree)
    {
        if (tree != null && tree.Content != "" && tree.Content != null && tree.SystemService == false && tree.BlackListed == false)
        {
            Process process = Process.GetProcessById(tree.ProcessId);
            bool isApp = User32.IsWindowVisible(tree.Handle) || User32.IsIconic(tree.Handle) || (User32.IsImmersiveProcess(process.Handle));

            if (isApp)
            {
                if (tree.ExecutablePath == tree.Content)
                {
                    JsonFacet facet = new JsonFacet(FacetType.App, tree.ExecutablePath);
                    runningFacets.Add(facet);
                }
                else
                {
                    JsonFacet facet = new JsonFacet(FacetType.File, tree.Content);
                    runningFacets.Add(facet);
                }
            }
        }

        foreach (var child in tree.Children)
        {
            InspectProcessTreeRecursive(child);
        }
    }

    public static void PrintProcessTreeRecursive(Facet processTree, int v)
    {
        Debug.WriteLine($"{new string(' ', v * 2)}- {processTree.Name} - (PID: {processTree.ProcessId}, {processTree.ParentProcessId}, DETAILS: {processTree.Handle}, {processTree.ExecutablePath})");
        foreach (var child in processTree.Children)
        {
            PrintProcessTreeRecursive(child, v + 1);
        }
    }

    public async Task InspectTrayFacets(Facet processTree)
    {
        List<Facet> trayFacets = new List<Facet>();

        HWND systemTray = User32.FindWindow("TopLevelWindowForOverflowXamlIsland", null);
        HWND contentBridge = User32.FindWindowEx(systemTray, 0, "Windows.UI.Composition.DesktopWindowContentBridge", null);
        HWND trayIconsWindow = User32.FindWindowEx(contentBridge, 0, "Windows.UI.Input.InputSite.WindowClass", null);

        AutomationElement trayIconsWindowElm = App.m_window.automation.FromHandle((nint)trayIconsWindow);
        AutomationElement[] trayIcons = trayIconsWindowElm.FindAllChildren();

        foreach (AutomationElement icon in trayIcons)
        {
            List<int> matchedWeights = new();
            List<Facet> matchedFacets = new();
            FindMatchingFacetsRecursive(icon.Name, processTree, matchedFacets, matchedWeights);

            Debug.WriteLine($"Tray Icon: {icon.Name} - PID: {processTree.ProcessId}, {processTree.ParentProcessId}, DETAILS: {processTree.Handle}, {processTree.ExecutablePath}");
            Debug.WriteLine($"Matched Facets: {matchedFacets.Count}");
            
            if (matchedFacets.Count > 0)
            {
                List<int> matchedWeightsRounded = matchedWeights.Select(mw => mw / 10).ToList();
                int maxWeight = matchedWeightsRounded.Max();
                List<int> weightIndex = matchedWeightsRounded.Select((weight, index) => new { weight, index })
                    .Where(x => x.weight == maxWeight)
                    .Select(x => x.index)
                    .ToList();

                Facet selected = matchedFacets.Where((_, index) => weightIndex.Contains(index)).MinBy(x => x.Level);
                JsonFacet facet = new JsonFacet(FacetType.App, selected.ExecutablePath);
                if (!runningFacets.Contains(facet)) runningFacets.Add(facet);
            }
            else
            {
                Debug.WriteLine($"No matching facets found for tray icon: {icon.Name}");
            }
        }
    }

    private static readonly string[] AppFolders = [
        "C:\\Program Files\\WindowsApps\\",
        "C:\\Program Files (x86)\\",
        "C:\\Program Files\\",
    ];

    private void FindMatchingFacetsRecursive(string name, Facet processTree, List<Facet> matchedFacets, List<int> matchedWeights)
    {

        if (processTree == null) return;

        int[] result = [];
        string token  = processTree.ExecutablePath ?? "";

        string? userFolder = System.Environment.GetEnvironmentVariable("USERPROFILE");
        string[] appFolders = AppFolders;
        if (userFolder != null) {
            appFolders = appFolders.Prepend(userFolder + "\\appdata\\local\\").ToArray();
            appFolders = appFolders.Prepend(userFolder + "\\appdata\\local\\Microsoft\\").ToArray();
        }

        for (int i = 0; i < appFolders.Length; i++)
        {
            if (token.ToLower().StartsWith(appFolders[i].ToLower()))
            {
                token = token.Substring(appFolders[i].Length);
                token = token.Split(Path.DirectorySeparatorChar).FirstOrDefault() ?? token;
            break;
            }
        }

        if (token != "" && !processTree.BlackListed) {
            int weighted = Fuzz.WeightedRatio(name, token, PreprocessMode.Full);

            if (weighted > 65)
            {
                Debug.WriteLine($"fuzzy results: {name} - {token}");
                matchedFacets.Add(processTree);
                matchedWeights.Add(weighted);
            }
        }

        foreach (var child in processTree.Children)
        {
            FindMatchingFacetsRecursive(name, child, matchedFacets, matchedWeights);
        }
    }
}

