using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LayerLab.ArtMaker
{
    public class PanelPreset : MonoBehaviour
    {
        #region Fields and Properties
       
        private List<PresetSlot> _presetSlots = new();
       
        #endregion

        #region Unity Lifecycle
       
        /// <summary>
        /// 초기화
        /// Initialize
        /// </summary>
        public void Init()
        {
            var index = 0;
            _presetSlots = transform.GetComponentsInChildren<PresetSlot>().ToList();
            foreach (var t in _presetSlots)
            {
                t.Init(index);
                index++;
            }
           
            DemoControl.Instance.OnPlayMode += CheckMode;
        }

        /// <summary>
        /// 오브젝트 파괴 시 이벤트 해제
        /// Unsubscribe events when object is destroyed
        /// </summary>
        private void OnDestroy()
        {
            // 싱글턴 인스턴스 null 체크 - 종료 시 파괴 순서 보장 불가 / Null check singleton instance - destruction order not guaranteed on quit
            if (DemoControl.Instance != null)
            {
                DemoControl.Instance.OnPlayMode -= CheckMode;
            }
        }
       
        #endregion

        #region Mode Management
       
        /// <summary>
        /// 플레이 모드 확인
        /// Check play mode
        /// </summary>
        /// <param name="playMode">플레이 모드 / Play mode</param>
        private void CheckMode(PlayMode playMode)
        {
            gameObject.SetActive(playMode == PlayMode.Home);
        }
       
        #endregion
    }
}