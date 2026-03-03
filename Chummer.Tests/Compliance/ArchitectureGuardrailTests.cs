using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Compliance;

[TestClass]
public class ArchitectureGuardrailTests
{
    private static readonly string[] HeadlessProjects =
    {
        "Chummer.Contracts",
        "Chummer.Core",
        "Chummer.Application",
        "Chummer.Infrastructure",
        "Chummer.Presentation"
    };

    private static readonly string[] BannedUiReferences =
    {
        "using System.Windows.Forms",
        "using Avalonia",
        "using Microsoft.AspNetCore.Components",
        "using Microsoft.JSInterop"
    };

    [TestMethod]
    public void Program_host_file_stays_transport_only()
    {
        string programPath = FindPath("Chummer.Web", "Program.cs");
        string text = File.ReadAllText(programPath);

        StringAssert.Contains(text, "app.MapInfoEndpoints();");
        StringAssert.Contains(text, "app.MapCharacterEndpoints();");
        StringAssert.Contains(text, "app.MapLifeModulesEndpoints();");
        StringAssert.Contains(text, "app.MapToolsEndpoints();");
        StringAssert.Contains(text, "app.MapSettingsEndpoints();");
        StringAssert.Contains(text, "app.MapRosterEndpoints();");
        StringAssert.Contains(text, "app.MapCommandEndpoints();");
        StringAssert.Contains(text, "app.MapWorkspaceEndpoints();");
        StringAssert.Contains(text, "builder.Services.AddChummerHeadlessCore(");

        Assert.IsFalse(text.Contains("Path.GetTempPath()", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("File.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("XDocument.Parse", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("public sealed record", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("app.MapPost(\"/api/characters/sections", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Chummer.Core.LifeModules", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("AddSingleton<", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Headless_projects_do_not_reference_ui_frameworks()
    {
        foreach (string project in HeadlessProjects)
        {
            string directory = FindDirectory(project);
            foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                foreach (string banned in BannedUiReferences)
                {
                    Assert.IsFalse(
                        text.Contains(banned, StringComparison.Ordinal),
                        $"Forbidden UI reference '{banned}' found in {file}.");
                }
            }
        }
    }

    [TestMethod]
    public void Tools_endpoints_stay_transport_only()
    {
        string endpointPath = FindPath("Chummer.Web", "Endpoints", "ToolsEndpoints.cs");
        string text = File.ReadAllText(endpointPath);

        Assert.IsFalse(text.Contains("Directory.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("File.", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("Path.Combine", StringComparison.Ordinal));
        Assert.IsFalse(text.Contains("XDocument", StringComparison.Ordinal));
    }

    private static string FindPath(params string[] parts)
    {
        foreach (string? root in CandidateRoots())
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;

            DirectoryInfo current = new(root);
            while (true)
            {
                string candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
                if (File.Exists(candidate))
                    return candidate;

                if (current.Parent == null)
                    break;

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate file.", Path.Combine(parts));
    }

    private static string FindDirectory(params string[] parts)
    {
        foreach (string? root in CandidateRoots())
        {
            if (string.IsNullOrWhiteSpace(root))
                continue;

            DirectoryInfo current = new(root);
            while (true)
            {
                string candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
                if (Directory.Exists(candidate))
                    return candidate;

                if (current.Parent == null)
                    break;

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate directory: " + Path.Combine(parts));
    }

    private static IEnumerable<string?> CandidateRoots()
    {
        yield return Environment.GetEnvironmentVariable("CHUMMER_REPO_ROOT");
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
        yield return "/src";
    }
}
