using System.Security.Cryptography;
using System.Text;
using Chummer.Contracts.Content;
using Chummer.Contracts.Rulesets;

namespace Chummer.Application.Content;

public sealed class DefaultRuntimeFingerprintService : IRuntimeFingerprintService
{
    public string ComputeResolvedRuntimeFingerprint(
        string rulesetId,
        IReadOnlyList<ContentBundleDescriptor> contentBundles,
        IReadOnlyList<RulePackRegistryEntry> rulePacks,
        IReadOnlyDictionary<string, string> providerBindings,
        string engineApiVersion)
    {
        string normalizedRulesetId = RulesetDefaults.NormalizeRequired(rulesetId);
        ArgumentNullException.ThrowIfNull(contentBundles);
        ArgumentNullException.ThrowIfNull(rulePacks);
        ArgumentNullException.ThrowIfNull(providerBindings);
        ArgumentException.ThrowIfNullOrWhiteSpace(engineApiVersion);

        StringBuilder fingerprintSource = new();
        fingerprintSource.Append("ruleset=").Append(normalizedRulesetId).Append('\n');
        fingerprintSource.Append("engine=").Append(engineApiVersion.Trim()).Append('\n');

        foreach (ContentBundleDescriptor bundle in contentBundles
                     .OrderBy(candidate => candidate.BundleId, StringComparer.Ordinal)
                     .ThenBy(candidate => candidate.Version, StringComparer.Ordinal))
        {
            fingerprintSource.Append("bundle=")
                .Append(bundle.BundleId)
                .Append('@')
                .Append(bundle.Version)
                .Append('|')
                .Append(string.Join(",", bundle.AssetPaths.OrderBy(path => path, StringComparer.Ordinal)))
                .Append('\n');
        }

        foreach (RulePackRegistryEntry rulePack in rulePacks
                     .OrderBy(candidate => candidate.Manifest.PackId, StringComparer.Ordinal)
                     .ThenBy(candidate => candidate.Manifest.Version, StringComparer.Ordinal))
        {
            RulePackManifest manifest = rulePack.Manifest;
            fingerprintSource.Append("pack=")
                .Append(manifest.PackId)
                .Append('@')
                .Append(manifest.Version)
                .Append('|')
                .Append(manifest.EngineApiVersion)
                .Append('|')
                .Append(manifest.TrustTier)
                .Append('|')
                .Append(manifest.Visibility)
                .Append('\n');

            foreach (RulePackAssetDescriptor asset in manifest.Assets
                         .OrderBy(candidate => candidate.Kind, StringComparer.Ordinal)
                         .ThenBy(candidate => candidate.Mode, StringComparer.Ordinal)
                         .ThenBy(candidate => candidate.RelativePath, StringComparer.Ordinal)
                         .ThenBy(candidate => candidate.Checksum, StringComparer.Ordinal))
            {
                fingerprintSource.Append("asset=")
                    .Append(asset.Kind)
                    .Append('|')
                    .Append(asset.Mode)
                    .Append('|')
                    .Append(asset.RelativePath)
                    .Append('|')
                    .Append(asset.Checksum)
                    .Append('\n');
            }
        }

        foreach (KeyValuePair<string, string> binding in providerBindings
                     .OrderBy(candidate => candidate.Key, StringComparer.Ordinal)
                     .ThenBy(candidate => candidate.Value, StringComparer.Ordinal))
        {
            fingerprintSource.Append("binding=")
                .Append(binding.Key)
                .Append('|')
                .Append(binding.Value)
                .Append('\n');
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource.ToString()));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
