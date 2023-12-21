using UnityEngine;
using UnityEngine.UI;

namespace GameEngine.Util
{
    public static class GEExtension
    {
		public static void SetNativeSize(this RawImage rawImage, float scale)
		{
			Texture tex = rawImage.mainTexture;
			if (tex != null)
			{
				int w = Mathf.RoundToInt(tex.width * rawImage.uvRect.width * scale);
				int h = Mathf.RoundToInt(tex.height * rawImage.uvRect.height * scale);
				rawImage.rectTransform.anchorMax = rawImage.rectTransform.anchorMin;
				rawImage.rectTransform.sizeDelta = new Vector2(w, h);
			}
		}
	}
}