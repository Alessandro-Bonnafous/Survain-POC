using UnityEngine;
using UnityEngine.UI;

namespace Survain.UI
{
    /// <summary>
    /// UI d'ambiance de la zone sauvage (#18) : une bannière centrée transitoire ("Vous entrez en
    /// zone sauvage") + un léger voile rouge en plein écran tant que le joueur est dans la zone.
    ///
    /// Singleton auto-créé au premier accès (même pattern qu'<see cref="InteractionPrompt"/>) :
    /// aucun setup côté scène, Canvas + éléments construits par code. Piloté par <c>WildZone</c>
    /// (trigger d'entrée/sortie). uGUI Text (pas TMP) pour rester sans dépendance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WildZoneBanner : MonoBehaviour
    {
        private static WildZoneBanner _instance;

        public static WildZoneBanner Instance
        {
            get { if (_instance == null) CreateInstance(); return _instance; }
        }

        private const float VisibleSeconds = 2.5f; // durée pleine avant le fondu
        private const float FadeSeconds = 0.8f;

        private Text _banner;
        private RawImage _overlay;
        private float _hideAt;

        private static void CreateInstance()
        {
            var go = new GameObject("_WildZoneBanner");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<WildZoneBanner>();
            _instance.BuildUI();
        }

        /// <summary>Affiche une bannière centrée qui s'estompe après un délai.</summary>
        public void Announce(string text)
        {
            if (_banner == null) return;
            _banner.text = text;
            SetBannerAlpha(1f);
            _hideAt = Time.time + VisibleSeconds;
        }

        /// <summary>Active/désactive le voile d'ambiance (joueur dans la zone sauvage).</summary>
        public void SetInside(bool inside)
        {
            if (_overlay != null) _overlay.enabled = inside;
        }

        private void Update()
        {
            if (_banner == null) return;
            float a = _banner.color.a;
            if (a > 0f && Time.time >= _hideAt)
                SetBannerAlpha(Mathf.MoveTowards(a, 0f, Time.deltaTime / FadeSeconds));
        }

        private void SetBannerAlpha(float a)
        {
            var c = _banner.color; c.a = a; _banner.color = c;
            var outline = _banner.GetComponent<Outline>();
            if (outline != null) outline.effectColor = new Color(0f, 0f, 0f, 0.8f * a);
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45; // sous les prompts/HUD critiques (50+)

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Voile d'ambiance plein écran (rouge sombre, faible alpha), désactivé par défaut.
            var overlayGo = new GameObject("Overlay");
            overlayGo.transform.SetParent(canvasGo.transform, false);
            _overlay = overlayGo.AddComponent<RawImage>();
            _overlay.texture = Texture2D.whiteTexture;
            _overlay.color = new Color(0.4f, 0.05f, 0.05f, 0.12f);
            _overlay.raycastTarget = false;
            var ort = _overlay.rectTransform;
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero;
            ort.offsetMax = Vector2.zero;
            _overlay.enabled = false;

            // Bannière centrée (un peu au-dessus du centre).
            var bannerGo = new GameObject("BannerText");
            bannerGo.transform.SetParent(canvasGo.transform, false);
            var rect = bannerGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 160f);
            rect.sizeDelta = new Vector2(1000f, 80f);

            _banner = bannerGo.AddComponent<Text>();
            _banner.text = string.Empty;
            _banner.alignment = TextAnchor.MiddleCenter;
            _banner.fontSize = 40;
            _banner.fontStyle = FontStyle.Bold;
            _banner.color = new Color(0.95f, 0.85f, 0.8f, 0f); // alpha 0 = caché au départ
            _banner.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _banner.raycastTarget = false;

            var outline = bannerGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0f);
            outline.effectDistance = new Vector2(2f, -2f);
        }
    }
}
