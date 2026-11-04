using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Temporarily presents the Stacktone mark over the game UI when the scene starts.
/// The overlay owns its own canvas, so it does not need to know anything about the
/// game UI beneath it.
/// </summary>
public class SplashScreenController : MonoBehaviour
{
  // RawImage uses the complete PNG instead of one of its imported sprite slices.
  [SerializeField] private Texture logoTexture;
  [SerializeField] private float displayDuration = 2.5f;
  [SerializeField] private float startScale = 0.0f;
  [SerializeField] private float endScale = 2.5f;

  private void Start()
  {
    StartCoroutine(ShowSplash());
  }

  private IEnumerator ShowSplash()
  {
    GameObject overlay = CreateOverlay();
    RectTransform logo = CreateLogo(overlay.transform);

    for (float elapsed = 0f; elapsed < displayDuration; elapsed += Time.unscaledDeltaTime)
    {
      float progress = Mathf.Clamp01(elapsed / displayDuration);
      // Ease out so the change is perceptible at the start but settles quietly.
      progress = 1f - Mathf.Pow(1f - progress, 3f);
      logo.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, progress);
      yield return null;
    }

    Destroy(overlay);
  }

  private GameObject CreateOverlay()
  {
    GameObject overlay = new GameObject("Splash Screen", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
    Canvas canvas = overlay.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.overrideSorting = true;
    canvas.sortingOrder = 100;

    CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1672f, 941f);
    scaler.matchWidthOrHeight = 0.5f;

    GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
    background.transform.SetParent(overlay.transform, false);
    RectTransform backgroundTransform = background.GetComponent<RectTransform>();
    backgroundTransform.anchorMin = Vector2.zero;
    backgroundTransform.anchorMax = Vector2.one;
    backgroundTransform.sizeDelta = Vector2.zero;
    // Matches the deep blue-black field in the supplied splash reference.
    background.GetComponent<Image>().color = new Color(0.0157f, 0.0431f, 0.0745f, 1f);

    return overlay;
  }

  private RectTransform CreateLogo(Transform parent)
  {
    GameObject logo = new GameObject("Stacktone Logo", typeof(RectTransform), typeof(RawImage));
    logo.transform.SetParent(parent, false);

    RawImage logoImage = logo.GetComponent<RawImage>();
    logoImage.texture = logoTexture;
    logoImage.raycastTarget = false;

    RectTransform logoTransform = logo.GetComponent<RectTransform>();
    logoTransform.anchorMin = new Vector2(0.5f, 0.5f);
    logoTransform.anchorMax = new Vector2(0.5f, 0.5f);
    logoTransform.pivot = new Vector2(0.5f, 0.5f);
    logoTransform.anchoredPosition = Vector2.zero;
    logoTransform.sizeDelta = new Vector2(520f, 292.5f);
    logoTransform.localScale = Vector3.one * startScale;

    return logoTransform;
  }
}
