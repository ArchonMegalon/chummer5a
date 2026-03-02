using Chummer;
using Chummer.Backend.Characters;
using Chummer.Backend.Datastructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chummer.Tests
{
    [TestClass]
    public class CharacterSettingsResolutionTests
    {
        [TestMethod]
        public void ResolveSettingsOrFallback_ReturnsFallbackWhenDictionaryIsEmpty()
        {
            var settings = Character.ResolveSettingsOrFallback(new LockingDictionary<string, CharacterSettings>());

            Assert.IsNotNull(settings);
        }

        [TestMethod]
        public void ResolveSettingsOrFallback_ReturnsFirstNonNullSettingsEntry()
        {
            var expected = new CharacterSettings();
            var dictionary = new LockingDictionary<string, CharacterSettings>();
            dictionary.TryAdd("test", expected);

            var actual = Character.ResolveSettingsOrFallback(dictionary);

            Assert.AreSame(expected, actual);
        }

        [TestMethod]
        public void ResolveSettingsOrFallback_ReturnsFallbackWhenDictionaryOnlyContainsNullEntries()
        {
            var dictionary = new LockingDictionary<string, CharacterSettings>();
            dictionary.TryAdd("null-entry", null);

            var settings = Character.ResolveSettingsOrFallback(dictionary);

            Assert.IsNotNull(settings);
        }
    }
}
