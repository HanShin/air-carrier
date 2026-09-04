using AetherArk.Content;
using AetherArk.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace AetherArk.Tests
{
    public sealed class BackgroundArtTests
    {
        [Test]
        public void EveryRegionAndFinale_HasAWidescreenRuntimeTexture()
        {
            for (var regionIndex = 1; regionIndex <= ContentCatalog.RegionCount; regionIndex++)
            {
                var region = ContentCatalog.GetRegion(regionIndex);
                var path = BackgroundArt.ResourcePath(regionIndex, false);
                Assert.That(path, Does.EndWith(region.id));

                var texture = Resources.Load<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, "Missing background for " + region.id);
                Assert.That(texture.width, Is.GreaterThan(texture.height), region.id + " must stay widescreen");
            }

            var finale = Resources.Load<Texture2D>(BackgroundArt.ResourcePath(ContentCatalog.RegionCount, true));
            Assert.That(finale, Is.Not.Null, "Missing Throne Gate finale background");
            Assert.That(finale.width, Is.GreaterThan(finale.height));
        }

        [Test]
        public void NonCampaignScreens_KeepTheOriginalFallbackBackground()
        {
            Assert.That(BackgroundArt.ResourcePath(0, false), Is.EqualTo(BackgroundArt.FallbackPath));
            Assert.That(Resources.Load<Texture2D>(BackgroundArt.FallbackPath), Is.Not.Null);
        }
    }
}
