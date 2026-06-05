using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Survain.AI.Npc;
using Survain.Items;

namespace Survain.UI
{
    /// <summary>
    /// Panneau de gestion du village (#14 phase 3), ouvert en interagissant avec le contremaître
    /// (point d'interaction unique). Liste les villageois (nom, faim/abri/moral, productivité) et
    /// permet d'assigner leur métier en jeu via un cycle ◀ Métier ▶.
    ///
    /// Singleton auto-créé (cf. InteractionPrompt), UI construite en code. À l'ouverture : libère
    /// le curseur et fige l'orbite caméra (PlayerCameraRig.RotationLocked) ; restaure à la fermeture.
    /// Échap ou E referme. S'appuie sur NpcController.All + SetJob (déjà en place).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcManagementPanel : MonoBehaviour
    {
        private static NpcManagementPanel _instance;

        public static NpcManagementPanel Instance
        {
            get
            {
                if (_instance == null) CreateInstance();
                return _instance;
            }
        }

        // Métiers assignables via le contremaître (le métier Contremaître n'en fait pas partie).
        private static readonly NpcJob[] Assignable =
            { NpcJob.SansEmploi, NpcJob.Bucheron, NpcJob.Mineur, NpcJob.Constructeur };

        private const float RowHeight = 30f;
        private const float BarWidth = 56f;
        private const float BarHeight = 14f;
        private const float PanelWidth = 600f;
        private const float TopOffset = 56f; // titre + en-têtes de colonnes
        private const float FooterHeight = 74f; // bouton Recruter + ligne de feedback

        // Colonnes (x relatifs au panneau).
        private const float ColName = 14f;
        private const float ColHunger = 140f;
        private const float ColShelter = 206f;
        private const float ColMorale = 272f;
        private const float ColProd = 344f;
        private const float ColPrev = 420f;
        private const float ColJob = 446f;
        private const float ColNext = 560f;

        private sealed class Row
        {
            public NpcController Npc;
            public GameObject Go;
            public RectTransform Hunger, Shelter, Morale;
            public Text Prod, Job;
        }

        private GameObject _root;
        private RectTransform _content;
        private Text _empty;
        private readonly List<Row> _rows = new List<Row>();
        private NpcController _foreman;
        private Transform _player;

        private GameObject _recruitBtnGo;
        private Text _recruitText;
        private Text _feedback;

        private static void CreateInstance()
        {
            var go = new GameObject("_NpcManagementPanel");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<NpcManagementPanel>();
            _instance.BuildUI();
        }

        /// <summary>Ouvre le panneau (reconstruit le roster), ou le ferme s'il est déjà ouvert.
        /// <paramref name="foreman"/> fait face à <paramref name="player"/> pendant l'échange.</summary>
        public void Toggle(NpcController foreman, Transform player)
        {
            if (_root.activeSelf) { Hide(); return; }
            _foreman = foreman;
            _player = player;
            if (_feedback != null) _feedback.text = string.Empty;
            Rebuild();
            SetOpen(true);
        }

        public void Hide() => SetOpen(false);

        private void SetOpen(bool open)
        {
            bool was = _root.activeSelf;
            _root.SetActive(open);

            // Curseur + gel caméra délégués au mode UI centralisé (cf. UiMode), sur transition.
            // Le contremaître fait face au joueur pendant l'échange (illusion de dialogue).
            if (open && !was)
            {
                UiMode.Push();
                if (_foreman != null) _foreman.BeginTalk(_player);
            }
            else if (!open && was)
            {
                UiMode.Pop();
                if (_foreman != null) _foreman.EndTalk();
                _foreman = null;
            }
        }

        private void Update()
        {
            if (!_root.activeSelf) return;

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { Hide(); return; }

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                if (row.Npc == null) { Rebuild(); return; } // un villageois a disparu (désertion)

                var n = row.Npc.Needs;
                if (n != null)
                {
                    SetFill(row.Hunger, n.Hunger);
                    SetFill(row.Shelter, n.Shelter);
                    SetFill(row.Morale, n.Morale);
                    row.Prod.text = $"×{n.WorkSpeedMultiplier:0.00}";
                }
                row.Job.text = JobLabel(row.Npc.Job);
            }
        }

        // ─── Construction du roster ──────────────────────────────────────────

