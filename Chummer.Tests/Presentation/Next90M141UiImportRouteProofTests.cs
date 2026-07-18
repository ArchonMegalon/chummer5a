using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests.Presentation;

[TestClass]
public sealed class Next90M141UiImportRouteProofTests
{
    [TestMethod]
    public void Materializer_emits_translator_xml_and_hero_lab_route_proof_with_direct_screenshot_tokens()
    {
        string repoRoot = FindRepoRoot();
        string scriptPath = Path.Combine(repoRoot, "scripts", "verify-next90-m141-ui-import-route-proof.py");
        string outputDirectory = Path.Combine(repoRoot, ".tmp", "m141-ui-proof-tests");
        Directory.CreateDirectory(outputDirectory);
        string outputPath = Path.Combine(outputDirectory, "NEXT90_M141_UI_IMPORT_ROUTE_PROOF.generated.json");

        RunPythonScript(scriptPath, repoRoot, outputPath);

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(outputPath));
        JsonElement root = document.RootElement;

        Assert.AreEqual("pass", root.GetProperty("status").GetString());
        Assert.AreEqual("next90-m141-ui-capture-direct-screenshot-and-runtime-proof-for-translator-xml-amendment", root.GetProperty("package_id").GetString());

        foreach (JsonProperty anchor in root.GetProperty("local_runtime_anchors").EnumerateObject())
        {
            string relativePath = anchor.Value.GetProperty("path").GetString() ?? string.Empty;
            Assert.IsFalse(Path.IsPathRooted(relativePath), $"Local proof anchor '{anchor.Name}' must be repository-relative.");
            Assert.IsTrue(
                File.Exists(Path.Combine(repoRoot, relativePath)),
                $"Local proof anchor '{anchor.Name}' must resolve inside the checked-out repository.");
        }

        JsonElement routes = root.GetProperty("route_rows");
        Assert.AreEqual(5, routes.GetArrayLength());

        JsonElement translatorRoute = routes.EnumerateArray().Single(row => row.GetProperty("id").GetString() == "source:translator_route");
        CollectionAssert.AreEquivalent(
            new[] { "38-translator-dialog-light.png" },
            translatorRoute.GetProperty("screenshots").EnumerateArray().Select(item => item.GetString()).OfType<string>().ToArray());
        StringAssert.Contains(translatorRoute.GetProperty("reason").GetString() ?? string.Empty, "38-translator-dialog-light.png");
        StringAssert.Contains(translatorRoute.GetProperty("reason").GetString() ?? string.Empty, "ExecuteCommandAsync_translator_xml_editor_and_hero_lab_importer_open_expected_dialogs");

        JsonElement xmlRoute = routes.EnumerateArray().Single(row => row.GetProperty("id").GetString() == "source:xml_amendment_editor_route");
        CollectionAssert.AreEquivalent(
            new[] { "39-xml-editor-dialog-light.png" },
            xmlRoute.GetProperty("screenshots").EnumerateArray().Select(item => item.GetString()).OfType<string>().ToArray());

        JsonElement heroLabRoute = routes.EnumerateArray().Single(row => row.GetProperty("id").GetString() == "source:hero_lab_importer_route");
        CollectionAssert.AreEquivalent(
            new[] { "40-hero-lab-importer-dialog-light.png" },
            heroLabRoute.GetProperty("screenshots").EnumerateArray().Select(item => item.GetString()).OfType<string>().ToArray());
        StringAssert.Contains(heroLabRoute.GetProperty("reason").GetString() ?? string.Empty, "CoordinateAsync_hero_lab_import_imports_workspace_and_sets_compat_notice");
    }

    private static void RunPythonScript(string scriptPath, string repoRoot, string outputPath)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "python3",
            WorkingDirectory = repoRoot,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };
        process.StartInfo.ArgumentList.Add(scriptPath);
        process.StartInfo.ArgumentList.Add("--repo-root");
        process.StartInfo.ArgumentList.Add(repoRoot);
        process.StartInfo.ArgumentList.Add("--out");
        process.StartInfo.ArgumentList.Add(outputPath);

        process.Start();
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Assert.Fail($"Materializer failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{stdout}{Environment.NewLine}stderr:{Environment.NewLine}{stderr}");
        }
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int index = 0; index < 8; index += 1)
        {
            if (File.Exists(Path.Combine(current, "Chummer.sln")))
            {
                return current;
            }

            DirectoryInfo? parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        Assert.Fail("Could not locate repository root from test base directory.");
        return string.Empty;
    }
}
