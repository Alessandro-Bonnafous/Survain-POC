# 🧠 CLAUDE.md — Mémoire persistante du projet SURVAIN

> **Ce fichier est la mémoire persistante du projet pour les sessions Claude.**
> Chaque nouvelle conversation avec Claude démarre sans mémoire. Ce document est conçu pour être **lu au début** de chaque session de dev et **mis à jour à la fin** de chaque session qui introduit une décision ou une implémentation importante.

---

## 📜 Règles d'utilisation (pour Claude)

**Au début de chaque session :**
1. Lire intégralement ce fichier avant de répondre à la première demande technique.
2. Consulter également [`docs/Vision_du_Jeu.md`](docs/Vision_du_Jeu.md) si le contexte produit est nécessaire.
3. Ne pas redemander à l'utilisateur des informations déjà documentées ici.

**Pendant la session :**
1. Respecter les conventions de code, de commit et d'architecture décrites plus bas.
2. Si une décision technique ou produit est prise, l'ajouter dans le [Journal des décisions](#-journal-des-décisions-append-only).
3. Si une convention manque ou doit évoluer, proposer explicitement la mise à jour à l'utilisateur.

**À la fin de la session (si pertinent) :**
1. Proposer à l'utilisateur une mise à jour de ce fichier couvrant :
   - Les nouvelles décisions prises (journal)
   - Les changements d'état (sprint, scope, jalons)
   - Les nouveaux glossaires/termes introduits
2. Ne jamais modifier ce fichier sans accord explicite de l'utilisateur — proposer le diff, laisser l'humain valider.
3. Les modifications de ce fichier font l'objet d'un commit `docs(claude): …` dédié.

**Ce qu'on ne met PAS ici :**
- Des secrets (clés API, tokens, credentials)
- Des détails d'implémentation éphémères (mieux dans le code ou les commentaires)
- Des résumés de conversation (mieux dans les issues GitHub)

---

## 🧭 Identité du projet

**SURVAIN** est un jeu PC mêlant survie, city-builder, politique féodale et mythologie nordique. Le joueur démarre seul dans une contrée sauvage, bâtit un village avec ses PNJ, puis s'intègre progressivement dans une hiérarchie territoriale menant jusqu'au royaume entier.

**Progression territoriale** : Contrée (solo) → Province (6 contrées, coop) → Région (10 provinces, PvP) → Royaume (10 régions, open world).

**Piliers de design** (non négociables sans discussion) :
- Craft non-répétitif, transmission par livres de recettes
- Stuff exclusivement craftable, jamais lootable
- Combat anti-zerg à effectifs fixes (PJ + PNJ)
- Économie fermée : pas d'hôtel des ventes, marché central à la Capitale uniquement
- Système divin inspiré du panthéon nordique (offrandes, bénédictions, reliques)
- Mort = perte de stuff + tombe

**Scope du POC** : boucle solo en Contrée uniquement. Tout le multi/PvP est hors-scope tant que la boucle solo ne fonctionne pas.

---

## 👥 Équipe

| Rôle | Personne | Responsabilités |
|---|---|---|
| Product Owner | Pascal | Vision, priorisation, validation |
| Développeur | Alessandro | Implémentation, architecture |
| Pair-programmer IA | Claude (Anthropic) | Support dev, architecture, docs |

---

## 🛠️ Stack technique