        private void Rebuild()
        {
            for (int i = 0; i < _rows.Count; i++)
                if (_rows[i].Go != null) Destroy(_rows[i].Go);
            _rows.Clear();

            int index = 0;
            var all = NpcController.All;
            for (int i = 0; i < all.Count; i++)
            {
                var npc = all[i];
                if (npc == null || npc.Job == NpcJob.Contremaitre) continue; // le contremaître ne se gère pas lui-même
                CreateRow(npc, index++);
            }

            _empty.gameObject.SetActive(index == 0);

            // Rafraîchit le coût affiché et repositionne le footer (bouton + feedback) sous les lignes.
            if (_recruitText != null) _recruitText.text = $"Recruter ({CostLabel()})";
            float rowsBottom = TopOffset + Mathf.Max(1, index) * RowHeight;
            PositionFooter(rowsBottom);

            // Ajuste la hauteur du panneau au nombre de villageois + footer.
            float h = rowsBottom + FooterHeight;
            _root.GetComponent<RectTransform>().sizeDelta = new Vector2(PanelWidth, h);
        }

        private void PositionFooter(float rowsBottom)
        {
            if (_recruitBtnGo != null)
                _recruitBtnGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(ColName, -(rowsBottom + 8f));
            if (_feedback != null)
                _feedback.rectTransform.anchoredPosition = new Vector2(ColName, -(rowsBottom + 44f));
        }

        // ─── Recrutement (#15) ───────────────────────────────────────────────

        private void DoRecruit()
        {
            var sp = NpcSpawner.Instance;
            if (sp == null) { SetFeedback("Recrutement indisponible."); return; }

            Vector3 from = _foreman != null ? _foreman.transform.position : Vector3.zero;
            var outcome = sp.TryRecruit(from);
            if (outcome == NpcSpawner.RecruitOutcome.Success) Rebuild(); // affiche la recrue
            SetFeedback(FeedbackFor(outcome));
        }

        private void SetFeedback(string text)
        {
            if (_feedback != null) _feedback.text = text;
        }

        private static string FeedbackFor(NpcSpawner.RecruitOutcome outcome)
        {
            switch (outcome)
            {
                case NpcSpawner.RecruitOutcome.Success:            return "Nouveau villageois recruté !";
                case NpcSpawner.RecruitOutcome.VillageFull:        return "Village plein.";
                case NpcSpawner.RecruitOutcome.NotEnoughResources: return "Ressources insuffisantes dans le coffre.";
                case NpcSpawner.RecruitOutcome.NoStorage:          return "Aucun coffre près du contremaître.";
                default:                                           return "Recrutement impossible.";
            }
        }

