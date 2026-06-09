using UnityEngine;
using UnityEngine.UI;

namespace Survain.UI
{
    /// <summary>
    /// Écran de mort (#19) : voile plein écran sombre + « VOUS ÊTES MORT » + décompte avant
    /// réapparition. Singleton auto-créé au premier accès (cf. InteractionPrompt) : aucun setup
    /// scène. Piloté par PlayerDeath (Show → SetCountdown chaque frame → Hide au respawn).
    ///
    /// Le voile est un Image plein écran avec raycastTarget = true : il bloque les clics UI
    /// pendant la mort. Les actions monde (récolte/frappe) sont neutralisées en parallèle via
    /// UiMode (PlayerDeath fait Push/Pop) ; le déplacement est gelé en désactivant PlayerController.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeathScreen : MonoBehaviour
    {
        private static DeathScreen _instance;

        public static DeathScreen Instance
        {
            get { if (_instance == null) CreateInstance(); return _instance; }
        }

        private GameObject _root;
        private Text _countdown;

        private static void CreateInstance()
        {
            var go = new GameObject("_DeathScreen");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DeathScreen>();
            _instance.BuildUI();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // au-dessus du prompt (50) et de la barre de vie (45)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Voile plein écran (bloque les clics UI).
            _root = new GameObject("Veil");
            _root.transform.SetParent(canvasGo.transform, false);
            var veil = _root.AddComponent<Image>();
            veil.color = new Color(0.25f, 0f, 0f, 0.65f);
            veil.raycastTarget = true;
            var veilRt = veil.rectTransform;
            veilRt.anchorMin = Vector2.zero;
            veilRt.anchorMax = Vector2.one;
            veilRt.offsetMin = Vector2.zero;
            veilRt.offsetMax = Vector2.zero;

            CreateText("Title", "VOUS ÊTES MORT", 64, new Vector2(0f, 60f));
            _countdown = CreateText("Countdown", string.Empty, 30, new Vector2(0f, -30f));

            _root.SetActive(false);
        }

        private Text CreateText(string name, string content, int fontSize, Vector2 offset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(1200f, 120f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        public void Show()
        {
            if (_root != null && !_root.activeSelf) _root.SetActive(true);
        }

        public void SetCountdown(float secondsRemaining)
        {
            if (_countdown == null) return;
            _countdown.text = $"Réapparition dans {Mathf.CeilToInt(secondsRemaining)}…";
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf) _root.SetActive(false);
        }
    }
}
