using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Cysharp.Threading.Tasks;
using Stratton.CI;
using Stratton.Core;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.Util;

namespace Stratton.Assets.Editor
{
    public class AssetsEditorSettings : ScriptableObject
    {
        public const string SettingsFileName = "AssetsEditorSettings";
        public const string SettingsDirectory = "Settings/Editor";
        public const string SettingsFullPath = "Assets/" + SettingsDirectory + "/" + SettingsFileName + ".asset";

        private static AssetsEditorSettings _instance;

        #region Serialized Fields

        [SerializeField] protected string _addressablesBuildPath = "AssetBundles/";
        [SerializeField] protected string _s3Region = "us-east-1";
        [SerializeField] protected string _s3BucketName = "";
        [SerializeField] protected string _s3FolderPath = "";
        [SerializeField] protected string _s3AccessKeyId = "";
        [SerializeField] protected string _s3SecretAccessKey = "";

        #endregion

        #region Properties

        public static AssetsEditorSettings Instance
        {
            get
            {
                return _instance ?? (_instance = Create());
            }
        }

        /// <summary>
        /// S3 region where the bucket exists.
        /// </summary>
        public string S3Region => _s3Region;
        /// <summary>
        /// S3 bucket name where the asset bundles are stored.
        /// </summary>
        public string S3BucketName => _s3BucketName;
        /// <summary>
        /// Path to your folder on the S3 bucket where the asset bundles are stored, e.g. "folder/subfolder/". Please remember about the trailing slash! You can also leave this parameter empty.
        /// </summary>
        public string S3FolderPath => _s3FolderPath;
        /// <summary>
        /// Access Key ID for the S3 bucket.
        /// </summary>
        public string S3AccessKeyId => _s3AccessKeyId;
        /// <summary>
        /// Secret Key for the S3 bucket.
        /// </summary>
        public string S3SecretAccessKey => _s3SecretAccessKey;

        #endregion

        #region Public Methods

        public void SetAddressablesActiveProfile(string profileName)
        {
            var addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;
            var profileSettings = addressableAssetSettings.profileSettings;
            var profileId = profileSettings.GetProfileId(profileName);
            addressableAssetSettings.activeProfileId = profileId;

            EditorUtility.SetDirty(addressableAssetSettings);
            AssetDatabase.SaveAssets();
        }

        public void ClearBuildFolder()
        {
            var fullBuildPath = PathUtils.MakeProjectAbsolute(_addressablesBuildPath);
            if (Directory.Exists(fullBuildPath))
            {
                Directory.Delete(fullBuildPath, true);
            }
        }

        public void FullBuild()
        {
            Log.Message(BaseLogChannel.Assets, $"Making a full content build...");
            AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
            AddressableAssetSettings.BuildPlayerContent(out var result);
            if (result.Error.IsNullOrEmpty())
            {
                var addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;
                var buildPath = addressableAssetSettings.RemoteCatalogBuildPath.GetValue(addressableAssetSettings);
                File.Copy(result.ContentStateFilePath, Path.Combine(buildPath, "addressables_content_state.bin"), true);
                var contentStateData = ContentUpdateScript.LoadContentState(result.ContentStateFilePath);
                var backupDirPath = Path.Combine(buildPath, "addressables_content_state_backup");
                if (!Directory.Exists(backupDirPath))
                {
                    Directory.CreateDirectory(backupDirPath);
                }
                File.Copy(result.ContentStateFilePath, Path.Combine(backupDirPath, $"addressables_content_state_{BuildSettings.Instance.ReleaseVersion}_{contentStateData.playerVersion}.bin"), true);
            }
        }

