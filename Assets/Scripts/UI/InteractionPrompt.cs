using UnityEngine;
using UnityEngine.UI;

namespace Survain.UI
{
    /// <summary>
    /// Affiche un prompt d'interaction centré-bas dans le viewport ("[E] Récolter Arbre").
    /// Singleton auto-créé au premier accès : pas de setup côté scène. Le Canvas
    /// screen-space overlay et le Text sont construits par code au Awake.
    ///
    /// Consommateurs : PlayerHarvester appelle Show(text) / Hide() selon le résultat
    /// du raycast hover. Le composant est passif (pas d'Update interne).
    ///
    /// Choix uGUI Text plutôt que TextMeshPro pour éviter la dépendance à TMP Essentials
    /// (pas garanti importé dans le projet). À migrer vers TMP au Sprint UI quand
    /// on touchera à l'esthétique des HUD.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPrompt : MonoBehaviour
    {
        private static InteractionPrompt _instance;

        /// <summary>
        /// Accesseur singleton. Crée l'instance au premier appel (lazy init).
        /// </summary>
        public static InteractionPrompt Instance
        {
            get
            {
                if (_instance == null) CreateInstance();
                return _instance;
            }
        }

        private Text _label;
        private GameObject _labelGo;

        private static void CreateInstance()
        {
            var go = new GameObject("_InteractionPrompt");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<InteractionPrompt>();
            _instance.BuildUI();
        }

        private void BuildUI()
        {
            // Canvas screen-space overlay au-dessus du HUD futur.
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // au-dessus du jeu, sous une UI critique éventuelle

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Label centré-bas (40% de la hauteur depuis le bas).
            _labelGo = new GameObject("PromptText");
            _labelGo.transform.SetParent(canvasGo.transform, false);

            var rect = _labelGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 200f); // 200px au-dessus du bas
            rect.sizeDelta = new Vector2(800f, 60f);

            _label = _labelGo.AddComponent<Text>();
            _label.text = string.Empty;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.fontSize = 28;
            _label.color = new Color(1f, 1f, 1f, 0.95f);
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Petit outline pour la lisibilité sur fonds clairs/sombres.
            var outline = _labelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            _labelGo.SetActive(false);
        }

        /// <summary>
        /// Affiche le prompt avec le texte donné.
        /// </summary>
        public void Show(string text)
        {
            if (_label == null) return;
            _label.text = text;
            if (!_labelGo.activeSelf) _labelGo.SetActive(true);
        }

        /// <summary>
        /// Masque le prompt.
        /// </summary>
        public void Hide()
        {
            if (_labelGo == null) return;
            if (_labelGo.activeSelf) _labelGo.SetActive(false);
        }
    }
}
