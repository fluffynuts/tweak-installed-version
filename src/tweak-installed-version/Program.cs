using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

const string UNINSTALL_KEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";

var search = args.FirstOrDefault();
do
{
    var apps = Reload();
    if (apps.Length == 0 && !string.IsNullOrWhiteSpace(search))
    {
        Console.WriteLine($"No apps found matching '{search}'");
        search = PromptForSearchOrIndex();
    }
    else
    {
        Print(apps);
        var input = PromptForSearchOrIndex();

        if (int.TryParse(input, out var idx) && apps.Any(a => a.Index == idx))
        {
            var match = apps.FirstOrDefault(a => a.Index == idx);
            if (match is null)
            {
                Console.WriteLine($"No app with index {idx}");
                continue;
            }

            ManageApp(match);
        }
        else
        {
            search = input;
        }
    }
} while (true);

string PromptForSearchOrIndex()
{
    return Prompt("Enter the number of an app to alter, or text to search for to reduce this list");
}

string Prompt(string question)
{
    Console.Out.Write($"{question} > ");
    return Console.ReadLine()?.Trim() ?? "";
}

void ManageApp(AppInfo app)
{
    Console.WriteLine(app.Name);
    Console.WriteLine($"Current version: {app.Version ?? "(not set)"}");
    var newVersion = Prompt("Enter new version");
    if (string.IsNullOrWhiteSpace(newVersion))
    {
        return;
    }

    var parts = newVersion.Split('.')
        .Select(s => int.TryParse(s, out var i)
            ? new { parsed = true, i }
            : new { parsed = false, i = -1 })
        .ToArray();
    var invalid = parts.Where(o => !o.parsed).ToArray();
    if (invalid.Any() || parts.Length > 4)
    {
        Console.WriteLine($"Invalid version number: {newVersion}");
        return;
    }

    var sanitised = string.Join(".", parts.Select(o => o.i));
    using var key = Registry.LocalMachine.OpenSubKey($"{UNINSTALL_KEY}\\{app.Container}", writable: true);
    key.SetValue("DisplayVersion", sanitised, RegistryValueKind.String);
}

void Print(AppInfo[] apps)
{
    foreach (var app in apps)
    {
        Console.WriteLine($"[{app.Index}] {app.Name} ({app.Version ?? "not set"})");
    }
}

AppInfo[] Reload()
{
    using var outer = Registry.LocalMachine.OpenSubKey(UNINSTALL_KEY);
    var containers = outer.GetSubKeyNames();
    var apps = new List<AppInfo>();
    var searchParts = string.IsNullOrWhiteSpace(search)
        ? new string[0]
        : search.ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries);
    foreach (var container in containers)
    {
        using var sub = outer.OpenSubKey(container);
        var name = sub.GetValue("DisplayName")?.ToString();
        var version = sub.GetValue("DisplayVersion")?.ToString();
        if (string.IsNullOrWhiteSpace(name))
        {
            continue;
        }

        if (searchParts.Any())
        {
            var lowerName = name.ToLower();
            var matches = searchParts.Aggregate(
                true,
                (acc, cur) => acc && lowerName.Contains(cur)
            );
            if (!matches)
            {
                continue;
            }
        }

        apps.Add(new AppInfo(container, name, version));
    }

    var idx = 1;
    return apps
        .OrderBy(o => o.Name)
        .Select(a => a.SetIndex(idx++))
        .ToArray();
}