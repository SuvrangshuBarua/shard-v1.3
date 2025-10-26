using System;

namespace Shard
{
    internal abstract class AssetManagerBase
    {
        private String assetPath;

        public string AssetPath { get; set; }

        public abstract void registerAssets();

        public abstract string getAssetPath(string asset);
    }
}