using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Survain.AI.Npc;

namespace Survain.UI
{
    /// <summary>
    /// Affiche une bulle d'alerte (texte) au-dessus des PNJ dont un besoin est critique
    /// (faim basse / moral bas) — #13 phase 3. Un seul Canvas screen-space partagé : les
    /// libellés suivent la position des PNJ projetée à l'écran (WorldToScreenPoint), comme
    /// l'a tranché l'arbitrage (overlay plutôt qu'un Canvas world-space par PNJ).
    ///
    /// Singleton auto-créé au premier accès (cf. InteractionPrompt) : aucun setup côté scène.
    /// S'auto-instancie via [RuntimeInitializeOnLoadMethod] pour tourner sans qu'un autre
    /// système n'ait à l'appeler. Itère NpcController.All chaque LateUpdate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcStatusOverlay : MonoBehaviour
    {
        private static NpcStatusOverlay _instance;

        private static readonly Color HungerColor = new Color(1f, 0.65f, 0.1f);
        private static readonly Color MoraleColor = new Color(0.95f, 0.25f, 0.25f);
        private const float HeadHeight = 2.2f; // hauteur du libellé au-dessus du pivot du PNJ

        private Canvas _canvas;
        private Camera _camera;
        private readonly Dictionary<NpcController, Text> _labels = new Dictionary<NpcController, Text>();
        private readonly List<NpcController> _toRemove = new List<NpcController>();
        private readonly HashSet<NpcController> _present = new HashSet<NpcController>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("_NpcStatusOverlay");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<NpcStatusOverlay>();
            _instance.BuildCanvas();
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 40; // sous le prompt d'interaction (50)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void LateUpdate()
        {
            if (_camera == null) _camera = Camera.main; // mis en cache, rafraîchi si la caméra change
            if (_camera == null) return;

            var all = NpcController.All;
            _present.Clear();

            // Met à jour / crée les libellés des PNJ avec un besoin critique.
            for (int i = 0; i < all.Count; i++)
            {
                var ctrl = all[i];
                if (ctrl == null) continue;
                _present.Add(ctrl);
                var needs = ctrl.Needs;

                bool moraleLow = needs != null && needs.IsMoraleLow;
                bool hungry = needs != null && needs.IsHungry;
                if (needs == null || (!moraleLow && !hungry))
                {
                    HideLabel(ctrl);
                    continue;
                }

                Vector3 world = ctrl.transform.position + Vector3.up * HeadHeight;
                Vector3 screen = _camera.WorldToScreenPoint(world);
                if (screen.z <= 0f) // PNJ derrière la caméra
                {
                    HideLabel(ctrl);
                    continue;
                }

                Text label = GetOrCreateLabel(ctrl);
                // Moral bas est plus grave que la faim → prioritaire dans l'affichage.
                if (moraleLow) { label.text = "Moral bas"; label.color = MoraleColor; }
                else { label.text = "Faim"; label.color = HungerColor; }
                label.rectTransform.position = screen;
                if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
            }

            // Nettoie les libellés des PNJ disparus (despawn / désertion).
            _toRemove.Clear();
            foreach (var kvp in _labels)
            {
                if (kvp.Key == null || !_present.Contains(kvp.Key))
                {
                    if (kvp.Value != null) Destroy(kvp.Value.gameObject);
                    _toRemove.Add(kvp.Key);
                }
            }
            for (int i = 0; i < _toRemove.Count; i++) _labels.Remove(_toRemove[i]);
        }

        private Text GetOrCreateLabel(NpcController ctrl)
        {
            if (_labels.TryGetValue(ctrl, out var existing) && existing != null) return existing;

            var go = new GameObject($"Bubble_{ctrl.name}");
            go.transform.SetParent(_canvas.transform, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var rt = text.rectTransform;
            rt.sizeDelta = new Vector2(200f, 30f);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); // ancrage bas-gauche → position = pixels écran

            _labels[ctrl] = text;
            return text;
        }

        private void HideLabel(NpcController ctrl)
        {
            if (_labels.TryGetValue(ctrl, out var label) && label != null && label.gameObject.activeSelf)
                label.gameObject.SetActive(false);
        }
    }
}
