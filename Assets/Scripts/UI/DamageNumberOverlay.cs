using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Survain.Gameplay.Combat;

namespace Survain.UI
{
    /// <summary>
    /// Bulles de dégâts flottantes (combat #16, Phase B) : à chaque coup typé porté à un ennemi,
    /// affiche <b>deux nombres séparés et colorés</b> — la part de biome (couleur du biome) et la part
    /// physique (gris) — qui montent à l'écran et s'estompent. Rend lisible la décomposition typée du
    /// modèle B4 (#84) pour le PO sur un build taggé, là où seule la Console la montrait jusqu'ici.
    ///
    /// Même pattern que <see cref="NpcStatusOverlay"/> / <see cref="InteractionPrompt"/> : un Canvas
    /// screen-space singleton auto-créé (<see cref="RuntimeInitializeOnLoadMethod"/>), libellés positionnés
    /// via <c>WorldToScreenPoint</c>. <b>Aucun setup côté scène.</b>
    ///
    /// Les bulles sont <b>fire-and-forget</b> : leur position est capturée en coordonnées <b>monde</b> à
    /// l'instant du coup, donc elles continuent de s'animer même après la mort/destruction de l'ennemi
    /// (même logique que le burst de particules détaché de ResourceNodeJuice). Pool interne réutilisé →
    /// pas d'instanciation par coup.
    ///
    /// Couleurs/tailles/durée = placeholders ajustables (équilibrage #88).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageNumberOverlay : MonoBehaviour
    {
        private static DamageNumberOverlay _instance;

        // ─── Tuning (placeholders #88) ──────────────────────────────────────
        private const float LifetimeSeconds = 1.0f;   // durée de vie d'une bulle
        private const float RiseScreenPixels = 70f;    // montée totale à l'écran sur la durée de vie
        private const float HeadHeight = 2.0f;         // hauteur d'apparition au-dessus du pivot ennemi
        private const float JitterPixels = 28f;        // étalement horizontal anti-chevauchement
        private const int FontSize = 26;

        private static readonly Color PhysicalColor = new Color(0.85f, 0.85f, 0.85f);

        /// <summary>Couleur d'une part de dégât selon son type (roster combat PO).</summary>
        private static Color ColorFor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Foret: return new Color(0.30f, 0.85f, 0.30f); // vert
                case DamageType.Plaines: return new Color(0.90f, 0.80f, 0.25f); // doré
                case DamageType.Montagnes: return new Color(0.40f, 0.60f, 1.00f); // bleu froid
                case DamageType.CoteMaritime: return new Color(0.90f, 0.20f, 0.20f); // rouge
                default: return PhysicalColor;
            }
        }

        private sealed class Bubble
        {
            public Text Text;
            public CanvasGroup Group;
            public Vector3 World;
            public float JitterX;
            public float BornAt;
            public bool Active;
        }

        private Canvas _canvas;
        private Camera _camera;
        private readonly List<Bubble> _bubbles = new List<Bubble>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("_DamageNumberOverlay");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DamageNumberOverlay>();
            _instance.BuildCanvas();
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 45; // entre NpcStatusOverlay (40) et InteractionPrompt (50)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        /// <summary>Affiche une bulle de dégâts typée au-dessus de <paramref name="worldPos"/>.
        /// No-op si l'overlay n'est pas encore initialisé. Les parts arrondies à 0 sont omises.</summary>
        public static void Show(Vector3 worldPos, DamageInfo hit)
            => _instance?.Spawn(worldPos + Vector3.up * HeadHeight, hit);

        private void Spawn(Vector3 world, DamageInfo hit)
        {
            int biome = Mathf.RoundToInt(hit.BiomeAmount);
            int physical = Mathf.RoundToInt(hit.PhysicalAmount);
            if (biome <= 0 && physical <= 0) return;

            // Deux nombres colorés dans une même bulle (rich text) → couleurs distinctes par type.
            string biomeHex = ColorUtility.ToHtmlStringRGB(ColorFor(hit.BiomeType));
            string physHex = ColorUtility.ToHtmlStringRGB(PhysicalColor);
            string text;
            if (biome > 0 && physical > 0)
                text = $"<color=#{biomeHex}>{biome}</color>  <color=#{physHex}>{physical}</color>";
            else if (biome > 0)
                text = $"<color=#{biomeHex}>{biome}</color>";
            else
                text = $"<color=#{physHex}>{physical}</color>";

            var b = GetFreeBubble();
            b.Text.text = text;
            b.World = world;
            b.JitterX = Random.Range(-JitterPixels, JitterPixels);
            b.BornAt = Time.time;
            b.Active = true;
            b.Group.alpha = 1f;
            if (!b.Text.gameObject.activeSelf) b.Text.gameObject.SetActive(true);
        }

        private Bubble GetFreeBubble()
        {
            for (int i = 0; i < _bubbles.Count; i++)
                if (!_bubbles[i].Active) return _bubbles[i];
            return CreateBubble();
        }

        private Bubble CreateBubble()
        {
            var go = new GameObject("DamageBubble");
            go.transform.SetParent(_canvas.transform, false);
            var group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var text = go.AddComponent<Text>();
            text.supportRichText = true;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = FontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var rt = text.rectTransform;
            rt.sizeDelta = new Vector2(200f, 36f);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); // ancrage bas-gauche → position = pixels écran

            var b = new Bubble { Text = text, Group = group };
            _bubbles.Add(b);
            return b;
        }

        private void LateUpdate()
        {
            if (_camera == null) _camera = Camera.main; // mis en cache, rafraîchi si la caméra change
            if (_camera == null) return;

            float now = Time.time;
            for (int i = 0; i < _bubbles.Count; i++)
            {
                var b = _bubbles[i];
                if (!b.Active) continue;

                float t = (now - b.BornAt) / LifetimeSeconds;
                if (t >= 1f) { Recycle(b); continue; }

                Vector3 screen = _camera.WorldToScreenPoint(b.World);
                if (screen.z <= 0f) { Recycle(b); continue; } // ennemi derrière la caméra

                screen.x += b.JitterX;
                screen.y += t * RiseScreenPixels; // montée
                b.Text.rectTransform.position = screen;
                b.Group.alpha = 1f - t; // fade
            }
        }

        private void Recycle(Bubble b)
        {
            b.Active = false;
            if (b.Text.gameObject.activeSelf) b.Text.gameObject.SetActive(false);
        }
    }
}
