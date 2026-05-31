using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Survain.AI.Npc;

namespace Survain.UI
{
    /// <summary>
    /// Panneau de détail d'un PNJ (#13 phase 3) : nom + 3 jauges (Faim / Abri / Moral), mises à
    /// jour en direct. Ouvert/fermé par NpcInteractable (touche E) ; Échap ferme aussi. Se masque
    /// si le PNJ lié disparaît (désertion).
    ///
    /// Singleton auto-créé au premier accès (cf. InteractionPrompt) : UI construite en code, zéro
    /// setup côté scène. Barres = RawImage + Texture2D.whiteTexture (rectangles pleins teintés).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcDetailPanel : MonoBehaviour
    {
        private static NpcDetailPanel _instance;

        public static NpcDetailPanel Instance
        {
            get
            {
                if (_instance == null) CreateInstance();
                return _instance;
            }
        }

        private const float BarWidth = 150f;
        private const float BarHeight = 16f;

        private GameObject _root;
        private Text _nameLabel;
        private RectTransform _hungerFill, _shelterFill, _moraleFill;
        private NpcController _bound;

        private static void CreateInstance()
        {
            var go = new GameObject("_NpcDetailPanel");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<NpcDetailPanel>();
            _instance.BuildUI();
        }

        /// <summary>Ouvre le panneau sur ce PNJ, ou le ferme s'il est déjà affiché pour lui.</summary>
        public void Toggle(NpcController npc)
        {
            if (_root.activeSelf && _bound == npc) { Hide(); return; }
            _bound = npc;
            if (_bound != null && _bound.Data != null) _nameLabel.text = _bound.Data.DisplayName;
            _root.SetActive(true);
        }

        public void Hide()
        {
            _bound = null;
            _root.SetActive(false);
        }

        private void Update()
        {
            if (!_root.activeSelf) return;

            if (_bound == null || _bound.Needs == null) { Hide(); return; } // PNJ disparu

            var n = _bound.Needs;
            SetFill(_hungerFill, n.Hunger);
            SetFill(_shelterFill, n.Shelter);
            SetFill(_moraleFill, n.Morale);

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Hide();
        }

        private static void SetFill(RectTransform fill, float value01)
        {
            fill.sizeDelta = new Vector2(Mathf.Clamp01(value01) * BarWidth, BarHeight);
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55; // au-dessus des bulles (40) et du prompt d'interaction (50)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Panneau ancré en haut à gauche.
            _root = NewRawImage(canvasGo.transform, "Panel", new Color(0.08f, 0.08f, 0.1f, 0.85f));
            var prt = _root.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(0f, 1f);
            prt.anchoredPosition = new Vector2(20f, -20f);
            prt.sizeDelta = new Vector2(240f, 150f);

            _nameLabel = NewText(_root.transform, "Name", "PNJ", 24, FontStyle.Bold, TextAnchor.UpperLeft);
            Place(_nameLabel.rectTransform, new Vector2(12f, -10f), new Vector2(216f, 30f));

            _hungerFill = NewGauge(_root.transform, "Faim", new Color(1f, 0.65f, 0.1f), -48f);
            _shelterFill = NewGauge(_root.transform, "Abri", new Color(0.3f, 0.7f, 1f), -84f);
            _moraleFill = NewGauge(_root.transform, "Moral", new Color(0.4f, 0.85f, 0.4f), -120f);

            _root.SetActive(false);
        }

        /// <summary>Crée une ligne de jauge (libellé + fond + remplissage) et retourne le RectTransform du remplissage.</summary>
        private RectTransform NewGauge(Transform parent, string label, Color fillColor, float y)
        {
            var caption = NewText(parent, $"Lbl_{label}", label, 16, FontStyle.Normal, TextAnchor.MiddleLeft);
            Place(caption.rectTransform, new Vector2(12f, y), new Vector2(60f, BarHeight + 8f));

            var bg = NewRawImage(parent, $"Bg_{label}", new Color(0f, 0f, 0f, 0.5f));
            Place(bg.GetComponent<RectTransform>(), new Vector2(74f, y - 4f), new Vector2(BarWidth, BarHeight));

            var fill = NewRawImage(parent, $"Fill_{label}", fillColor);
            var frt = fill.GetComponent<RectTransform>();
            // Ancrage gauche pour que la largeur représente la valeur.
            frt.anchorMin = frt.anchorMax = new Vector2(0f, 1f);
            frt.pivot = new Vector2(0f, 1f);
            frt.anchoredPosition = new Vector2(74f, y - 4f);
            frt.sizeDelta = new Vector2(BarWidth, BarHeight);
            return frt;
        }

        private static GameObject NewRawImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<RawImage>();
            img.texture = Texture2D.whiteTexture;
            img.color = color;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return go;
        }

        private static Text NewText(Transform parent, string name, string content, int size, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            return text;
        }

        private static void Place(RectTransform rt, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }
    }
}
