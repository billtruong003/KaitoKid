using System.Text;
using Stratton.Core;
using Stratton.Core.Types;
using UnityEngine;

namespace Stratton.CI
{
    public class BuildSettings : ScriptableObject
    {
        public const string SettingsFileName = "BuildSettings";
        public const string SettingsDirectory = "Settings/Resources";

        private static BuildSettings _instance;

        [SerializeField] private BuilderType _builderType = BuilderType.Local;
        [SerializeField] private DistributionPlatformType _distributionPlatformType = BaseDistributionPlatformType.None;
        [SerializeField] private StageType _buildStage = BaseStageType.Dev;
        [SerializeField] private int _buildNumber = 1;
        [SerializeField] private string _repoBranch = string.Empty;
        [SerializeField] private string _repoRevision = string.Empty;

        public static BuildSettings Instance
        {
            get
            {
                return _instance != null ? _instance : (_instance = Create());
            }
        }

        public BuilderType BuilderType => _builderType;
        public DistributionPlatformType DistributionPlatformType => _distributionPlatformType;
        public StageType BuildStage => _buildStage;
        public string ReleaseVersion
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.PlayerSettings.bundleVersion;
#else
                return Application.version;
#endif
            }
        }
        public int BuildNumber => _buildNumber;
        public string RepoBranch => _repoBranch;
        public string RepoRevision => _repoRevision;

        private static BuildSettings Create()
        {
            return Resources.Load<BuildSettings>(SettingsFileName);
        }

        public void SetBuildParameters(BuilderType builderType, DistributionPlatformType distributionPlatformType, StageType buildStage, string releaseVersion, int buildNumber, string repoBranch, string repoRevision)
        {
            _builderType = builderType;
            _distributionPlatformType = distributionPlatformType;
            _buildStage = buildStage;
            _buildNumber = buildNumber;
            _repoBranch = repoBranch;
            _repoRevision = repoRevision;

#if UNITY_EDITOR
            UnityEditor.PlayerSettings.bundleVersion = releaseVersion;
#endif

            if (!Application.isPlaying)
            {
                Save();
            }
            var sb = new StringBuilder();
            sb.AppendLine($"BuilderType: {_builderType}");
            sb.AppendLine($"StageType: {_buildStage}");
            sb.AppendLine($"StoreType: {_distributionPlatformType}");
            sb.AppendLine($"ReleaseVersion: {releaseVersion}");
            sb.AppendLine($"BuildNumber: {_buildNumber}");
            sb.AppendLine($"RepoBranch: {_repoBranch}");
            sb.AppendLine($"RepoRevision: {_repoRevision}");
            Log.Message(BaseLogChannel.Build, $"Settings included in the build:\n{sb}");
        }

        public void Save()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
    }
}