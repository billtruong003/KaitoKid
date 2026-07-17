using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LayerLab.ArtMaker
{
   /// <summary>
   /// 플레이 모드 열거형
   /// Play mode enumeration
   /// </summary>
   public enum PlayMode
   {
       None,
       Home,
       Experience
   }

   /// <summary>
   /// 데모 전체 흐름을 제어하는 컨트롤러
   /// Controller that manages the overall demo flow
   /// </summary>
   public class DemoControl : MonoBehaviour
   {
       #region Fields and Properties
       
       public static DemoControl Instance { get; private set; }
       public Action<PlayMode> OnPlayMode { get; set; } // 플레이 모드 변경 이벤트 / Play mode change event
       public PlayMode CurrentPlayMode { get; set; } // 현재 플레이 모드 / Current play mode

       [field: SerializeField] public PanelParts PanelParts { get; set; } // 부품 패널 참조 / Parts panel reference
       [field: SerializeField] public PanelPreset PanelPreset { get; set; } // 프리셋 패널 참조 / Preset panel reference
       [field: SerializeField] public PresetData PresetData { get; set; } // 프리셋 데이터 ScriptableObject / Preset data ScriptableObject

       [SerializeField] private Sprite[] sprites; // 스프라이트 배열 / Sprite array
       [SerializeField] private Button buttonHome, buttonRandomParts, buttonExperience; // UI 버튼 / UI buttons
       [SerializeField] private GameObject buttonMouseMove; // 마우스 이동 버튼 오브젝트 / Mouse move button object
       public string pathAsset; // 에셋 경로 / Asset path

       // 스프라이트 이름 기반 캐시 - 반복 검색 최적화 / Sprite name-based cache for optimized repeated lookups
       private Dictionary<string, Sprite> _spriteCache;
       
       #endregion

       #region Unity Lifecycle
       
       /// <summary>
       /// 인스턴스 설정
       /// Set instance
       /// </summary>
       private void Awake()
       {
           Instance = this;
       }

       /// <summary>
       /// 시작 시 초기화
       /// Initialize on start
       /// </summary>
       private void Start()
       {
           ChangeMode(PlayMode.Home);
           Init();
       }
       
       #endregion

       #region Initialization
       
       /// <summary>
       /// 초기화
       /// Initialize
       /// </summary>
       public void Init()
       {
           InitializeManagers();
           OnClick_RandomParts();
       }

       /// <summary>
       /// 매니저들 초기화
       /// Initialize managers
       /// </summary>
       private void InitializeManagers()
       {
           Player.Instance.PartsManager.Init();
           CameraControl.Instance.Init();
           Player.Instance.Init();
           PanelParts.Init();
           PanelPreset.Init();
           AnimationController.Instance.Init();
           
       }
       
       #endregion

       #region Static Methods
       
       /// <summary>
       /// 부품 유형별 색상 변경 가능 여부 확인
       /// Check if color can be changed for parts type
       /// </summary>
       /// <param name="partsType">부품 유형 / Parts type</param>
       /// <returns>색상 변경 가능 여부 / Can change color</returns>
       public static bool CanChangeColor(PartsType partsType) => 
           partsType is PartsType.Hair_Short or PartsType.Brow or PartsType.Beard or PartsType.Skin;
       
       #endregion

       #region Mode Management
       
       /// <summary>
       /// 플레이 모드 변경
       /// Change play mode
       /// </summary>
       /// <param name="playMode">플레이 모드 / Play mode</param>
       public void ChangeMode(PlayMode playMode)
       {
           if (CurrentPlayMode == playMode) return;
           CurrentPlayMode = playMode;
           OnPlayMode?.Invoke(playMode);

           switch (playMode)
           {
               case PlayMode.Home:
                   SetHomeMode();
                   break;
               case PlayMode.Experience:
                   SetExperienceMode();
                   break;
               default:
                   throw new ArgumentOutOfRangeException();
           }
       }

       /// <summary>
       /// 홈 모드 UI 설정
       /// Set home mode UI
       /// </summary>
       private void SetHomeMode()
       {
           buttonMouseMove.SetActive(false);
           buttonRandomParts.gameObject.SetActive(true);
           buttonExperience.gameObject.SetActive(true);
           buttonHome.gameObject.SetActive(false);
       }

       /// <summary>
       /// 체험 모드 UI 설정
       /// Set experience mode UI
       /// </summary>
       private void SetExperienceMode()
       {
           buttonMouseMove.SetActive(true);
           buttonRandomParts.gameObject.SetActive(false);
           buttonExperience.gameObject.SetActive(false);
           buttonHome.gameObject.SetActive(true);
       }
       
       #endregion

       #region Utility Methods
       
       /// <summary>
       /// 스프라이트 가져오기
       /// Get sprite
       /// </summary>
       /// <param name="name">스프라이트 이름 / Sprite name</param>
       /// <returns>스프라이트 / Sprite</returns>
       public Sprite GetSprite(string name)
       {
           // 스프라이트 캐시 초기화 / Initialize sprite cache
           if (_spriteCache == null)
           {
               _spriteCache = new Dictionary<string, Sprite>();
               foreach (var s in sprites)
                   if (s != null) _spriteCache[s.name] = s;
           }

           // "/" 이후 이름으로 검색 / Search by name after "/"
           int slashIndex = name.IndexOf('/');
           string key = slashIndex >= 0 ? name.Substring(slashIndex + 1) : name;
           return _spriteCache.TryGetValue(key, out var sprite) ? sprite : null;
       }
       
       #endregion

       #region Button Events
       
       /// <summary>
       /// 랜덤 부품 버튼 클릭
       /// Click random parts button
       /// </summary>
       public void OnClick_RandomParts()
       {
           AudioManager.Instance.PlaySound(SoundList.ButtonRandom, 0.7f);
           PanelParts.PanelPartsList.OnClick_Close(false);
    
           // 부품 랜덤 적용 / Apply random parts
           Player.Instance.PartsManager.RandomParts();

           // 색상 랜덤 적용 / Apply random colors
           ColorPresetManager.Instance.SetRandomAllColor();

           // Hex 표시 업데이트 / Update hex display
           StartCoroutine(UpdateHexAfterRandomColors());
       }

       /// <summary>
       /// 랜덤 색상 적용 후 Hex 업데이트
       /// Update hex after applying random colors
       /// </summary>
       private System.Collections.IEnumerator UpdateHexAfterRandomColors()
       {
           yield return new WaitForEndOfFrame();
           yield return new WaitForEndOfFrame(); // 색상 적용 완료 대기 / Wait for color application to complete

           // 현재 선택된 부품의 색상으로 Hex 표시 업데이트 / Update hex display with current selected part's color
           if (ColorPicker.Instance != null)
           {
               var currentPartsType = ColorPicker.Instance.CurrentPartsType;
               if (currentPartsType != PartsType.None)
               {
                   Color currentColor = ColorPresetManager.Instance.GetColorByType(currentPartsType);
                   ColorFavoriteManager.Instance?.UpdateHexDisplay(currentColor);
               }
           }
       }
       
       /// <summary>
       /// 체험하기 버튼 클릭
       /// Click experience button
       /// </summary>
       public void OnClick_Experience()
       {
           Player.Instance.SetCollider(true);
           AudioManager.Instance.PlaySound(SoundList.ButtonDefault);
           ChangeMode(PlayMode.Experience);
       }

       /// <summary>
       /// 홈 버튼 클릭
       /// Click home button
       /// </summary>
       public void OnClick_Home()
       {
           Player.Instance.SetCollider(false);
           AudioManager.Instance.PlaySound(SoundList.ButtonDefault);
           ChangeMode(PlayMode.Home);
       }
       
       #endregion

       #region SNS Button Events
       
       /// <summary>
       /// 디스코드 버튼 클릭
       /// Click Discord button
       /// </summary>
       public void OnClick_Discord()
       {
           
       }

       /// <summary>
       /// 페이스북 버튼 클릭
       /// Click Facebook button
       /// </summary>
       public void OnClick_Facebook()
       {
           
       }


       /// <summary>
       /// 에셋 스토어 버튼 클릭
       /// Click Asset Store button
       /// </summary>
       public void OnClick_AssetStore()
       {
           
       }

       /// <summary>
       /// 에셋 버튼 클릭
       /// Click Asset button
       /// </summary>
       public void OnClick_Asset()
       {
           
       }
       
       #endregion
   }
}