| Couche | Choix | Notes |
|---|---|---|
| Moteur | Unity 6 LTS | URP (Universal Render Pipeline) |
| Langage | C# | .NET Standard 2.1 |
| Rendu visé | 3D low-poly / stylisé | Compromis qualité/rapidité pour POC |
| Input | Unity Input System | Nouveau système (pas l'ancien) |
| IDE dev | Visual Studio 2022 Community | Workload « Game development with Unity » |
| OS cible POC | Windows x64 | Autres plateformes hors scope |
| VCS | Git + Git LFS | Indispensable vu les assets 3D |
| Hébergement repo | GitHub | [Alessandro-Bonnafous/Survain-POC](https://github.com/Alessandro-Bonnafous/Survain-POC) |
| CI/CD | _non défini_ | À discuter à partir du Sprint 2 |
| Gestion tâches | GitHub Issues + Milestones | 6 sprints définis (S0 à S5) |

---

## 📊 État actuel

**Sprint en cours :** Sprint 1 — Récolte & Craft

**Objectifs du sprint** : boucle minimale de survie — récolter dans le monde, gérer un inventaire, transformer la matière par un craft engageant non-répétitif.
- [x] Système d'items et ScriptableObjects (issue #5)
- [ ] Nœuds de ressources et système de récolte (issue #6)
- [ ] Inventaire joueur (issue #7)
- [ ] Système de craft basique tier gris (issue #8) — ⚠️ **point de design critique** : la mécanique d'engagement non-répétitive (QTE/timing/autre) doit être validée avec Pascal AVANT implémentation. Différenciateur clé du jeu.

**Livrable du sprint :** build où le joueur peut couper un arbre / miner une roche, voir les ressources dans son inventaire, et crafter un outil de base via une mécanique non-répétitive.

**Sprint précédent — Sprint 0 — Fondations (clôturé le 2026-05-16) :**
- [x] Setup projet Unity (issue #1)
- [x] Architecture dossiers et scripts fondateurs (issue #1)
- [x] Génération procédurale de terrain — 1 biome forêt tempérée (issue #2)
- [x] Contrôleur joueur 3D, 3e personne (issue #3)
- [x] Système de caméra 3e personne — zoom, smoothing, lock rotation (issue #4)
- [x] Cycle jour/nuit basique (issue #28)

**Dernière décision en date :** _voir le journal ci-dessous._

**Prochain milestone :** Sprint 2 (à définir — probablement Combat & Construction d'après la vision).

---

## ⏳ Décisions en attente (à arbitrer)

> Petites décisions remontées du chat / Discord qui n'ont pas encore été tranchées. À traiter avant la phase qui en dépend pour ne pas bloquer.

- **Équilibrage arme « Montagnes » : 8 dmg vs 6 dmg** — à arbitrer par Pascal **avant la première table d'armes craftables** (Sprint 1 ne touche qu'aux outils — bois, pierre, fibre, hache/pioche en pierre — donc plus bloquant pour Sprint 1). Probable horizon : Sprint 2+. Pas de code à toucher tant que la décision n'est pas prise.
- **Mécanique de craft non-répétitive (issue #8)** — à arbitrer avec Pascal **avant implémentation de #8**. Proposition de l'issue : QTE/timing simple pour le tier gris, qualité du résultat dépendante de la performance joueur. Choix structurant pour tout le système de craft (les tiers vert/bleu hériteront du pattern). Pas de code Sprint 1 sur le craft tant que pas tranché.

---

## 📝 Conventions de code (C# / Unity)

### Nommage

| Élément | Convention | Exemple |
|---|---|---|
| Classes, structs, enums | PascalCase | `PlayerController`, `BiomeType` |
| Méthodes | PascalCase | `TryInteract()` |
| Propriétés publiques | PascalCase | `CurrentHealth` |
| Champs privés | `_camelCase` | `_stamina`, `_rigidbody` |
| Champs `[SerializeField]` privés | `_camelCase` | `[SerializeField] private float _speed;` |
| Constantes | PascalCase | `MaxInventorySlots` |
| Paramètres et variables locales | camelCase | `targetPosition` |
| Interfaces | Préfixe `I` + PascalCase | `IInteractable` |
| Fichiers | Un type public par fichier, nom = nom du type | `PlayerController.cs` |

### Organisation

- **Namespaces** sous `Survain.<Domain>` : `Survain.Player`, `Survain.World.Generation`, `Survain.AI.Npc`, etc.
- Un `Assets/Scripts/` racine, puis sous-dossiers par domaine (miroir des namespaces).
- Les `ScriptableObject` de config vont dans `Assets/ScriptableObjects/` et sont créables via `[CreateAssetMenu]`.
- Les prefabs dans `Assets/Prefabs/<Catégorie>/`.

### Style

- Pas de `using System.Linq` dans les chemins de hot-path (Update, FixedUpdate) — alloue trop.
- Préférer `TryGet…` aux exceptions pour les cas d'échec attendus.
- Éviter `FindObjectOfType` en runtime ; injecter les dépendances via inspector ou via un locator.
- Commentaires XML `///` sur les API publiques non triviales.
- Log via un wrapper `SurvainLog` (à créer) plutôt que `Debug.Log` direct, pour pouvoir filtrer par catégorie.

### Tests

_À définir au Sprint 1._ Probablement Unity Test Framework avec tests EditMode pour la logique pure.

---

## 🔀 Conventions Git

### Branches

- `main` : toujours buildable, protégée
- `feat/<court-nom>` : nouvelle feature
- `fix/<court-nom>` : correction de bug
- `chore/<court-nom>` : technique (refacto, CI, docs)

### Commits — Conventional Commits

Format : `<type>(<scope>): <description en français>`

```
feat(player): ajout du déplacement WASD
fix(inventory): corriger crash quand slot vide
docs(claude): ajouter décision sur le système de stamina
chore(git): configurer LFS pour les FBX
```

### Pull Requests

- Une PR = une issue (ou un groupe cohérent)
- Titre = titre de l'issue ou commit principal
- Corps mentionne `Closes #<numéro>`
- Merge en **Squash and merge**

---

## 🏗️ Décisions architecturales

Cette section liste les choix structurants qui conditionnent le reste du code. Les changements nécessitent discussion explicite.

- **URP** (pas HDRP, pas Built-in) : compromis performance/qualité, tooling stable pour low-poly.
- **New Input System** : plus flexible pour les bindings futurs (manette, rebind joueur).
- **ScriptableObjects pour la config** : recettes, biomes, métiers de PNJ, stats d'armes sont des data assets éditables sans code.
- **Singleton pattern pour le `GameManager`** : uniquement pour le `GameManager` global (états du jeu). Le reste évite les singletons.
- **Pas de DOTS/ECS pour le POC** : MonoBehaviour classique. DOTS uniquement si un bottleneck perf nous y force.

---

## 🧠 Journal des décisions (append-only)

> **Format** : `YYYY-MM-DD — <titre court>` puis contexte, décision, alternatives considérées, conséquences.
> **Ordre** : antéchronologique (plus récent en haut).

### 2026-05-17 — Système d'items et ScriptableObjects (Sprint 1, issue #5)

**Contexte.** Issue #5 du Sprint 1, fondation indispensable pour #6 (récolte), #7 (inventaire) et #8 (craft). Besoin d'un modèle de données SO qui couvre les 6 rôles métier d'item (Resource, Tool, Weapon, Armor, Building, Consumable), les 3 tiers de craft (Basic/Wild/Superior), les nœuds de récolte, et qui supporte une sauvegarde future par identifiant stable.

**Décisions.**
1. **Hiérarchie de SO `ItemData` (abstract) + sous-classes concrètes**, PAS de SO monolithique avec enum `ItemType` qui pilote des champs conditionnels. Cohérent avec le précédent `PlayerMovementConfig` + `PlayerCameraConfig` (2026-04-26) : un SO = un domaine cohérent. Chaque sous-classe (`ResourceData`, `ToolData`, `WeaponData`, `ArmorData`, `BuildingData`, `ConsumableData`) porte ses propres champs ; l'enum `ItemType` reste exposé sur la base pour filtrage rapide sans cast (UI, requêtes).
2. **Sous-classes Weapon/Armor/Building/Consumable créées en squelette dès maintenant** (pas seulement Resource/Tool minimum viable). Évite les renames d'asset/menu plus tard ; enrichissement champ par champ au fil des sprints. Coût marginal : ~80 lignes pour les 4 squelettes.
3. **Identifiants stables : champ `string Id` édité à la main en kebab-case** (`stone-axe`, `raw-wood`), PAS GUID Unity ni nom d'asset. Lisible dans les saves, stable au rename d'asset. Format et unicité validés à l'édition par `ItemRegistry.OnValidate` (Regex kebab-case + HashSet de doublons + warn/error via `SurvainLog.Category.System`).
4. **`ItemRegistry` SO global introduit dans cette PR** (pas reporté). Référencé depuis `GameSettings.ItemRegistry` (même pattern que `defaultBiome`). Justification : critère d'acceptation « items sérialisables pour la sauvegarde future » implique une résolution `Id` → `ItemData` au load. Aurait nécessité un patch GameSettings de toute façon en #7. API : `GetItemById(string)`, `GetResourceNodeById(string)`, `AllItems`, `AllResourceNodes`. Peuplé manuellement (drag&drop) ou via le menu bootstrap.
5. **`ItemTier` reste un enum simple** (`Basic`/`Wild`/`Superior`), PAS un SO. Précédent `BiomeType` (2026-04-18). Si un `TierVisualsConfig` (couleur UI, multiplicateurs) devient nécessaire plus tard, on l'introduira en complément sans casser l'enum.
6. **`ResourceNodeData` SO data + futur `ResourceNode` MonoBehaviour runtime** (issue #6). Le SO décrit le type de nœud (item produit, qty, temps, outil requis) ; le MonoBehaviour portera la logique de placement et hit-detection. Split cohérent avec `BiomeConfig` ↔ `TerrainGenerator`. Suffixe `Data` retenu pour distinguer SO data ↔ runtime component.
7. **`ItemsBootstrap` Editor menu pour matérialiser les 6 premiers items + 4 nœuds + Registry**, idempotent (re-lance sans risque). **Les `.asset` générés ET leurs `.meta` SONT versionnés** (rectification du 2026-05-17 par rapport à mon premier réflexe initial qui invoquait le pattern `TerrainGenerator`). Raison : `GameSettings.asset._itemRegistry` référence `Registry.asset` **par GUID** (stocké dans le `.meta`). Si on ne versionne pas le `.meta`, chaque dev qui re-lance le Bootstrap génère un GUID différent → `GameSettings.asset` pointe sur missing reference au clone. Idem en chaîne pour les `ResourceNodeData` qui référencent les `ResourceData` par GUID. Le pattern `TerrainGenerator` (assets non versionnés) ne tient que parce que rien ne référence le mesh généré — il est pur runtime. Pour tout SO susceptible d'être référencé par un autre asset, on versionne le couple `.asset` + `.meta`. Le Bootstrap reste donc un outil de **setup initial** dont on commit le résultat, pas un régénérateur permanent.
8. **Icônes (champ `Sprite _icon`) laissées nullable et vides au Sprint 1.** Pas d'asset graphique créé. L'UI inventaire (#7) gérera le null par un fallback couleur/texte le moment venu.

**Alternatives écartées.**
- **SO monolithique** + enum `ItemType` qui pilote l'affichage : SO bavard, pas de type-safety, custom editors requis pour cacher les champs non pertinents (overhead). Rejeté.
- **Composition par modules `[SerializeReference]`** (Stackable, Tool, Weapon en composants) : flexible mais Inspector Unity médiocre, complexité disproportionnée pour le POC. Rejeté.
- **GUID Unity comme identifiant** : ultra-stable mais opaque, illisible dans les saves, complique le debug. Rejeté.
- **Pas de registry, scan `Resources.Load` au load** : marche techniquement mais oblige à un placement strict dans `Resources/`, casse au moindre déplacement d'asset. Rejeté.
- **Asset menu `Survain/Data/Items/...`** : `Items` étant un nouveau domaine de premier rang (pas une config), il a son propre groupe top-level dans le menu Create : `Survain/Items/...`. Cohérent avec la structure de namespaces.

**Conséquences.**
- Pattern « base abstract SO + sous-classes concrètes + Id string stable + Registry global » devient le template pour les futurs domaines de contenu (recettes de craft #8, métiers de PNJ, dialogues...).
- `Survain.Items` est le 4e namespace de premier rang du projet (avec `Survain.Core`, `Survain.Data`, `Survain.Gameplay`). Les futurs assets de contenu auront leur propre namespace top-level si le domaine est cohérent (recettes → `Survain.Crafting` probable).
- `GameSettings.asset` reçoit un nouveau champ `_itemRegistry`. Pas de `FormerlySerializedAs` (nouvel ajout, pas un rename) ; valeur par défaut `null` à brancher dans l'Inspector après checkout.
- Pattern de validation `OnValidate` avec `SurvainLog.Warn/Error` pour les SO de registre / config introduit. À répliquer pour les futurs SO de catalogue (recettes, recettes de PNJ, etc.).
- L'arborescence `Assets/ScriptableObjects/Items/{Resources,Tools,ResourceNodes}` + `Registry.asset` est créée par le menu Bootstrap au premier lancement Unity post-checkout **et committée**. Les forks/clones ultérieurs ont les assets directement sans avoir à relancer le Bootstrap.
- Le bootstrap stocke les valeurs initiales d'équilibrage (temps de récolte 2–6s, quantités 1–4, durabilité 80) en dur dans le code — c'est volontaire pour le POC. Quand Pascal voudra équilibrer, on tunera directement dans les `.asset` via l'Inspector ; le code de bootstrap n'est qu'un point de départ idempotent. **Important** : les valeurs en dur dans le code et les valeurs sérialisées dans les `.asset` peuvent diverger après tuning — le code reste la valeur d'origine, c'est `.asset` qui fait foi au runtime.
- **Règle générale dégagée** : pour les SO data référencés par d'autres assets (config globale, registry, contenu), on versionne le couple `.asset` + `.meta`. Pour les artefacts purement runtime régénérés à chaque session (mesh terrain), on ne versionne ni l'un ni l'autre. Le critère est : « est-ce qu'un autre asset committé en dépend par GUID ? »

---

### 2026-05-16 — Cycle jour/nuit basique (Sprint 0, issue #28)

**Contexte.** Issue #28 et dernière brique fonctionnelle du Sprint 0. Avec terrain, joueur et caméra en place, il manquait l'ambiance temporelle qui transforme une scène statique en monde "vivant". Périmètre POC strict : une seule Directional Light qui tourne, intensité et couleur variables, ambient piloté. Skybox dynamique, étoiles, lune et audio hors scope.

**Décisions.**
1. **MonoBehaviour `DayNightCycle` + SO `TimeOfDayConfig`, PAS de logique dans le SO.** Cohérent avec `TerrainGenerator` ↔ `TerrainGenerationSettings`. Le SO porte la durée du cycle, les `Gradient` (sun + ambient), l'`AnimationCurve` d'intensité et l'heure de départ. Le composant lit ces données et applique l'état chaque frame.
2. **Heure normalisée `[0..1]` plutôt qu'une simulation `0h..24h`.** Couplage direct aux courbes/gradients du SO (qui sont indexés sur `[0..1]` nativement par Unity), zéro conversion, déterministe. Une horloge `HH:MM` simulée serait pertinente quand on aura un calendrier (saisons, événements datés) — Sprint 3+ au plus tôt.
3. **`RenderSettings.ambientMode = Flat` forcé au `Start`.** Le mode par défaut "Skybox" ignore `RenderSettings.ambientLight`, donc notre gradient ne s'appliquerait pas. On bascule en Flat pour que le SO ait l'autorité. Trade-off accepté : on perd l'ambient calculé depuis la skybox URP. À reconsidérer si on intègre un jour une skybox dynamique (Sprint 2+).
4. **Phases `DayPhase { Night, Dawn, Day, Dusk }` avec bornes hardcodées** (`0.0/0.2/0.3/0.7/0.8`). Volontairement simple pour Sprint 0. Si on veut tuner les bornes par biome ou par contexte, on évoluera vers un SO de phases — pas avant que le besoin se présente.
5. **`Update` (pas `FixedUpdate`), pas de singleton.** Le cycle est visuel donc lié au rendu, pas à la physique. `FindObjectOfType` interdit par convention — les futurs consommateurs (AI, audio, gameplay) s'abonnent à `OnPhaseChanged` ou polleront `CurrentPhase`/`CurrentTime01` via une référence inspector.
6. **GameObject `_TimeOfDay` dédié, PAS le composant directement sur la Directional Light.** Découple "qui pilote le cycle" de "ce qui est piloté". Cohérent avec `_GameManager`, `_WorldRoot`, `_Player` (préfixe `_` pour remonter en tête de hiérarchie).

**Alternatives écartées.**
- **Horloge simulée `HH:MM` avec multiplicateur de temps** : conversion supplémentaire vers `[0..1]` pour évaluer les courbes, sans bénéfice tant qu'on n'a pas de calendrier.
- **Skybox dynamique (Procedural Skybox + variable `_SunDirection`, ou Volume URP avec interpolation)** : excellent visuellement mais demande un shader custom ou un gros volume animé, disproportionné pour Sprint 0.
- **Bornes de phase dans le SO** : configurable mais sans use-case Sprint 0 ; ajoute 4 champs au SO pour rien.
- **`FixedUpdate`** : pas pour du visuel — risque de stutter à high refresh rate.
- **Composant directement sur la Directional Light** : couple le pilote au piloté, complique le test (impossible d'avoir 2 lights pilotables sans rétro-engineering).

**Conséquences.**
- Le `_TimeOfDay` rejoint le set des GameObjects préfixés `_` à la racine de `Main.unity`. Tout système qui consulte l'heure passe par sa référence (pas de singleton).
- `RenderSettings.ambientMode` est désormais piloté par code au runtime — la valeur sérialisée dans la scène (en mode Skybox typiquement) est écrasée dès le `Start`. Effet de bord à connaître : si on désactive `DayNightCycle` au runtime, l'ambient reste figé sur la dernière valeur appliquée jusqu'à `RenderSettings.ambientMode = Skybox` manuel.
- Les défauts du SO (gradients, courbe) sont calibrés pour un visuel cohérent avec le terrain low-poly. Tuning à la main si feel différent voulu.
- Pattern « SO de courbes/gradients indexées sur `[0..1]` + composant qui les évalue chaque frame » devient le template pour les autres systèmes d'ambiance dynamique (météo, season cycle si Sprint 3+).
- **Sprint 0 clôturé fonctionnellement** dès que cette issue est mergée. La liste des objectifs du sprint est entièrement cochée.

---

### 2026-05-16 — Polish caméra : zoom, smoothing, lock rotation (Sprint 0, issue #4)

**Contexte.** Issue #4 du Sprint 0 — polish du `PlayerCameraRig` posé en #3. Trois items à ajouter par-dessus l'existant : zoom à la molette, smoothing du mouvement caméra, lock de rotation (préparation Sprint 2 mode construction). Le SphereCast anti-clipping et l'orbitale souris étaient déjà en place et ont été conservés.

**Décisions.**
1. **`Mathf.SmoothDampAngle` pour yaw, `Mathf.SmoothDamp` pour pitch, PAS `Lerp`.** `SmoothDamp` produit un easing critique (pas d'overshoot) qui est l'attendu standard pour une caméra suiveuse. Sur le yaw, la version `Angle` gère le wrap 360° (passer de 359° à 1° prend le chemin court). Sur le pitch, la valeur est déjà clampée en `[-89..89]`, pas de wrap à gérer → `SmoothDamp` standard suffit. Champ unique `RotationSmoothTime` partagé entre les deux, `0` = comportement direct ancien. Valeur par défaut 0.08s.
2. **Zoom event-driven via `_zoomAction.performed`, PAS polling dans LateUpdate.** Le scroll wheel produit des ticks discrets ; en polling on lit `0` la plupart des frames et la valeur du tick uniquement le frame où l'événement arrive — risque de perdre des ticks si la frame est sautée. L'abonnement à `performed` est appelé une fois par tick par Input System lui-même. Cohérent avec le pattern Jump du `PlayerController`.
3. **`RotationLocked` = simple propriété publique `bool { get; set; }`, PAS event ni méthode.** Le futur mode construction (Sprint 2) doit pouvoir lock/unlock à volonté ; une propriété est l'API la plus directe. Pas de mémorisation de l'angle pré-lock (au unlock, on reprend là où on est). Pas de binding qui l'active au Sprint 0, juste l'API exposée.

**Alternatives écartées.**
- **`Lerp` framerate-dépendant** (`Lerp(a, b, smoothFactor)` sans `Time.deltaTime`) : flicker selon le framerate, mauvaise pratique.
- **`Lerp` framerate-indépendant** (`Lerp(a, b, 1 - Mathf.Pow(decay, Time.deltaTime))`) : marche, mais paramètre `decay` non-intuitif comparé à un `smoothTime` en secondes.
- **Zoom en polling `ReadValue<float>()`** : marche en pratique mais introduit une fragilité framerate non nécessaire vu qu'on a déjà l'event natif.
- **Lock via event `OnLockChanged`** : surcharge pour un cas où le polling de la propriété par les consommateurs suffit.

**Conséquences.**
- Le SO `PlayerCameraConfig.asset` existant reçoit 5 nouveaux champs aux valeurs par défaut. Tuning à ajuster si feel différent voulu.
- L'action `Zoom` (Value/Axis, bind `<Mouse>/scroll/y` + `<Gamepad>/dpad/y`) est maintenant une dépendance de la map "Player". Tout `.inputactions` futur du projet devra l'inclure (ou un équivalent) pour que le rig fonctionne.
- Pattern « target + current + `SmoothDamp` + velocity ref » devient le template pour les autres systèmes de polish caméra (cadrage dynamique, FOV variable, etc.).
- `PlayerCameraRig.RotationLocked` est l'API que le futur système de construction (Sprint 2) appellera pour figer la vue.

---

### 2026-04-26 — Contrôleur joueur 3e personne (Sprint 0, issue #3)

**Contexte.** Issue #3 du Sprint 0. Avec un terrain explorable (#2) et un GameManager (#1) en place, il manquait le contrôle joueur pour valider la boucle minimale « lancer le jeu → se déplacer dans le monde ». Choix techniques structurants à figer parce qu'ils conditionnent le combat (Sprint 3+), les interactions monde (Sprint 1) et la caméra (qui dure tout le POC).

**Décisions.**
1. **`CharacterController`, PAS `Rigidbody`.** Standard Unity pour un perso non-piloté par physics : gestion native pente/marche, slide le long des murs, déplacement déterministe via `Move()`. Un `Rigidbody` serait nécessaire pour subir les forces (knockback, push) — pas notre besoin au Sprint 0. À reconsidérer au Sprint 3 (combat) — possiblement un hybride (CharacterController standard + bascule Rigidbody pendant un knockback).
2. **Caméra orbitale custom, PAS Cinemachine.** Cohérent avec les rejets Shader Graph et Unity Terrain : code-first, diff Git lisible, zéro asset binaire, zéro dépendance package supplémentaire. Le `PlayerCameraRig` fait ~120 lignes de C# pur (yaw/pitch souris, distance fixe, `SphereCast` anti-clipping) — exactement ce dont on a besoin. Cinemachine serait pertinent pour des cinématiques scriptées (cutscenes, target groups) — pas avant le Sprint 4 si jamais.
3. **Input via `InputActionAsset` sérialisé + lookup par nom (`FindActionMap`/`FindAction`), PAS de wrapper C# généré, PAS de `PlayerInput.SendMessages`.** `SendMessages` dispatche par nom de méthode au runtime (fragile au refactor, pas vérifié à la compilation). Le wrapper généré ajoute un fichier à versionner et à régénérer à chaque modif. Le lookup par nom une fois au `Awake` + abonnement à `performed`/`canceled` est aussi rapide et garde le code explicite. **Convention :** un seul composant est propriétaire du cycle `Enable`/`Disable` d'une map (`PlayerController` ici) ; les autres consommateurs (`PlayerCameraRig`) lisent les actions sans toucher à l'activation.
4. **Deux SO de config séparés** (`PlayerMovementConfig` + `PlayerCameraConfig`), PAS un unique `PlayerConfig`. Chaque composant a sa config dédiée — cohérent avec `TerrainGenerationSettings` ↔ `TerrainGenerator`. Permet d'équilibrer locomotion et caméra indépendamment, et de swapper un set complet par contexte de jeu plus tard (caméra exploration vs combat, par ex).
5. **Visuel joueur = primitive `Capsule` avec son `CapsuleCollider` retiré au profit du `CharacterController`.** Le `CharacterController` est lui-même un collider — laisser le `CapsuleCollider` du primitive crée un double-collider qui décolle le joueur du sol. Convention à rappeler quand on remplacera la capsule par un mesh riggé (le mesh aura potentiellement ses propres colliders pour hit-detection, mais le mouvement passera toujours par le `CharacterController` de la racine).
6. **Curseur verrouillé/caché par le `PlayerCameraRig` au `OnEnable`, libéré au `OnDisable`.** Le rig caméra est l'autorité sur le curseur (il consomme la souris). Pour le POC, **Esc** suffit à libérer le curseur dans le Game view (comportement Unity Editor). Au Sprint 1+, un menu pause devra explicitement libérer le curseur.

**Alternatives écartées.**
- **Rigidbody + `ForceMode`** : tuning lourd (drag, friction de matériau, gestion explicite de la pente via raycast), inutile tant qu'on ne se fait pas pousser.
- **Cinemachine** : asset-driven (Virtual Cameras, blends, noise profiles), incompatible avec la préférence code-first du projet, et ajoute une dépendance package à maintenir.
- **Wrapper C# généré pour les InputActions** : fichier généré à régénérer à chaque modif, overhead non justifié pour 3-4 actions sur 1 map.
- **`PlayerInput` component avec `SendMessages`/`BroadcastMessages`** : dispatch par string, non typé, masque le flux d'input dans le boilerplate Unity.
- **`Camera.main` / `FindObjectOfType` pour résoudre les refs runtime** : viole la convention CLAUDE.md (« injecter les dépendances via inspector »).
- **SO unique `PlayerConfig`** : moins propre, gros fichier de tuning, plus difficile à swapper par contexte.

**Conséquences.**
- Le prefab `_Player` (capsule + `CharacterController` + scripts) devient l'unique source de vérité de la kinématique joueur. Tout système qui veut la position joueur passe par ce Transform.
- La `Main Camera` porte désormais le `PlayerCameraRig`. Sa position/rotation sérialisée dans `Main.unity` n'a plus d'importance — le rig prend le contrôle au premier `LateUpdate`. Le commentaire du 2026-04-19 (« position provisoire à réécrire avec un suivi de joueur ») est donc honoré.
- Le pattern « SO de config + composant qui consomme + lookup par nom dans `InputActionAsset` » devient le template pour tout futur système d'input (UI, véhicules, dialogue).
- Arborescence `Assets/ScriptableObjects/Player/` créée — accueillera les futures configs joueur (équipement, stats RPG, etc.).
- Les placeholders (cubes/sphères du terrain) n'ont pas de collider, donc la caméra les traverse. Cohérent avec la décision du 2026-04-19. Quand on introduira la collecte (Sprint 1), les prefabs dédiés auront des colliders et la caméra reculera automatiquement contre eux (`collisionMask` = tous les layers par défaut).
- **Convention `_camelCase` réaffirmée et code aligné.** Le code initial utilisait `[SerializeField] private camelCase` (sans underscore), divergence avec CLAUDE.md qui prescrit `_camelCase`. Décision prise dans la même session : on aligne le code sur le doc. Tous les `[SerializeField]` privés du projet et les champs runtime privés ont été renommés en `_camelCase`. Les SO/MonoBehaviours déjà référencés par des assets/prefabs/scènes (`GameSettings`, `BiomeConfig`, `TerrainGenerationSettings`, `GameManager`, `TerrainGenerator`) ont reçu un `[FormerlySerializedAs("oldName")]` pour que Unity migre les valeurs au prochain load — la migration s'écrit ensuite dans le YAML au prochain save Unity. Convention valide pour tous les futurs scripts.

---

### 2026-04-19 — Génération procédurale du terrain (Sprint 0, issue #2)

**Contexte.** Issue #2 du Sprint 0. Première brique de monde 3D : besoin d'un terrain explorable sur lequel poser les briques suivantes (contrôleur joueur, cycle jour/nuit, récolte). Choix techniques structurants à figer parce qu'ils conditionnent toutes les futures itérations sur le monde (chunks, biomes additionnels, navigation IA).

**Décisions.**
1. **Mesh généré par code, PAS Unity Terrain.** Trois raisons : (a) le style low-poly flat-shaded sort naturellement d'un mesh à vertices dupliqués par triangle, ce qui est tordu à obtenir avec Unity Terrain ; (b) déterminisme strict par seed, sans aucun asset binaire (`.terraindata`) à versionner — le terrain est entièrement reproductible depuis la C# + le SO ; (c) chemin direct vers une découpe en chunks au Sprint 2/3 si le terrain s'étend (juste une boucle de plus). Conséquence : on perd le tooling de painting/sculpting Unity Terrain — assumé, on n'en a pas besoin au POC.
2. **Perlin multi-octaves** (`Mathf.PerlinNoise` empilé avec `persistence`/`lacunarity`) pour le relief. Simple, déterministe, largement documenté, suffisant pour des collines crédibles. Si on a besoin de structures plus complexes (rivières, falaises) plus tard, on basculera sur du noise dérivé (Domain Warping, Voronoi) — pas avant.
3. **Vertex color + shader HLSL custom URP** pour la coloration par altitude, plutôt que Shader Graph. Le `.shader` est du texte (versioning Git propre, diff lisible), Shader Graph est un asset binaire fragile aux mises à jour d'Unity. Le shader est volontairement minimal (diffus + ambient via `SampleSH`) — on ajoutera du `_Color` ou des keywords URP si nécessaire.
4. **Aucune texture pour le POC.** Le gradient d'altitude évalué en `Color` par vertex suffit à donner du caractère au terrain. Pas de `_BaseMap`, pas de tiling, pas de splat map.
5. **Placement des placeholders par raycast** sur le `MeshCollider` du terrain (pas de recalcul du Perlin côté placeholders). Une seule source de vérité pour le sol → le placement reste cohérent même si la formule de hauteur change. Rejet par pente via l'angle de la normale du hit.
6. **Convention forte : les placeholders n'ont PAS de collider.** Au Sprint 0, seul le `MeshCollider` du terrain porte la physique. Les cubes/sphères sont purement visuels. À reconsidérer quand on introduira la collecte (Sprint 1) — probablement un `Collider` sur le prefab d'arbre/rocher dédié, pas sur le placeholder.
7. **Convention forte : la scène `Main.unity` ne contient PAS le mesh généré ni les placeholders.** Ils sont régénérés à chaque Play / appel de `Generate()` (ContextMenu de l'Inspector) à partir du seed. Si tu vois des enfants `Placeholders` ou un mesh sérialisé sous `_WorldRoot` dans la scène committée, c'est un bug — appeler `Clear` via ContextMenu et resauver. Raison : on ne veut pas versionner du mesh binaire dans le `.unity`, et on veut garantir que le seed est l'unique source de vérité.
8. **Seed lu depuis `GameSettings.WorldSeed`**, avec override par champ sérialisé `seedOverride` sur le composant. Si seed = 0 (défaut), un seed aléatoire est tiré et loggué — pratique pour itérer visuellement, et le log permet de retomber sur un terrain qu'on a aimé en le mettant en `seedOverride`.

**Alternatives écartées.**
- **Unity Terrain** : tooling lourd, asset binaire `.terraindata` à versionner (incompatible avec un projet où on veut un diff propre), painting overkill au POC.
- **Shader Graph** : asset binaire JSON-like, casse régulièrement entre versions URP, diff Git illisible. Notre shader fait 30 lignes — Graph serait disproportionné.
- **Mesh à vertices partagés + normal smoothing** : nécessite soit un shader qui compute la normale via dérivées (`ddx`/`ddy`, fragile, non supporté partout) soit un post-traitement CPU pour casser les normales. Beaucoup plus simple de dupliquer les vertices à la génération.
- **Perlin GPU (compute shader)** : prématuré pour un terrain 100m × 80 subdivs (génération <100ms attendue sur CPU), et complique le déterminisme cross-plateforme.
- **Poisson disk pour la distribution des placeholders** : meilleure qualité visuelle mais coût implémentation disproportionné. Le rejet aléatoire avec contrainte de pente suffit visuellement au Sprint 0.

**Conséquences.**
- Tout système qui voudra interagir avec le sol (placement, navigation, raycasts gameplay) doit passer par le `MeshCollider` du `_WorldRoot` — pas par un recalcul Perlin parallèle. Une seule source de vérité.
- Le shader `Survain/TerrainVertexColor` est désormais une dépendance versionnée du projet ; toute évolution (ajout d'AO, brouillard, fog distance) y passera, pas dans un Shader Graph.
- Le pattern « SO de paramètres + composant qui consomme + ContextMenu Generate/Clear » devient le template pour les futurs systèmes de génération (ex: décor, cluster d'arbres par biome).
- L'arborescence `Assets/Art/Materials/Shaders/` est créée et accueillera les futurs shaders custom du projet. Convention : un shader = un fichier `.shader`, pas de Graph.
- `_WorldRoot` est un GameObject vide à la racine de `Main.unity` qui porte le `TerrainGenerator` et tous ses composants requis (`MeshFilter`, `MeshRenderer`, `MeshCollider`). Le préfixe `_` (cohérent avec `_GameManager`) le fait remonter en tête de hiérarchie.
- Caméra Main repositionnée provisoirement à `(0, 35, -55)` rotation `(25°, 0, 0)` pour qu'on voie quelque chose au démarrage. **Provisoire** — l'issue caméra du Sprint 0 (à venir) réécrira ça proprement avec un suivi de joueur.

---

### 2026-04-18 — ScriptableObjects : GameSettings + BiomeConfig (clôture issue #1)

**Contexte.** Issue #1 (Sprint 0), étape 4 et dernière. Les fondations (arborescence, logging, GameManager) étant posées, il fallait introduire la convention de données : comment le projet stocke et expose la configuration sans mélanger données et logique.

**Décisions.**
1. **ScriptableObjects = conteneurs de données purs, zéro logique métier.** Aucune méthode de gameplay dans un SO. Les systèmes (génération de terrain, craft, etc.) lisent les données ; les SO ne calculent rien. Convention à signaler en revue de code si un SO commence à porter de la logique.
2. **`CreateAssetMenu` rangé sous `Survain/Data/...`** : évite la pollution du menu Create d'Unity. Tout nouveau SO du projet suit ce pattern (`menuName = "Survain/<domaine>/<Nom>"`).
3. **`GameSettings` : un seul asset par projet (singleton conceptuel).** Pas d'enforcement code au POC (pas de chargement automatique via `Resources` ou Addressables), juste une convention stricte. Asset unique : `Assets/ScriptableObjects/Settings/GameSettings.asset`.
4. **Biome de référence POC : forêt tempérée.** Asset `Assets/ScriptableObjects/Biomes/ForetTemperee.asset`, biome actif dans `GameSettings.defaultBiome`. C'est le seul biome implémenté jusqu'au Sprint 1 au minimum.
5. **`BiomeType` enum dans `BiomeConfig`** (pas un SO séparé) : le type enum suffit pour les règles de gameplay au POC. Un SO dédié pour le type serait de l'over-engineering.

**Alternatives écartées.**
- Logique de fallback ou de chargement dans les SO : rompt la séparation données/logique, rend les tests plus complexes.
- Menu Create à la racine (sans sous-menu `Survain/`) : pollue le menu Create d'Unity avec nos types, difficile à retrouver dans un projet qui grandit.
- Singleton enforcé par code (`Resources.Load` + vérification au démarrage) : overkill pour le POC, à considérer au Sprint 2 si le besoin de chargement fiable se présente.

**Conséquences.**
- Clôture de l'issue #1 — Sprint 0 architecture de base entièrement posée.
- Tout nouveau SO du projet hérite de ces conventions (namespace `Survain.Data`, menu `Survain/...`, accesseurs read-only, zéro logique).
- Les systèmes futurs (génération terrain, craft) référenceront `GameSettings` via l'Inspector ou un locator, jamais via `new`.

---

### 2026-04-18 — GameManager : singleton persistant + state machine

**Contexte.** Issue #1 (Sprint 0), étape 3. Les fondations de logging et d'arborescence étant posées, il fallait introduire le premier composant architectural majeur : un gestionnaire d'état global qui persiste entre les scènes et sert de point d'entrée à la simulation.

**Décisions.**
1. **State machine enum + switch** (pas de classes `State` polymorphes) : 3 états (`Menu`, `Playing`, `Paused`) ne justifient pas la complexité d'un pattern State complet. Un `switch` est lisible, sans allocation, et extensible à 5–6 états sans refacto. Si le nombre d'états dépasse 6 ou que les états portent leur propre logique, revisiter vers un pattern State dédié.
2. **Double notification : `event Action<GameState, GameState>` C# statique + `UnityEvent<GameState, GameState>` sérialisé** : les deux sont déclenchés à chaque transition réussie avec la signature `(previousState, newState)`. L'event C# est la voie principale pour le code (zéro friction, pas d'allocation de delegate supplémentaire) ; le `UnityEvent` permet de brancher des listeners depuis l'inspecteur Unity sans écrire une ligne de code (utile pour l'UI ou les feedbacks audio au POC). Choisir l'un ou l'autre selon le contexte de l'appelant.
3. **Auto-création du singleton interdite** : si une scène démarre sans `GameManager` instancié, c'est une erreur de setup — le code logge en `SurvainLog.Error` mais ne crée pas de `GameManager` à la volée. Le `GameManager` doit toujours être présent via le prefab `_GameManager` dans la scène de bootstrap. Raison : l'auto-création masque les oublis de setup et peut produire un état incohérent si le prefab a des sérialisations non-defaults.
4. **`Time.timeScale` géré uniquement ici** : seul le `GameManager` écrit `Time.timeScale` pour la gestion de la pause (`0f` en `Paused`, `1f` sinon). Toute autre manipulation du timeScale (slow-mo, cinématiques) devra passer par une API du `GameManager` ou être coordonnée explicitement pour éviter les conflits. Convention à rappeler en revue de code.

**Alternatives écartées.**
- Pattern State polymorphe (classe abstraite `BaseState` + sous-classes) : over-engineering pour 3 états au stade POC. À considérer si les états deviennent complexes (entrée/sortie, sous-états).
- Event C# seul (pas de `UnityEvent`) : oblige à écrire du code C# pour tout listener, y compris les réactions UI simples — moins pratique pour le prototypage.
- `UnityEvent` seul (pas d'event C# statique) : les `UnityEvent` nécessitent une référence à l'objet sérialisé, incompatibles avec des systèmes purement code (tests, logique non-MonoBehaviour).
- `ScriptableObject` d'état partagé (pattern SO Event) : pertinent pour la multi-scène complexe, disproportionné pour le POC.

**Conséquences.**
- Tous les systèmes qui réagissent à l'état global (UI, IA, audio) doivent s'abonner à `GameManager.OnStateChanged` (code) ou au `UnityEvent StateChanged` (inspector).
- La scène `Main.unity` contient désormais une instance du prefab `Assets/Prefabs/_GameManager.prefab`.
- Le préfixe `_` du nom du GameObject est une convention pour le faire remonter en tête de hiérarchie dans l'éditeur Unity.
- Transitions autorisées : `Menu → Playing`, `Playing ⇄ Paused`, `Playing → Menu`, `Paused → Menu`. Toute autre transition est refusée avec un `SurvainLog.Error`.

---

### 2026-04-18 — Arborescence `Assets/` et wrapper de logging `SurvainLog`

**Contexte.** Issue #1 (Sprint 0). Le projet Unity était initialisé mais `Assets/` ne contenait que les dossiers par défaut (`Scenes`, `Settings`) et `ThirdParty`. Aucune structure de code ni convention de logging n'était posée, ce qui bloquait le démarrage des tâches suivantes du sprint (génération de terrain, contrôleur joueur).

**Décisions.**
1. **Arborescence `Assets/` posée** alignée avec les namespaces `Survain.*` :
   - `Assets/Scripts/{Core,Gameplay,UI,Data,Editor}` — un sous-dossier par domaine, miroir des namespaces.
   - `Assets/ScriptableObjects/{Settings,Biomes}` — assets de configuration éditables hors code.
   - `Assets/Prefabs/` et `Assets/Art/{Models,Textures,Materials,Audio}` — contenu visuel du projet.
   - `Scripts/Editor/` isolé pour que Unity le compile dans une assembly séparée (accès aux APIs `UnityEditor`).
2. **`.gitkeep` dans chaque dossier vide** pour que Git suive l'arborescence. Les `.meta` associés seront générés par Unity à la prochaine ouverture de l'éditeur et commités dans un second temps.
3. **`SurvainLog` comme unique point de logging** du projet (`Assets/Scripts/Core/SurvainLog.cs`) :
   - 8 catégories (`System`, `Gameplay`, `UI`, `World`, `AI`, `Save`, `Audio`, `Network`) avec couleurs Rich Text distinctes pour lisibilité en Console Unity.
   - `Info`/`Warn` décorés `[Conditional("UNITY_EDITOR")]` + `[Conditional("DEVELOPMENT_BUILD")]` + `[Conditional("SURVAIN_LOGS_ENABLED")]` → strippés à la compilation en build release (aucun coût runtime).
   - `Error` toujours actif, même en release, pour capturer les erreurs critiques en prod.
   - API statique (pas de singleton) : zéro friction à l'usage, pas d'initialisation.

**Convention actée (non négociable).** **Jamais** de `UnityEngine.Debug.Log`/`LogWarning`/`LogError` direct dans le code du projet. Tout log passe par `SurvainLog.Info/Warn/Error` avec une catégorie explicite. À signaler en revue de code si un `Debug.Log` apparaît.

**Alternatives écartées.**
- Bibliothèque externe type Serilog/ZLogger : overkill pour le POC, dépendance supplémentaire, tooling Unity moins intégré que `Debug.Log`.
- Singleton `MonoBehaviour` de logging : inutile puisque pas d'état à maintenir, et ajoute un objet à gérer dans chaque scène.
- Enum de catégorie avec attribut `[Description]` pour le nom lisible : redondant, `ToString()` de l'enum suffit.

**Conséquences.**
- Toute nouvelle feature peut maintenant ranger son code dans un domaine clair (`Survain.Gameplay.*` va dans `Scripts/Gameplay/`, etc.).
- Logs filtrables par catégorie dans la Console Unity via le champ de recherche (`[Gameplay]`, `[AI]`, …).
- Pour activer les `Info`/`Warn` dans un build release, ajouter le define `SURVAIN_LOGS_ENABLED` dans Project Settings → Player → Scripting Define Symbols.
- Les `.meta` Unity seront générés à la première ouverture de l'éditeur après ce commit et devront être commités à part.

---

### 2026-04-17 — Initialisation du projet Unity et premier asset tiers

**Contexte.** Création du projet Unity dans le repo via Unity Hub (Unity 6 LTS, template 3D URP) et import d'un premier asset tiers (SimpleNaturePack, low-poly nature) pour avoir des éléments visuels prêts à l'emploi pour le prototypage du terrain au Sprint 0.

**Décisions.**
1. **Unity 6 LTS** confirmé comme version de référence — figée dans `ProjectSettings/ProjectVersion.txt`. Tous les devs doivent installer cette version exacte via Unity Hub.
2. **Renderers Mobile retirés** (`Mobile_RPAsset`, `Mobile_Renderer`) : POC PC-only assumé, pas de surcoût technique.
3. **Template tutoriel URP supprimé** : `Readme.asset` et dossier `TutorialInfo/` retirés du commit initial pour partir sur une base propre.
4. **Format `.slnx` ajouté au `.gitignore`** : nouveau format de solution VS, généré par Unity, à ignorer comme les `.sln`.
5. **Assets du Unity Asset Store NON versionnés** : Standard Unity Asset Store EULA interdit la redistribution publique des fichiers sources. Chaque dev doit réimporter les packs depuis « My Assets » du Package Manager avec son propre compte Unity. Conséquence : tous les packs vont dans `Assets/ThirdParty/<NomDuPack>/` et ce dossier entier est gitignoré (`/Assets/ThirdParty/` + `/Assets/ThirdParty.meta`). Premier pack importé : `SimpleNaturePack`.
6. **Convention de rangement `Assets/ThirdParty/`** adoptée : tout asset externe (Asset Store, packs gratuits, addons) va dans ce dossier. Sépare proprement le code/contenu du projet de celui des tiers, et permet un seul pattern gitignore générique au lieu de devoir lister chaque pack.
7. **`.unitypackage.meta` également ignorés** pour éviter les meta orphelins quand Unity génère des metas pour des `.unitypackage` qui ne sont pas suivis.

**Conséquences.**
- Nouvelle section « Assets tiers à réimporter » dans le `README.md` avec procédure pas-à-pas.
- À chaque ajout d'un nouveau pack tiers : (a) ajouter son chemin dans `.gitignore`, (b) ajouter une ligne dans le tableau du `README.md`, (c) acter ici dans le journal.
- Si un dev voit des prefabs roses / refs manquantes en ouvrant le projet → réflexe d'aller dans Package Manager → My Assets pour réimporter.

**Reste à faire.**
- [ ] Récupérer l'URL exacte de la page Asset Store du `SimpleNaturePack` et compléter le tableau du `README.md`.
- [x] ~~Renommer `SampleScene` en `Main.unity`~~ — fait, scène présente dans `Assets/Scenes/Main.unity`.

---

### 2026-04-17 — Licence propriétaire et politique de branche

**Contexte.** Premières décisions de gouvernance à acter avant le commit initial : choix de licence et règles de protection de `main`.

**Décisions.**
1. **Licence : All Rights Reserved** (propriétaire). Le code reste lisible sur GitHub mais aucun usage, copie, modification ou redistribution n'est autorisé sans accord écrit. Choix conservateur qui garde toutes les options ouvertes pour la suite (commercialisation Steam, levée de fonds, ouverture future en MIT/Apache).
2. **Pas de branch protection sur `main`** pendant le POC. Équipe restreinte (2 devs), souplesse privilégiée. À reconsidérer à la mise en place d'une CI/CD ou si l'équipe s'agrandit.

**Alternatives écartées (licence).** MIT/Apache (trop ouverts si volonté de commercialiser) ; GPL/AGPL (copyleft, complique contributions externes et un éventuel deal éditeur).

**Conséquences.**
- Fichier `LICENSE` créé à la racine (texte propriétaire bilingue FR/EN).
- Section Licence du `README.md` mise à jour explicitement.
- Aucune règle à configurer dans GitHub Settings → Branches.
- Toute contribution externe future nécessitera un CLA si la licence évolue.

---

### 2026-04-17 — Création du repo et de la base documentaire

**Contexte.** Environnement de dev (Unity, VS 2022, Git, Git LFS) installé. Issues GitHub créées pour les Sprints 0 à 5. Besoin d'initialiser la base du repo avec documentation avant d'intégrer le projet Unity.

**Décision.** Premier commit composé de : `README.md`, `CLAUDE.md`, `.gitignore` (Unity), `.gitattributes` (LFS + fins de ligne), `docs/INSTALL.md`, `docs/CONTRIBUTING.md`, `docs/Vision_du_Jeu.md`. Le projet Unity sera créé dans un second temps dans ce même dossier via Unity Hub, puis committé séparément.

**Alternatives.** Créer le projet Unity en premier puis ajouter la doc par-dessus. Rejetée : le `.gitignore` doit exister avant que Unity génère `Library/` et `Temp/` pour éviter de polluer le premier commit.

**Conséquences.**
- Il faut avoir Git LFS installé avant tout clone futur.
- Le second commit (initialisation Unity) devra utiliser Unity 6 LTS + template URP 3D.

---

## 📖 Glossaire

| Terme | Définition |
|---|---|
| Contrée | Unité de territoire de base, jouable en solo. 6 Contrées = 1 Province. |
| Province | 6 Contrées sous un Baron. Première couche multi. |
| Région | 10 Provinces sous un Seigneur (joueur). PvP organisé. |
| Royaume | 10 Régions sous le Roi (GM). Open world. |
| PNJ | Personnage Non-Joueur : recruté, gère un métier, a des besoins et un moral. |
| Hall divin | Lieu sacré d'un panthéon (ex: Midgard, Jötunheim) ; octroie bonus. |
| Stuff | Équipement du joueur (armes, armures). Craftable uniquement. |
| POC | Proof of Concept — première version jouable démontrant les piliers. |

---

## 🔗 Ressources

- **Repo GitHub** : https://github.com/Alessandro-Bonnafous/Survain-POC
- **Issues & Milestones** : https://github.com/Alessandro-Bonnafous/Survain-POC/issues
- **Vision complète** : [`docs/Vision_du_Jeu.md`](docs/Vision_du_Jeu.md)
- **Guide joueur** : [`docs/INSTALL.md`](docs/INSTALL.md)
- **Contribution** : [`docs/CONTRIBUTING.md`](docs/CONTRIBUTING.md)

---

*Dernière mise à jour : 2026-05-17 (Sprint 1 — décision système d'items, issue #5)*
