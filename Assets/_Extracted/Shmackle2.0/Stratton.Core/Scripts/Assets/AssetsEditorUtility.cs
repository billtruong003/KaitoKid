#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.AddressableAssets;

namespace Stratton.Assets
{
	public static class AssetsEditorUtility
	{
		private const string DefaultLocalGroupPath = "Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset";
		
		public static AssetReferenceSprite AddSpriteToAddressables(string spritePath, string address)
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			string assetGuid = AssetDatabase.AssetPathToGUID(spritePath);

			AddressableAssetEntry assetEntry = settings.CreateOrMoveEntry(assetGuid, AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(DefaultLocalGroupPath));
			assetEntry.address = address;

			return new AssetReferenceSprite(assetEntry.guid);
		}

		public static AssetReferenceTexture2D AddTextureToAddressables(string spritePath, string address)
		{
			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			string assetGuid = AssetDatabase.AssetPathToGUID(spritePath);

			AddressableAssetEntry assetEntry = settings.CreateOrMoveEntry(assetGuid, AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(DefaultLocalGroupPath));
			assetEntry.address = address;

			return new AssetReferenceTexture2D(assetEntry.guid);
		}
	}
}
#endif
