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
        public void ResolveSettingsOrFallback_ReturnsFallbackWhenDictionaryIsNull()
        {
            var settings = Character.ResolveSettingsOrFallback(null, "default", "fallback");

            Assert.IsNotNull(settings);
        }


        [TestMethod]
        public void ResolveSettingsOrFallback_ReturnsDefaultEntryWhenPresentAndNonNull()
        {
            var expected = new CharacterSettings();
            var dictionary = new LockingDictionary<string, CharacterSettings>();
            dictionary.TryAdd("default", expected);
            dictionary.TryAdd("fallback", new CharacterSettings());

            var actual = Character.ResolveSettingsOrFallback(dictionary, "default", "fallback");

            Assert.AreSame(expected, actual);
        }

        [TestMethod]
        public void ResolveSettingsOrFallback_ReturnsFallbackWhenDictionaryIsEmpty()
        {
            var settings = Character.ResolveSettingsOrFallback(new LockingDictionary<string, CharacterSettings>(), "default", "fallback");

            Assert.IsNotNull(settings);
        }

        [TestMethod]
        public void ResolveSettingsOrFallback_ReturnsFallbackWhenDefaultAndFallbackEntriesAreNull()
        {
            var expected = new CharacterSettings();
            var dictionary = new LockingDictionary<string, CharacterSettings>();
            dictionary.TryAdd("default", null);
            dictionary.TryAdd("fallback", null);
            dictionary.TryAdd("other", expected);

            var actual = Character.ResolveSettingsOrFallback(dictionary, "default", "fallback");

            Assert.AreSame(expected, actual);
        }
    }
}
