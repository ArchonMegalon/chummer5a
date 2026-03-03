using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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

    [TestMethod]
    public void Web_project_does_not_reference_core_directly()
    {
        string projectPath = FindPath("Chummer.Web", "Chummer.Web.csproj");
        string text = File.ReadAllText(projectPath);

        Assert.IsFalse(text.Contains(@"..\Chummer.Core\Chummer.Core.csproj", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Character_xml_parsers_live_in_infrastructure_not_core()
    {
        string infrastructureSectionPath = FindPath("Chummer.Infrastructure", "Xml", "ICharacterSectionService.cs");
        string infrastructureFilePath = FindPath("Chummer.Infrastructure", "Xml", "ICharacterFileService.cs");
        string? coreCharactersDirectory = TryFindDirectory("Chummer.Core", "Characters");

        Assert.IsTrue(File.Exists(infrastructureSectionPath));
        Assert.IsTrue(File.Exists(infrastructureFilePath));

        if (!string.IsNullOrWhiteSpace(coreCharactersDirectory))
        {
            string coreSectionPath = Path.Combine(coreCharactersDirectory, "ICharacterSectionService.cs");
            string coreFilePath = Path.Combine(coreCharactersDirectory, "ICharacterFileService.cs");

            Assert.IsFalse(File.Exists(coreSectionPath), "Character section parser interface must not live in Chummer.Core.");
            Assert.IsFalse(File.Exists(coreFilePath), "Character file parser interface must not live in Chummer.Core.");
        }
    }

    [TestMethod]
    public void Infrastructure_project_does_not_reference_core_project()
    {
        string projectPath = FindPath("Chummer.Infrastructure", "Chummer.Infrastructure.csproj");
        string text = File.ReadAllText(projectPath);

        Assert.IsFalse(text.Contains(@"..\Chummer.Core\Chummer.Core.csproj", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Headless_project_references_follow_layering_rules()
    {
        Dictionary<string, HashSet<string>> allowedReferences = new(StringComparer.Ordinal)
        {
            ["Chummer.Contracts"] = new HashSet<string>(StringComparer.Ordinal),
            ["Chummer.Core"] = new HashSet<string>(StringComparer.Ordinal) { "Chummer.Contracts" },
            ["Chummer.Application"] = new HashSet<string>(StringComparer.Ordinal) { "Chummer.Contracts" },
            ["Chummer.Presentation"] = new HashSet<string>(StringComparer.Ordinal) { "Chummer.Contracts" },
            ["Chummer.Infrastructure"] = new HashSet<string>(StringComparer.Ordinal) { "Chummer.Application", "Chummer.Contracts" },
            ["Chummer.Web"] = new HashSet<string>(StringComparer.Ordinal) { "Chummer.Application", "Chummer.Contracts", "Chummer.Infrastructure" }
        };

        foreach ((string project, HashSet<string> allowed) in allowedReferences)
        {
            HashSet<string> actual = ReadProjectReferenceNames(project);
            List<string> forbidden = actual.Except(allowed, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToList();

            Assert.AreEqual(
                0,
                forbidden.Count,
                $"{project} has forbidden project references: {string.Join(", ", forbidden)}.");
        }
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

    private static HashSet<string> ReadProjectReferenceNames(string projectName)
    {
        string projectPath = FindPath(projectName, $"{projectName}.csproj");
        XDocument document = XDocument.Load(projectPath);

        return document
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFileNameWithoutExtension(include.Replace('\\', Path.DirectorySeparatorChar)))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.Ordinal);
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

    private static string? TryFindDirectory(params string[] parts)
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

        return null;
    }

    private static IEnumerable<string?> CandidateRoots()
    {
        yield return Environment.GetEnvironmentVariable("CHUMMER_REPO_ROOT");
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
        yield return "/src";
    }
}