        private static string CostLabel()
        {
            var sp = NpcSpawner.Instance;
            if (sp == null || sp.RecruitCost == null || sp.RecruitCost.Count == 0) return "gratuit";

            var cost = sp.RecruitCost;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < cost.Count; i++)
            {
                if (cost[i].Item == null) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(cost[i].Amount).Append(' ').Append(cost[i].Item.DisplayName);
            }
            return sb.Length > 0 ? sb.ToString() : "gratuit";
        }

        private void CreateRow(NpcController npc, int index)
        {
            var go = new GameObject($"Row_{npc.name}");
            go.transform.SetParent(_content, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(TopOffset + index * RowHeight));
            rt.sizeDelta = new Vector2(PanelWidth, RowHeight);

            var row = new Row { Npc = npc, Go = go };

            string name = npc.Data != null ? npc.Data.DisplayName : npc.name;
            Label(go.transform, name, ColName, 120f, TextAnchor.MiddleLeft, 16);

            row.Hunger = Bar(go.transform, ColHunger, new Color(1f, 0.65f, 0.1f));
            row.Shelter = Bar(go.transform, ColShelter, new Color(0.3f, 0.7f, 1f));
            row.Morale = Bar(go.transform, ColMorale, new Color(0.4f, 0.85f, 0.4f));

            row.Prod = Label(go.transform, "×1.00", ColProd, 64f, TextAnchor.MiddleLeft, 15);

            var npcRef = npc; // capture
            ArrowButton(go.transform, "◀", ColPrev, () => CycleJob(npcRef, -1));
            row.Job = Label(go.transform, JobLabel(npc.Job), ColJob, 110f, TextAnchor.MiddleCenter, 15);
            ArrowButton(go.transform, "▶", ColNext, () => CycleJob(npcRef, +1));

            _rows.Add(row);
        }

        private static void CycleJob(NpcController npc, int dir)
        {
            int i = System.Array.IndexOf(Assignable, npc.Job);
            if (i < 0) i = 0;
            int next = (i + dir + Assignable.Length) % Assignable.Length;
            npc.SetJob(Assignable[next]);
        }

        // ─── Construction UI ─────────────────────────────────────────────────

        private void BuildUI()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>(); // nécessaire pour cliquer les boutons

            _root = NewRaw(canvasGo.transform, "Panel", new Color(0.08f, 0.08f, 0.1f, 0.92f));
            var prt = _root.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(PanelWidth, 200f);

            _content = _root.GetComponent<RectTransform>();

            Label(_root.transform, "Gestion du village", ColName, PanelWidth - 28f, TextAnchor.UpperLeft, 22, FontStyle.Bold)
                .rectTransform.anchoredPosition = new Vector2(ColName, -10f);

            // En-têtes de colonnes.
            Header("Faim", ColHunger);
            Header("Abri", ColShelter);
            Header("Moral", ColMorale);
            Header("Prod", ColProd);
            Header("Métier", ColJob);

            _empty = Label(_root.transform, "Aucun villageois à gérer.", ColName, PanelWidth - 28f, TextAnchor.MiddleLeft, 15);
            _empty.rectTransform.anchoredPosition = new Vector2(ColName, -TopOffset - 4f);
            _empty.gameObject.SetActive(false);

            // Footer : bouton « Recruter » + ligne de feedback (repositionnés dans Rebuild).
            _recruitBtnGo = NewRaw(_root.transform, "RecruitBtn", new Color(0.22f, 0.42f, 0.26f, 1f));
            var brt = _recruitBtnGo.GetComponent<RectTransform>();
            brt.sizeDelta = new Vector2(260f, 28f);
            var btnImg = _recruitBtnGo.GetComponent<RawImage>();
            btnImg.raycastTarget = true;
            var btn = _recruitBtnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(DoRecruit);

            _recruitText = Label(_recruitBtnGo.transform, "Recruter", 0f, 260f, TextAnchor.MiddleCenter, 15, FontStyle.Bold);
            var trt = _recruitText.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            _feedback = Label(_root.transform, string.Empty, ColName, PanelWidth - 28f, TextAnchor.MiddleLeft, 14);
            _feedback.color = new Color(0.9f, 0.85f, 0.55f);

            _root.SetActive(false);
        }

        private void Header(string text, float x)
        {
            var t = Label(_root.transform, text, x, 70f, TextAnchor.LowerLeft, 13);
            t.color = new Color(0.7f, 0.7f, 0.75f);
            t.rectTransform.anchoredPosition = new Vector2(x, -34f);
        }

        // ─── Primitives UI ───────────────────────────────────────────────────

        private RectTransform Bar(Transform parent, float x, Color fillColor)
        {
            var bg = NewRaw(parent, "Bg", new Color(0f, 0f, 0f, 0.5f));
            Place(bg.GetComponent<RectTransform>(), new Vector2(x, -(RowHeight - BarHeight) * 0.5f), new Vector2(BarWidth, BarHeight));

            var fill = NewRaw(parent, "Fill", fillColor);
            var frt = fill.GetComponent<RectTransform>();
            Place(frt, new Vector2(x, -(RowHeight - BarHeight) * 0.5f), new Vector2(BarWidth, BarHeight));
            return frt;
        }

        private static void SetFill(RectTransform fill, float v) =>
            fill.sizeDelta = new Vector2(Mathf.Clamp01(v) * BarWidth, BarHeight);

        private void ArrowButton(Transform parent, string glyph, float x, UnityEngine.Events.UnityAction onClick)
        {
            var t = Label(parent, glyph, x, 22f, TextAnchor.MiddleCenter, 20, FontStyle.Bold);
            Place(t.rectTransform, new Vector2(x, -(RowHeight - 24f) * 0.5f), new Vector2(22f, 24f));
            t.raycastTarget = true;
            var btn = t.gameObject.AddComponent<Button>();
            btn.targetGraphic = t;
            btn.onClick.AddListener(onClick);
        }

        private Text Label(Transform parent, string content, float x, float w, TextAnchor anchor, int size, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("T");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            Place(rt, new Vector2(x, -(RowHeight - 20f) * 0.5f), new Vector2(w, 20f));
            return t;
        }

        private static GameObject NewRaw(Transform parent, string name, Color color)
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

        private static void Place(RectTransform rt, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static string JobLabel(NpcJob job)
        {
            switch (job)
            {
                case NpcJob.SansEmploi: return "Sans emploi";
                case NpcJob.Bucheron: return "Bûcheron";
                case NpcJob.Mineur: return "Mineur";
                case NpcJob.Constructeur: return "Constructeur";
                case NpcJob.Contremaitre: return "Contremaître";
                default: return job.ToString();
            }
        }
    }
}
