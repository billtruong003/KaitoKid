#if STDB_BINDINGS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BillGameCore;
using SpacetimeDB;
using SpacetimeDB.Types;
using LayerLab.ArtMaker;

namespace SpumOnline.UI
{
    /// <summary>
    /// Giao dien tao nhan vat dung Layer Lab (Spine 2D).
    /// Cho phep chon cac bo phan, mau sac va xem truoc thoi gian thuc
    /// truoc khi goi CreateCharacter reducer.
    /// </summary>
    public class CharacterCustomizeUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector - Input
        // -------------------------------------------------------

        [Header("Username")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_Text usernameValidationText;

        [Header("Preview Character (Layer Lab)")]
        [Tooltip("PartsManager tren nhan vat preview trong scene.")]
        [SerializeField] private PartsManager _previewParts;

        // -------------------------------------------------------
        // Part Cyclers - Prev/Next buttons + index text
        // -------------------------------------------------------

        [Header("Skin")]
        [SerializeField] private Button skinPrev;
        [SerializeField] private Button skinNext;
        [SerializeField] private TMP_Text skinIndexText;

        [Header("Eyes")]
        [SerializeField] private Button eyesPrev;
        [SerializeField] private Button eyesNext;
        [SerializeField] private TMP_Text eyesIndexText;

        [Header("Brow")]
        [SerializeField] private Button browPrev;
        [SerializeField] private Button browNext;
        [SerializeField] private TMP_Text browIndexText;

        [Header("Mouth")]
        [SerializeField] private Button mouthPrev;
        [SerializeField] private Button mouthNext;
        [SerializeField] private TMP_Text mouthIndexText;

        [Header("Hair")]
        [SerializeField] private Button hairPrev;
        [SerializeField] private Button hairNext;
        [SerializeField] private TMP_Text hairIndexText;

        [Header("Hair Hat")]
        [SerializeField] private Button hairHatPrev;
        [SerializeField] private Button hairHatNext;
        [SerializeField] private TMP_Text hairHatIndexText;

        [Header("Beard")]
        [SerializeField] private Button beardPrev;
        [SerializeField] private Button beardNext;
        [SerializeField] private TMP_Text beardIndexText;

        [Header("Top (Ao)")]
        [SerializeField] private Button topPrev;
        [SerializeField] private Button topNext;
        [SerializeField] private TMP_Text topIndexText;

        [Header("Bottom (Quan)")]
        [SerializeField] private Button bottomPrev;
        [SerializeField] private Button bottomNext;
        [SerializeField] private TMP_Text bottomIndexText;

        [Header("Boots (Giay)")]
        [SerializeField] private Button bootsPrev;
        [SerializeField] private Button bootsNext;
        [SerializeField] private TMP_Text bootsIndexText;

        [Header("Gloves (Gang tay)")]
        [SerializeField] private Button glovesPrev;
        [SerializeField] private Button glovesNext;
        [SerializeField] private TMP_Text glovesIndexText;

        [Header("Helmet (Mu)")]
        [SerializeField] private Button helmetPrev;
        [SerializeField] private Button helmetNext;
        [SerializeField] private TMP_Text helmetIndexText;

        [Header("Eyewear (Kinh)")]
        [SerializeField] private Button eyewearPrev;
        [SerializeField] private Button eyewearNext;
        [SerializeField] private TMP_Text eyewearIndexText;

        [Header("Gear Left (Vu khi trai)")]
        [SerializeField] private Button gearLeftPrev;
        [SerializeField] private Button gearLeftNext;
        [SerializeField] private TMP_Text gearLeftIndexText;

        [Header("Gear Right (Vu khi phai)")]
        [SerializeField] private Button gearRightPrev;
        [SerializeField] private Button gearRightNext;
        [SerializeField] private TMP_Text gearRightIndexText;

        [Header("Back (Lung)")]
        [SerializeField] private Button backPrev;
        [SerializeField] private Button backNext;
        [SerializeField] private TMP_Text backIndexText;

        // -------------------------------------------------------
        // Color Picker - chon 1 trong 4 muc tieu mau, dieu chinh RGB
        // -------------------------------------------------------

        [Header("Color Target Buttons")]
        [SerializeField] private Button skinColorButton;
        [SerializeField] private Button hairColorButton;
        [SerializeField] private Button beardColorButton;
        [SerializeField] private Button browColorButton;

        [Header("Color Sliders")]
        [SerializeField] private Slider colorR;
        [SerializeField] private Slider colorG;
        [SerializeField] private Slider colorB;
        [SerializeField] private Image colorPreview;
        [SerializeField] private TMP_Text colorTargetLabel;

        // -------------------------------------------------------
        // Actions
        // -------------------------------------------------------

        [Header("Actions")]
        [SerializeField] private Button createButton;
        [SerializeField] private Button randomButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Panel Animation")]
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private RectTransform panelRect;

        // -------------------------------------------------------
        // State
        // -------------------------------------------------------

        private enum ColorTarget { Skin, Hair, Beard, Brow }

        private ColorTarget _activeColorTarget = ColorTarget.Hair;
        private Color _skinColor = new Color(1f, 0.85f, 0.72f);
        private Color _hairColor = new Color(0.5f, 0.3f, 0.2f);
        private Color _beardColor = new Color(0.5f, 0.3f, 0.2f);
        private Color _browColor = new Color(0.3f, 0.2f, 0.15f);
        private bool _isCreating;

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        private void OnEnable()
        {
            AnimatePanelIn();
        }

        private void Start()
        {
            if (_previewParts != null)
            {
                _previewParts.Init();
                var skinNames = _previewParts.GetCurrentSkinNames(PartsType.Skin);
                Debug.Log($"[CharacterCustomizeUI] Init OK. Skin list count: {(skinNames != null ? skinNames.Count : 0)}");
            }
            else
            {
                Debug.LogError("[CharacterCustomizeUI] _previewParts is NULL! Wizard did not wire it.");
            }

            SetupPartButtons();
            SetupColorButtons();
            SetupActionButtons();
            UpdateAllDisplays();
            ApplyAllColorsToPreview();
        }

        // -------------------------------------------------------
        // Setup
        // -------------------------------------------------------

        private void SetupPartButtons()
        {
            BindPartCycler(skinPrev, skinNext, PartsType.Skin, skinIndexText);
            BindPartCycler(eyesPrev, eyesNext, PartsType.Eyes, eyesIndexText);
            BindPartCycler(browPrev, browNext, PartsType.Brow, browIndexText);
            BindPartCycler(mouthPrev, mouthNext, PartsType.Mouth, mouthIndexText);
            BindPartCycler(hairPrev, hairNext, PartsType.Hair_Short, hairIndexText);
            BindPartCycler(hairHatPrev, hairHatNext, PartsType.Hair_Hat, hairHatIndexText);
            BindPartCycler(beardPrev, beardNext, PartsType.Beard, beardIndexText);
            BindPartCycler(topPrev, topNext, PartsType.Top, topIndexText);
            BindPartCycler(bottomPrev, bottomNext, PartsType.Bottom, bottomIndexText);
            BindPartCycler(bootsPrev, bootsNext, PartsType.Boots, bootsIndexText);
            BindPartCycler(glovesPrev, glovesNext, PartsType.Gloves, glovesIndexText);
            BindPartCycler(helmetPrev, helmetNext, PartsType.Helmet, helmetIndexText);
            BindPartCycler(eyewearPrev, eyewearNext, PartsType.Eyewear, eyewearIndexText);
            BindPartCycler(gearLeftPrev, gearLeftNext, PartsType.Gear_Left, gearLeftIndexText);
            BindPartCycler(gearRightPrev, gearRightNext, PartsType.Gear_Right, gearRightIndexText);
            BindPartCycler(backPrev, backNext, PartsType.Back, backIndexText);
        }

        private void BindPartCycler(Button prev, Button next, PartsType type, TMP_Text display)
        {
            if (prev != null)
                prev.onClick.AddListener(() => CyclePart(type, -1, display));
            if (next != null)
                next.onClick.AddListener(() => CyclePart(type, 1, display));
        }

        private void SetupColorButtons()
        {
            if (skinColorButton != null)
                skinColorButton.onClick.AddListener(() => SelectColorTarget(ColorTarget.Skin));
            if (hairColorButton != null)
                hairColorButton.onClick.AddListener(() => SelectColorTarget(ColorTarget.Hair));
            if (beardColorButton != null)
                beardColorButton.onClick.AddListener(() => SelectColorTarget(ColorTarget.Beard));
            if (browColorButton != null)
                browColorButton.onClick.AddListener(() => SelectColorTarget(ColorTarget.Brow));

            if (colorR != null)
            {
                colorR.minValue = 0f; colorR.maxValue = 1f;
                colorR.onValueChanged.AddListener(_ => OnColorSliderChanged());
            }
            if (colorG != null)
            {
                colorG.minValue = 0f; colorG.maxValue = 1f;
                colorG.onValueChanged.AddListener(_ => OnColorSliderChanged());
            }
            if (colorB != null)
            {
                colorB.minValue = 0f; colorB.maxValue = 1f;
                colorB.onValueChanged.AddListener(_ => OnColorSliderChanged());
            }

            // Mac dinh chon Hair color
            SelectColorTarget(ColorTarget.Hair);
        }

        private void SetupActionButtons()
        {
            if (createButton != null)
                createButton.onClick.AddListener(OnCreateClicked);
            if (randomButton != null)
                randomButton.onClick.AddListener(OnRandomClicked);
        }

        // -------------------------------------------------------
        // Part Cycling
        // -------------------------------------------------------

        private void CyclePart(PartsType type, int direction, TMP_Text display)
        {
            if (_previewParts == null)
            {
                Debug.LogWarning("[CharacterCustomizeUI] CyclePart: _previewParts is null!");
                return;
            }

            Debug.Log($"[CharacterCustomizeUI] CyclePart({type}, {(direction > 0 ? "Next" : "Prev")})");

            if (direction > 0)
                _previewParts.NextItem(type);
            else
                _previewParts.PrevItem(type);

            UpdatePartDisplay(type, display);

            // Hieu ung nhan nut
            if (display != null && Bill.IsReady)
            {
                BillTween.Scale(display.transform, 1.2f, 0.08f)
                    ?.SetEase(EaseType.OutBack)
                    .SetTarget(display)
                    .OnComplete(() =>
                    {
                        BillTween.Scale(display.transform, 1f, 0.08f)
                            ?.SetTarget(display);
                    });
            }
        }

        private void UpdatePartDisplay(PartsType type, TMP_Text display)
        {
            if (display == null || _previewParts == null) return;

            int current = _previewParts.GetCurrentPartIndex(type);
            var names = _previewParts.GetCurrentSkinNames(type);
            int total = names != null ? names.Count : 1;

            display.text = current < 0 ? "None" : $"{current + 1}/{total}";
        }

        private void UpdateAllDisplays()
        {
            UpdatePartDisplay(PartsType.Skin, skinIndexText);
            UpdatePartDisplay(PartsType.Eyes, eyesIndexText);
            UpdatePartDisplay(PartsType.Brow, browIndexText);
            UpdatePartDisplay(PartsType.Mouth, mouthIndexText);
            UpdatePartDisplay(PartsType.Hair_Short, hairIndexText);
            UpdatePartDisplay(PartsType.Hair_Hat, hairHatIndexText);
            UpdatePartDisplay(PartsType.Beard, beardIndexText);
            UpdatePartDisplay(PartsType.Top, topIndexText);
            UpdatePartDisplay(PartsType.Bottom, bottomIndexText);
            UpdatePartDisplay(PartsType.Boots, bootsIndexText);
            UpdatePartDisplay(PartsType.Gloves, glovesIndexText);
            UpdatePartDisplay(PartsType.Helmet, helmetIndexText);
            UpdatePartDisplay(PartsType.Eyewear, eyewearIndexText);
            UpdatePartDisplay(PartsType.Gear_Left, gearLeftIndexText);
            UpdatePartDisplay(PartsType.Gear_Right, gearRightIndexText);
            UpdatePartDisplay(PartsType.Back, backIndexText);
        }

        // -------------------------------------------------------
        // Color Picker
        // -------------------------------------------------------

        private void SelectColorTarget(ColorTarget target)
        {
            _activeColorTarget = target;

            Color c = GetColor(target);
            if (colorR != null) colorR.SetValueWithoutNotify(c.r);
            if (colorG != null) colorG.SetValueWithoutNotify(c.g);
            if (colorB != null) colorB.SetValueWithoutNotify(c.b);
            if (colorPreview != null) colorPreview.color = c;

            if (colorTargetLabel != null)
            {
                colorTargetLabel.text = target switch
                {
                    ColorTarget.Skin => "Da",
                    ColorTarget.Hair => "Toc",
                    ColorTarget.Beard => "Rau",
                    ColorTarget.Brow => "Long may",
                    _ => ""
                };
            }
        }

        private Color GetColor(ColorTarget target)
        {
            return target switch
            {
                ColorTarget.Skin => _skinColor,
                ColorTarget.Hair => _hairColor,
                ColorTarget.Beard => _beardColor,
                ColorTarget.Brow => _browColor,
                _ => Color.white
            };
        }

        private void SetColor(ColorTarget target, Color c)
        {
            switch (target)
            {
                case ColorTarget.Skin: _skinColor = c; break;
                case ColorTarget.Hair: _hairColor = c; break;
                case ColorTarget.Beard: _beardColor = c; break;
                case ColorTarget.Brow: _browColor = c; break;
            }
        }

        private void OnColorSliderChanged()
        {
            Color c = new Color(
                colorR != null ? colorR.value : 0.5f,
                colorG != null ? colorG.value : 0.3f,
                colorB != null ? colorB.value : 0.2f,
                1f
            );

            SetColor(_activeColorTarget, c);
            if (colorPreview != null) colorPreview.color = c;

            ApplyColorToPreview(_activeColorTarget, c);
        }

        private void ApplyColorToPreview(ColorTarget target, Color c)
        {
            if (_previewParts == null) return;

            switch (target)
            {
                case ColorTarget.Skin: _previewParts.ChangeSkinColor(c); break;
                case ColorTarget.Hair: _previewParts.ChangeHairColor(c); break;
                case ColorTarget.Beard: _previewParts.ChangeBeardColor(c); break;
                case ColorTarget.Brow: _previewParts.ChangeBrowColor(c); break;
            }
        }

        private void ApplyAllColorsToPreview()
        {
            if (_previewParts == null) return;
            _previewParts.ChangeSkinColor(_skinColor);
            _previewParts.ChangeHairColor(_hairColor);
            _previewParts.ChangeBeardColor(_beardColor);
            _previewParts.ChangeBrowColor(_browColor);
        }

        // -------------------------------------------------------
        // Random
        // -------------------------------------------------------

        private void OnRandomClicked()
        {
            if (_previewParts == null) return;

            _previewParts.RandomParts();
            UpdateAllDisplays();

            // Hieu ung nut Random
            if (randomButton != null && Bill.IsReady)
            {
                BillTween.Scale(randomButton.transform, 1.15f, 0.1f)
                    ?.SetEase(EaseType.OutBack)
                    .SetTarget(randomButton)
                    .OnComplete(() =>
                    {
                        BillTween.Scale(randomButton.transform, 1f, 0.1f)
                            ?.SetTarget(randomButton);
                    });
            }
        }

        // -------------------------------------------------------
        // Create Character
        // -------------------------------------------------------

        private void OnCreateClicked()
        {
            if (_isCreating) return;

            // Validate username
            string username = usernameInput != null ? usernameInput.text?.Trim() : "";
            if (string.IsNullOrEmpty(username))
            {
                ShowValidationError("Vui long nhap ten nhan vat.");
                return;
            }
            if (username.Length < 2)
            {
                ShowValidationError("Ten phai co it nhat 2 ky tu.");
                return;
            }
            if (username.Length > 20)
            {
                ShowValidationError("Ten khong duoc qua 20 ky tu.");
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null || !gm.IsConnected)
            {
                ShowStatus("Chua ket noi server.");
                return;
            }

            _isCreating = true;
            ShowStatus("Dang tao nhan vat...");
            if (createButton != null) createButton.interactable = false;

            // Lay chi so bo phan tu preview
            int skin = _previewParts != null ? _previewParts.GetCurrentPartIndex(PartsType.Skin) : 0;
            int eyes = _previewParts != null ? _previewParts.GetCurrentPartIndex(PartsType.Eyes) : 0;
            int brow = _previewParts != null ? _previewParts.GetCurrentPartIndex(PartsType.Brow) : 0;
            int mouth = _previewParts != null ? _previewParts.GetCurrentPartIndex(PartsType.Mouth) : 0;
            int hairShort = _previewParts != null ? _previewParts.GetCurrentPartIndex(PartsType.Hair_Short) : 0;
            int beard = _previewParts != null ? _previewParts.GetCurrentPartIndex(PartsType.Beard) : -1;
            int top = _previewParts != null ? _previewParts.GetCurrentPartIndex(PartsType.Top) : 0;
            int bottom = _previewParts != null ? _previewParts.GetCurrentPartIndex(PartsType.Bottom) : 0;
            int boots = _previewParts != null ? _previewParts.GetCurrentPartIndex(PartsType.Boots) : 0;

            // Dong goi mau sac
            int skinColor = CharacterVisualSync.PackColor(_skinColor);
            int hairColor = CharacterVisualSync.PackColor(_hairColor);
            int beardColor = CharacterVisualSync.PackColor(_beardColor);
            int browColor = CharacterVisualSync.PackColor(_browColor);

            // Goi reducer - khop voi server signature:
            // CreateCharacter(string, int skin, int eyes, int brow, int mouth,
            //   int hairShort, int beard, int top, int bottom, int boots,
            //   int skinColor, int hairColor, int beardColor, int browColor)
            gm.Connection.Reducers.CreateCharacter(
                username,
                skin, eyes, brow, mouth,
                hairShort, beard, top, bottom, boots,
                skinColor, hairColor, beardColor, browColor
            );

            // Cho phan hoi tu server
            if (Bill.IsReady)
            {
                Bill.Events.SubscribeOnce<SubscriptionAppliedEvent>(_ =>
                {
                    _isCreating = false;
                    if (createButton != null) createButton.interactable = true;
                });

                // Timeout 5 giay
                Bill.Timer.Delay(5f, () =>
                {
                    if (_isCreating)
                    {
                        _isCreating = false;
                        if (createButton != null) createButton.interactable = true;
                        ShowStatus("Tao nhan vat that bai. Thu lai.");
                    }
                });
            }

            // Vao game sau khi tao thanh cong
            if (Bill.IsReady)
            {
                Bill.Timer.Delay(1f, () =>
                {
                    if (gm.LocalPlayerPosition != null)
                    {
                        gm.Connection.Reducers.EnterWorld();
                        Bill.Scene.Load("GameWorld");
                    }
                });
            }
        }

        // -------------------------------------------------------
        // UI Feedback
        // -------------------------------------------------------

        private void ShowValidationError(string message)
        {
            if (usernameValidationText != null)
            {
                usernameValidationText.text = message;
                usernameValidationText.color = Color.red;

                // Hieu ung rung
                if (usernameInput != null && Bill.IsReady)
                {
                    RectTransform rt = usernameInput.GetComponent<RectTransform>();
                    float originalX = rt.anchoredPosition.x;

                    BillTween.Float(0f, 1f, 0.4f, t =>
                    {
                        float offset = Mathf.Sin(t * Mathf.PI * 6f) * 5f * (1f - t);
                        rt.anchoredPosition = new Vector2(originalX + offset, rt.anchoredPosition.y);
                    })?.SetTarget(rt);
                }
            }
        }

        private void ShowStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        // -------------------------------------------------------
        // Panel Animation
        // -------------------------------------------------------

        private void AnimatePanelIn()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                BillTween.Fade(panelCanvasGroup, 1f, 0.4f)
                    ?.SetEase(EaseType.OutQuad);
            }

            if (panelRect != null)
            {
                panelRect.localScale = Vector3.one * 0.85f;
                BillTween.Scale(panelRect, 1f, 0.35f)
                    ?.SetEase(EaseType.OutBack);
            }
        }

        /// <summary>
        /// Hieu ung dong panel (truoc khi chuyen scene).
        /// </summary>
        public void AnimatePanelOut(System.Action onComplete = null)
        {
            if (panelCanvasGroup != null)
            {
                BillTween.Fade(panelCanvasGroup, 0f, 0.3f)
                    ?.SetEase(EaseType.InQuad)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}

#endif // STDB_BINDINGS
