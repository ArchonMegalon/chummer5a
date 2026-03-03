using System;
using System.IO;
using System.Linq;
using Chummer.Contracts.Api;
using Chummer.Infrastructure.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests;

[TestClass]
public class ToolCatalogServiceTests
{
    [TestMethod]
    public void Master_index_reads_xml_files_and_tolerates_invalid_documents()
    {
        string root = CreateTempDirectory();
        try
        {
            string dataDir = Path.Combine(root, "data");
            Directory.CreateDirectory(dataDir);
            File.WriteAllText(Path.Combine(dataDir, "valid.xml"), "<chummer><item /><item /></chummer>");
            File.WriteAllText(Path.Combine(dataDir, "broken.xml"), "<chummer>");

            var service = new XmlToolCatalogService(root);
            MasterIndexResponse response = service.GetMasterIndex();

            Assert.AreEqual(2, response.Count);
            Assert.AreEqual(2, response.Files.Count);
            Assert.IsTrue(response.Files.Any(file => file.File == "valid.xml" && file.Root == "chummer" && file.ElementCount >= 3));
            Assert.IsTrue(response.Files.Any(file => file.File == "broken.xml" && file.Root == string.Empty && file.ElementCount == 0));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
    }

    [TestMethod]
    public void Translator_languages_reads_name_when_present_and_falls_back_to_code()
    {
        string root = CreateTempDirectory();
        try
        {
            string langDir = Path.Combine(root, "lang");
            Directory.CreateDirectory(langDir);
            File.WriteAllText(Path.Combine(langDir, "en-us.xml"), "<chummer><name>English</name></chummer>");
            File.WriteAllText(Path.Combine(langDir, "fr-fr.xml"), "<chummer><metadata /></chummer>");

            var service = new XmlToolCatalogService(root);
            TranslatorLanguagesResponse response = service.GetTranslatorLanguages();

            Assert.AreEqual(2, response.Count);
            Assert.AreEqual(2, response.Languages.Count);
            Assert.IsTrue(response.Languages.Any(language => language.Code == "en-us" && language.Name == "English"));
            Assert.IsTrue(response.Languages.Any(language => language.Code == "fr-fr" && language.Name == "fr-fr"));
        }
        finally
        {
            DeleteTempDirectory(root);
        }
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
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Ignore cleanup failures in tests.
        }
    }
}
