using UnityEngine;
using UnityEngine.UI;

namespace Stratton.Core
{
    public static class RectTransformExtensions
    {
        #region Public Methods

        /// <summary>
        /// If isScalingToHeight is false, it will scale to width
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="isScalingToHeight"></param>
        public static void ResizeToContainer(this RectTransform rectTransform, Rect containerRect)
        {
            float aspectRatio = rectTransform.rect.width / rectTransform.rect.height;

            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;

            Vector2 containerSize = new Vector2(Mathf.Floor(containerRect.width), Mathf.Floor(containerRect.height));

            rectTransform.sizeDelta = new Vector2(Mathf.Floor(containerSize.y * aspectRatio), containerSize.y);
            if (rectTransform.sizeDelta.y > containerSize.y)
            {
                rectTransform.sizeDelta = new Vector2(containerSize.x, Mathf.Floor(containerSize.x / aspectRatio));
            }
        }

        public static void ResizeToContainer(this RectTransform rectTransform, Rect containerRect, Image image)
        {
            image.SetNativeSize();
            rectTransform.sizeDelta = image.rectTransform.sizeDelta;
            ResizeToContainer(rectTransform, containerRect);
        }

        #endregion
    }
}