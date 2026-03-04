using System;
using System.IO;
using System.Linq;
using Chummer.Application.Content;
using Chummer.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class ContentOverlayCatalogServiceTests
{
    [TestMethod]
    public void ResolveDataFile_prefers_highest_priority_enabled_overlay()
    {
        string root = CreateTempDirectory();
        try
        {
            string baseData = Path.Combine(root, "data");
            Directory.CreateDirectory(baseData);
            File.WriteAllText(Path.Combine(baseData, "lifemodules.xml"), "<chummer><source>base</source></chummer>");

            string amendsRoot = Path.Combine(root, "Amends");
            CreatePack(amendsRoot, "pack-low", priority: 10, enabled: true, dataFileContent: "<chummer><source>low</source></chummer>");
            CreatePack(amendsRoot, "pack-high", priority: 100, enabled: true, dataFileContent: "<chummer><source>high</source></chummer>");

            var service = new FileSystemContentOverlayCatalogService(root, root, amendsRoot);

            string resolved = service.ResolveDataFile("lifemodules.xml");
            Assert.IsTrue(resolved.EndsWith(Path.Combine("pack-high", "data", "lifemodules.xml"), StringComparison.Ordinal));

            var dataDirectories = service.GetDataDirectories();
            Assert.AreEqual(3, dataDirectories.Count);
            Assert.AreEqual(Path.Combine(root, "data"), dataDirectories[0]);
            Assert.AreEqual(Path.Combine(amendsRoot, "pack-low", "data"), dataDirectories[1]);
            Assert.AreEqual(Path.Combine(amendsRoot, "pack-high", "data"), dataDirectories[2]);

            var catalog = service.GetCatalog();
            Assert.AreEqual(2, catalog.Overlays.Count);
            CollectionAssert.AreEqual(new[] { "pack-low", "pack-high" }, catalog.Overlays.Select(overlay => overlay.Id).ToArray());
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [TestMethod]
    public void Root_manifest_path_registers_single_overlay_pack()
    {
        string root = CreateTempDirectory();
        try
        {
            string baseData = Path.Combine(root, "data");
            string baseLang = Path.Combine(root, "lang");
            Directory.CreateDirectory(baseData);
            Directory.CreateDirectory(baseLang);
            File.WriteAllText(Path.Combine(baseData, "lifemodules.xml"), "<chummer><source>base</source></chummer>");
            File.WriteAllText(Path.Combine(baseLang, "en-us.xml"), "<chummer><name>English</name></chummer>");

            string amendsRoot = Path.Combine(root, "Docker", "Amends");
            Directory.CreateDirectory(Path.Combine(amendsRoot, "data"));
            Directory.CreateDirectory(Path.Combine(amendsRoot, "lang"));
            File.WriteAllText(Path.Combine(amendsRoot, "manifest.json"),
                "{\n  \"id\": \"local-test-amend\",\n  \"name\": \"Local Test Amend\",\n  \"priority\": 100,\n  \"enabled\": true\n}");

            var service = new FileSystemContentOverlayCatalogService(root, root, amendsRoot);
            var catalog = service.GetCatalog();

            Assert.AreEqual(1, catalog.Overlays.Count);
            Assert.AreEqual("local-test-amend", catalog.Overlays[0].Id);
            Assert.AreEqual(ContentOverlayModes.ReplaceFile, catalog.Overlays[0].Mode);
            Assert.AreEqual(2, service.GetLanguageDirectories().Count);
            Assert.IsTrue(service.GetLanguageDirectories().Contains(Path.Combine(amendsRoot, "lang"), StringComparer.Ordinal));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [TestMethod]
    public void Multiple_amend_roots_support_platform_path_separator()
    {
        string root = CreateTempDirectory();
        try
        {
            string baseData = Path.Combine(root, "data");
            Directory.CreateDirectory(baseData);
            File.WriteAllText(Path.Combine(baseData, "lifemodules.xml"), "<chummer><source>base</source></chummer>");

            string packOne = Path.Combine(root, "AmendsOne");
            string packTwo = Path.Combine(root, "AmendsTwo");
            CreateRootPack(packOne, "pack-one", priority: 10);
            CreateRootPack(packTwo, "pack-two", priority: 20);

            string configured = string.Join(Path.PathSeparator, packOne, packTwo);
            var service = new FileSystemContentOverlayCatalogService(root, root, configured);
            var catalog = service.GetCatalog();

            Assert.AreEqual(2, catalog.Overlays.Count);
            CollectionAssert.AreEqual(
                new[] { "pack-one", "pack-two" },
                catalog.Overlays.Select(overlay => overlay.Id).ToArray());
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [TestMethod]
    public void ResolveDataFile_ignores_merge_catalog_pack_for_full_file_replacement()
    {
        string root = CreateTempDirectory();
        try
        {
            string baseData = Path.Combine(root, "data");
            Directory.CreateDirectory(baseData);
            string baseLifeModules = Path.Combine(baseData, "lifemodules.xml");
            File.WriteAllText(baseLifeModules, "<chummer><source>base</source></chummer>");

            string amendsRoot = Path.Combine(root, "Amends");
            string packRoot = Path.Combine(amendsRoot, "pack-merge");
            Directory.CreateDirectory(Path.Combine(packRoot, "data"));
            File.WriteAllText(Path.Combine(packRoot, "manifest.json"),
                "{\n  \"id\": \"pack-merge\",\n  \"priority\": 100,\n  \"enabled\": true,\n  \"mode\": \"merge-catalog\"\n}");
            File.WriteAllText(Path.Combine(packRoot, "data", "lifemodules.xml"), "<chummer><source>overlay</source></chummer>");

            var service = new FileSystemContentOverlayCatalogService(root, root, amendsRoot);
            string resolved = service.ResolveDataFile("lifemodules.xml");

            Assert.AreEqual(baseLifeModules, resolved);
            Assert.AreEqual(ContentOverlayModes.MergeCatalog, service.GetCatalog().Overlays[0].Mode);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [TestMethod]
    public void ResolveDataFile_uses_exact_file_name_and_does_not_match_fragment_prefix()
    {
        string root = CreateTempDirectory();
        try
        {
            string baseData = Path.Combine(root, "data");
            Directory.CreateDirectory(baseData);
            string baseQualities = Path.Combine(baseData, "qualities.xml");
            File.WriteAllText(baseQualities, "<chummer><qualities /></chummer>");

            string amendsRoot = Path.Combine(root, "Amends");
            string packRoot = Path.Combine(amendsRoot, "pack-fragment");
            Directory.CreateDirectory(Path.Combine(packRoot, "data"));
            File.WriteAllText(Path.Combine(packRoot, "manifest.json"),
                "{\n  \"id\": \"pack-fragment\",\n  \"priority\": 100,\n  \"enabled\": true\n}");
            File.WriteAllText(Path.Combine(packRoot, "data", "qualities.test-amend.xml"), "<chummer><qualities /></chummer>");

            var service = new FileSystemContentOverlayCatalogService(root, root, amendsRoot);
            string resolved = service.ResolveDataFile("qualities.xml");

            Assert.AreEqual(baseQualities, resolved);
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    private static void CreatePack(string amendsRoot, string id, int priority, bool enabled, string dataFileContent)
    {
        string packRoot = Path.Combine(amendsRoot, id);
        Directory.CreateDirectory(Path.Combine(packRoot, "data"));
        File.WriteAllText(Path.Combine(packRoot, "manifest.json"),
            "{\n" +
            "  \"id\": \"" + id + "\",\n" +
            "  \"name\": \"" + id + "\",\n" +
            "  \"priority\": " + priority + ",\n" +
            "  \"enabled\": " + (enabled ? "true" : "false") + "\n" +
            "}");
        File.WriteAllText(Path.Combine(packRoot, "data", "lifemodules.xml"), dataFileContent);
    }

    private static void CreateRootPack(string rootPath, string id, int priority)
    {
        Directory.CreateDirectory(Path.Combine(rootPath, "data"));
        File.WriteAllText(Path.Combine(rootPath, "manifest.json"),
            "{\n" +
            "  \"id\": \"" + id + "\",\n" +
            "  \"name\": \"" + id + "\",\n" +
            "  \"priority\": " + priority + ",\n" +
            "  \"enabled\": true\n" +
            "}");
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "chummer-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTempDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures in tests.
        }
    }
}