        public void UpdateBuild()
        {
            Log.Message(BaseLogChannel.Assets, $"Making a content update build...");
            var addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;
            // Download remote addressables_content_state.bin file for the diff (bundles from the original build)
            var contentStateDataPath = ContentUpdateScript.GetContentStateDataPath(false);
            try
            {
                if (ResourceManagerConfig.ShouldPathUseWebRequest(contentStateDataPath))
                {
                    contentStateDataPath = DownloadPreviousContentState(contentStateDataPath);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to download the addressables_content_state.bin file: {e.Message}");
            }
            var bundleFileNamesList = new List<string>();
            var contentStateData = ContentUpdateScript.LoadContentState(contentStateDataPath);
            if (contentStateData == null)
            {
                throw new Exception($"The addressables_content_state.bin file is invalid or missing!");
            }
            // Download remote catalog_XXXX.XX.XX.XX.XX.XX.json file for the diff (bundles from the last update build)
            var catalogFileName = $"catalog_{contentStateData.playerVersion}.json";
            ContentCatalogData catalogData = null;
            try
            {
                var catalogDataUrl = PathUtils.CombineUrl(AddressableAssetSettingsDefaultObject.Settings.RemoteCatalogLoadPath.GetValue(addressableAssetSettings), catalogFileName);
                catalogData = DownloadRemoteCatalog(catalogDataUrl);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to download the {catalogFileName} file: {e.Message}");
            }
            if (catalogData == null)
            {
                throw new Exception($"The {catalogFileName} file is invalid or missing!");
            }
            // TODO: Update this to make it compatible with Addressables >= 2.0
            // var catalogBundlePaths = catalogData.InternalIds.Where(i => i.EndsWith(".bundle"));
            foreach (var bundle in contentStateData.cachedBundles)
            {
                var fileName = Path.GetFileName(bundle.bundleFileId);
                if (!bundleFileNamesList.Contains(fileName))
                {
                    bundleFileNamesList.Add(fileName);
                }
            }
            // foreach (var bundlePath in catalogBundlePaths)
            // {
            //     var fileName = Path.GetFileName(bundlePath);
            //     if (!bundleFileNamesList.Contains(fileName))
            //     {
            //         bundleFileNamesList.Add(fileName);
            //     }
            // }
            var modifiedEntries = ContentUpdateScript.GatherModifiedEntriesWithDependencies(addressableAssetSettings, contentStateDataPath);
            if (modifiedEntries.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Modified entries in Cannot Change Post Release Groups were detected. The following changes were detected:");
                foreach (var entry in modifiedEntries)
                {
                    sb.AppendLine(entry.Key.AssetPath);
                }
                throw new Exception(sb.ToString());
            }
            ContentUpdateScript.BuildContentUpdate(addressableAssetSettings, contentStateDataPath);
            var buildPath = AddressableAssetSettingsDefaultObject.Settings.RemoteCatalogBuildPath.GetValue(addressableAssetSettings);
            // Remove all redundant bundles which got built but haven't changed since the last update
            foreach (var file in Directory.GetFiles(buildPath))
            {
                if (bundleFileNamesList.Contains(Path.GetFileName(file)))
                {
                    File.Delete(file);
                }
            }
        }

        public async UniTask Upload()
        {
            // if (AssetsSettings.Instance.CurrentAssetsHostType == BaseAssetsHostType.S3)
            // {
            //     var uploadTask = AssetsS3Uploader.Upload(S3AccessKeyId, S3SecretAccessKey, S3Region, S3BucketName, S3FolderPath);
            //     await uploadTask;
            // }
        }

        public List<string> GetAddressableScenePaths()
        {
            List<string> scenePaths = new List<string>();
            if (AssetsSettings.Instance.AddressablesSceneList != null)
            {
                scenePaths = AssetsSettings.Instance.AddressablesSceneList.SceneAssetReferences
                    .Select(s => AssetDatabase.GetAssetPath(s.SceneReference.editorAsset)).ToList();
            }
            return scenePaths;
        }

        #endregion

        #region Private Methods

        private static AssetsEditorSettings Create()
        {
            return AssetDatabase.LoadAssetAtPath<AssetsEditorSettings>(SettingsFullPath);
        }

        private string DownloadPreviousContentState(string path)
        {
            if (!Directory.Exists(ContentUpdateScript.PreviousContentStateFileCachePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ContentUpdateScript.PreviousContentStateFileCachePath));
            }
            else if (File.Exists(ContentUpdateScript.PreviousContentStateFileCachePath))
            {
                File.Delete(ContentUpdateScript.PreviousContentStateFileCachePath);
            }
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };
            var bytes = httpClient.GetByteArrayAsync(path).GetAwaiter().GetResult();
            File.WriteAllBytes(ContentUpdateScript.PreviousContentStateFileCachePath, bytes);
            Log.Message(BaseLogChannel.Assets, $"Successfully downloaded the previous content state!");
            return ContentUpdateScript.PreviousContentStateFileCachePath;
        }

        private ContentCatalogData DownloadRemoteCatalog(string path)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };
            var json = httpClient.GetStringAsync(path).GetAwaiter().GetResult();
            var catalog = JsonUtility.FromJson<ContentCatalogData>(json);
            Log.Message(BaseLogChannel.Assets, $"Successfully downloaded the remote catalog!");
            return catalog;
        }

        #endregion
    }
}