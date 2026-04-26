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

**Sprint en cours :** Sprint 0 — Fondations

**Objectifs du sprint** : mise en place technique et premier monde explorable.
- [x] Setup projet Unity (issue #1, mergée)
- [x] Architecture dossiers et scripts fondateurs (issue #1, mergée)
- [x] Génération procédurale de terrain — 1 biome forêt tempérée (issue #2, mergée le 2026-04-26)
- [ ] Contrôleur joueur 3D, 3e personne (issue #3, **en cours** sur branche `3-contrôleur-joueur-3e-personne`)
- [ ] Cycle jour/nuit basique

**Livrable du sprint :** build où le joueur peut se déplacer dans un monde 3D généré.

**Dernière décision en date :** _voir le journal ci-dessous._

**Prochain milestone :** Sprint 1 — Récolte & Craft.

---

## ⏳ Décisions en attente (à arbitrer)

> Petites décisions remontées du chat / Discord qui n'ont pas encore été tranchées. À traiter avant la phase qui en dépend pour ne pas bloquer.

- **Équilibrage arme « Montagnes » : 8 dmg vs 6 dmg** — à arbitrer par Pascal **avant le démarrage du Sprint 1** (rappel échange Discord). Impacte la première table d'armes craftables. Pas de code à toucher tant que la décision n'est pas prise.

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

*Dernière mise à jour : 2026-04-19 (Génération procédurale du terrain — issue #2)*
