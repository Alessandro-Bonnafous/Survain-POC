using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Survain.Gameplay.Player;

namespace Survain.UI
{
    /// <summary>
    /// Marqueur d'écran des tombes (#19) : pour chaque <see cref="Grave"/> active, affiche un repère
    /// projeté à l'écran avec la distance, clampé aux bords quand la tombe est hors champ (style
    /// marqueur de quête) — rend la tombe « facile à retrouver », en complément du faisceau monde.
    ///
    /// Singleton auto-créé (cf. NpcStatusOverlay) : aucun setup scène. Itère Grave.All en LateUpdate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GraveMarker : MonoBehaviour
    {
        private static GraveMarker _instance;

        private const float WorldHeight = 1.5f;  // point visé au-dessus de la tombe
        private const float EdgeMargin = 60f;     // marge de clamp aux bords (px de référence)
        private static readonly Color MarkerColor = new Color(0.7f, 0.88f, 1f);

        private Canvas _canvas;
        private Camera _camera;
        private readonly Dictionary<Grave, Text> _labels = new Dictionary<Grave, Text>();
        private readonly List<Grave> _toRemove = new List<Grave>();
        private readonly HashSet<Grave> _present = new HashSet<Grave>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;
            var go = new GameObject("_GraveMarker");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<GraveMarker>();
            _instance.BuildCanvas();
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 44; // sous la barre de vie (45) et le prompt (50)
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        private void LateUpdate()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            var graves = Grave.All;
            _present.Clear();

            Vector3 playerPos = PlayerController.Instance != null
                ? PlayerController.Instance.transform.position
                : _camera.transform.position;

            for (int i = 0; i < graves.Count; i++)
            {
                var grave = graves[i];
                if (grave == null) continue;
                _present.Add(grave);

                Vector3 world = grave.transform.position + Vector3.up * WorldHeight;
                Vector3 sp = _camera.WorldToScreenPoint(world);

                Text label = GetOrCreateLabel(grave);
                float dist = Vector3.Distance(playerPos, grave.transform.position);
                label.text = $"☠ {Mathf.RoundToInt(dist)} m";

                label.rectTransform.position = ClampToScreen(sp);
                if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
            }

            // Nettoie les marqueurs des tombes disparues (loot récupéré / timer).
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

        /// <summary>Projette en coordonnées écran ; si la cible est hors champ ou derrière la
        /// caméra, clampe la position au bord de l'écran dans la direction de la cible.</summary>
        private static Vector3 ClampToScreen(Vector3 screenPoint)
        {
            float w = Screen.width;
            float h = Screen.height;
            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);

            bool behind = screenPoint.z < 0f;
            Vector2 p = new Vector2(screenPoint.x, screenPoint.y);
            if (behind) p = center - (p - center); // miroir : la cible derrière pointe vers l'arrière

            bool onScreen = !behind && p.x >= 0f && p.x <= w && p.y >= 0f && p.y <= h;
            if (onScreen) return new Vector3(p.x, p.y, 0f);

            Vector2 dir = p - center;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
            dir.Normalize();

            float halfW = center.x - EdgeMargin;
            float halfH = center.y - EdgeMargin;
            float absX = Mathf.Abs(dir.x);
            float absY = Mathf.Abs(dir.y);
            float scaleX = absX > 0.0001f ? halfW / absX : float.MaxValue;
            float scaleY = absY > 0.0001f ? halfH / absY : float.MaxValue;
            float scale = Mathf.Min(scaleX, scaleY);

            Vector2 edge = center + dir * scale;
            return new Vector3(edge.x, edge.y, 0f);
        }

        private Text GetOrCreateLabel(Grave grave)
        {
            if (_labels.TryGetValue(grave, out var existing) && existing != null) return existing;

            var go = new GameObject("GraveMarker");
            go.transform.SetParent(_canvas.transform, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = MarkerColor;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = text.rectTransform;
            rt.sizeDelta = new Vector2(160f, 32f);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); // position = pixels écran

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            _labels[grave] = text;
            return text;
        }
    }
}
