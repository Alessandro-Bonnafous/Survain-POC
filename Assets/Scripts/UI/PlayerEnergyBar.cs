using UnityEngine;
using UnityEngine.UI;
using Survain.Gameplay.Player;

namespace Survain.UI
{
    /// <summary>
    /// Barre d'énergie du joueur (combat #16, Phase A / A1), juste sous la barre de vie. Singleton
    /// auto-créé via [RuntimeInitializeOnLoadMethod] : aucun setup côté scène. Calquée trait pour
    /// trait sur <see cref="PlayerHealthBar"/> — construit son Canvas + ses Image par code et se
    /// cale sur <see cref="PlayerEnergy.Instance"/> par polling chaque LateUpdate (pas d'abonnement
    /// à gérer ; robuste si le joueur (re)spawn). Masquée tant qu'aucun PlayerEnergy n'existe.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEnergyBar : MonoBehaviour
    {
        private static PlayerEnergyBar _instance;

        private static readonly Color BorderColor = new Color(0.05f, 0.05f, 0.05f, 1f);
        private static readonly Color FillFull = new Color(0.96f, 0.82f, 0.22f, 1f); // or/jaune énergie
        private static readonly Color FillLow = new Color(0.62f, 0.46f, 0.12f, 1f);  // ambre sombre

        private const float BarWidth = 360f;
        private const float BarHeight = 22f;
        private const float Margin = 28f;
        private const float Border = 3f;
        // Décalage vertical : sous la barre de vie (hauteur 34 + marge 28 + petit espace).
        private const float TopOffset = 70f;

        private RectTransform _fill;
        private Image _fillImage;
        private Text _label;
        private GameObject _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("_PlayerEnergyBar");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PlayerEnergyBar>();
            _instance.BuildUI();
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45; // même strate que la barre de vie
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // root : conteneur positionnel uniquement (RectTransform nu, transparent).
            _root = new GameObject("EnergyBar");
            _root.transform.SetParent(canvasGo.transform, false);
            var rootRt = _root.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0f, 1f);
            rootRt.anchoredPosition = new Vector2(Margin, -TopOffset);
            rootRt.sizeDelta = new Vector2(BarWidth, BarHeight);

            // Liseré/fond : Image ENFANT opaque qui remplit le root (structure de rendu prouvée).
            var border = new GameObject("Border");
            border.transform.SetParent(_root.transform, false);
            var borderImg = border.AddComponent<Image>();
            borderImg.color = BorderColor;
            borderImg.raycastTarget = false;
            Stretch(borderImg.rectTransform, 0f);

            // Remplissage : Image ENFANT, largeur pilotée par anchorMax.x dans LateUpdate.
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_root.transform, false);
            _fillImage = fillGo.AddComponent<Image>();
            _fillImage.color = FillFull;
            _fillImage.raycastTarget = false;
            _fill = _fillImage.rectTransform;
            _fill.anchorMin = new Vector2(0f, 0f);
            _fill.anchorMax = new Vector2(1f, 1f);
            _fill.offsetMin = new Vector2(Border, Border);
            _fill.offsetMax = new Vector2(-Border, -Border);

            // Libellé "EN X/Y" centré.
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_root.transform, false);
            _label = labelGo.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 14;
            _label.fontStyle = FontStyle.Bold;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.raycastTarget = false;
            Stretch(_label.rectTransform, 0f);
            var outline = labelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1f, -1f);

            _root.SetActive(false);
        }

        /// <summary>Étire un RectTransform pour remplir son parent, avec une marge uniforme.</summary>
        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private void LateUpdate()
        {
            var energy = PlayerEnergy.Instance;
            if (energy == null)
            {
                if (_root.activeSelf) _root.SetActive(false);
                return;
            }

            if (!_root.activeSelf) _root.SetActive(true);

            float ratio = Mathf.Clamp01(energy.Normalized);
            _fill.anchorMax = new Vector2(ratio, 1f);
            _fillImage.color = Color.Lerp(FillLow, FillFull, ratio);
            _label.text = $"EN {energy.CurrentEnergy:0}/{energy.MaxEnergy:0}";
        }
    }
}
