using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Stratton.Core
{
    public class SceneUtils
    {
        #region Fields

        private static Dictionary<string, bool> _uiScenesLoadStatuses = new();

        #endregion

        #region Properties

        public static bool IsUIMenuSceneCachingEnabled => false;

        #endregion

        #region Public Methods

        public static bool IsSceneLoaded(string sceneName)
        {
            return SceneManager.GetSceneByName(sceneName).isLoaded;
        }

        public static void SynchronouslyLoadInternalScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        public static void SynchronouslyLoadInternalSceneAdditive(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        }

        public static async UniTask<CommonResult> LoadInternalScene(string sceneName, IProgress<float> progress = null)
        {
            return await LoadInternalScene(sceneName, LoadSceneMode.Single, progress);
        }

        public static async UniTask<CommonResult> LoadInternalSceneAdditive(string sceneName, IProgress<float> progress = null)
        {
            return await LoadInternalScene(sceneName, LoadSceneMode.Additive, progress);
        }

        public static async UniTask<CommonResult> LoadInternalScene(string sceneName, LoadSceneMode loadMode, IProgress<float> progress = null)
        {
            var handle = SceneManager.LoadSceneAsync(sceneName, loadMode);
            float lastPercent = 0;

            while (!handle.isDone)
            {
                if (handle.progress != lastPercent)
                {
                    lastPercent = handle.progress;
                    progress?.Report(lastPercent);
                }

                await UniTask.Delay(1);
            }

            progress?.Report(1f);

            return CommonResult.Success;
        }

        public static async UniTask<CommonResult> UnloadInternalScene(string sceneName, IProgress<float> progress = null)
        {
            var handle = SceneManager.UnloadSceneAsync(sceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
            float lastPercent = 0;
            while (!handle.isDone)
            {
                if (handle.progress != lastPercent)
                {
                    lastPercent = handle.progress;
                    progress?.Report(lastPercent);
                }
                await UniTask.Delay(1);
            }
            progress?.Report(1f);
            return CommonResult.Success;
        }

        public static bool IsInternalSceneLoaded(string sceneName)
        {
            return SceneManager.GetSceneByName(sceneName).isLoaded;
        }

        public static bool IsUISceneLoaded(string sceneName)
        {
            if (_uiScenesLoadStatuses.ContainsKey(sceneName))
            {
                return _uiScenesLoadStatuses[sceneName];
            }
            return false;
        }

        public static void SetUISceneLoadStatus(string sceneName, bool status)
        {
            _uiScenesLoadStatuses[sceneName] = status;
        }

        #endregion
    }
}