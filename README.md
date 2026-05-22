# 🏰 SURVAIN — POC

> Jeu PC en cours de prototypage mêlant **survie**, **city-builder**, **politique féodale** et **mythologie nordique**.
> Le joueur commence seul dans une contrée sauvage, bâtit un village avec ses PNJ, puis s'intègre progressivement dans une structure territoriale allant jusqu'au royaume.

**🚧 Statut :** POC — Sprint 0 (Fondations) · Unity 6 LTS · 3D low-poly

---

## 📖 Table des matières

- [Vision du jeu](#-vision-du-jeu)
- [Stack technique](#️-stack-technique)
- [Prérequis](#-prérequis)
- [Installation (dev)](#-installation-dev)
- [Structure du projet](#-structure-du-projet)
- [Workflow de développement](#-workflow-de-développement)
- [Packaging d'un build](#-packaging-dun-build)
- [Versioning et tags](#️-versioning-et-tags)
- [Contribuer](#-contribuer)
- [Documentation](#-documentation)
- [Licence](#-licence)

---

## 🎯 Vision du jeu

SURVAIN propose une expérience évolutive à quatre échelles territoriales : **Contrée** (solo) → **Province** (coop) → **Région** (PvP organisé) → **Royaume** (open world). Le POC se concentre sur la **boucle solo en Contrée**, cœur fondateur du jeu.

Différenciateurs :
- Craft non-répétitif avec transmission par livres de recettes
- Stuff exclusivement craftable, jamais lootable
- Combat anti-zerg à effectifs fixes
- Économie fermée sans hôtel des ventes
- Mécaniques divines inspirées du panthéon nordique

📄 Détails complets : [`docs/Vision_du_Jeu.md`](docs/Vision_du_Jeu.md)

---

## 🛠️ Stack technique

| Couche | Choix | Version |
|---|---|---|
| Moteur | Unity | 6 LTS |
| Render pipeline | URP (Universal Render Pipeline) | 17.x |
| Langage | C# | .NET Standard 2.1 |
| Input | Unity Input System | dernière stable |
| IDE recommandé | Visual Studio 2022 Community | 17.x |
| VCS | Git + Git LFS | — |
| CI/CD | _à définir_ | — |

---

## 📋 Prérequis

Installer dans cet ordre :

1. **[Unity Hub](https://unity.com/download)** puis depuis le Hub installer **Unity 6 LTS** avec :
   - Module « Microsoft Visual Studio Community 2022 » (s'installe automatiquement avec le workload Unity)
   - Module « Documentation »
2. **[Visual Studio 2022 Community](https://visualstudio.microsoft.com/vs/community/)** avec le workload **« Game development with Unity »**
3. **[Git for Windows](https://gitforwindows.org)**
4. **[Git LFS](https://git-lfs.com)** (indispensable avant le premier clone)

Vérifier :

```bash
git --version
git lfs version
```

**Specs minimales recommandées :** 16 Go de RAM, SSD, GPU dédié (même modeste).

---

## 🚀 Installation (dev)

### Premier clone

```bash
git lfs install
git clone https://github.com/Alessandro-Bonnafous/Survain-POC.git
cd Survain-POC
git lfs pull
```

### Ouvrir le projet dans Unity

1. Lancer **Unity Hub**
2. `Add` → pointer vers le dossier `Survain-POC/` cloné
3. Sélectionner la version **Unity 6 LTS** installée
4. Ouvrir le projet (la première ouverture compile les assets, ça peut prendre plusieurs minutes)
5. Ouvrir la scène `Assets/Scenes/Main.unity` (à créer lors du Sprint 0)

### Lancer en mode éditeur

Appuyer sur **▶️ Play** dans la barre supérieure de l'éditeur Unity.

### Assets tiers

Deux sources d'assets externes alimentent le projet — chacune avec une politique de versioning différente.

#### 🛍️ Unity Asset Store (modèles 3D, packs Synty…)

Les packs **Unity Asset Store** ne sont **pas versionnés** (la Standard Unity Asset Store EULA interdit leur redistribution publique). À réimporter manuellement après chaque clone, avec ton compte Unity.

| Pack | Lien Asset Store | Dossier d'import attendu |
|---|---|---|
| SimpleNaturePack | _lien à compléter_ | `Assets/ThirdParty/SimpleNaturePack/` |
| Synty Sidekick Characters | [voir Asset Store](https://assetstore.unity.com/publishers/5217) (publisher Synty) | `Assets/Synty/SidekickCharacters/` ⚠️ chemin forcé par le pack |
| Synty PolygonGeneric _(Sprint 2 — construction)_ | [voir Asset Store](https://assetstore.unity.com/publishers/5217) | `Assets/ThirdParty/Synty/PolygonGeneric/` |

**Procédure d'import :**

1. Ouvrir le projet dans Unity.
2. `Window` → `Package Manager` → sélectionner **« My Assets »** en haut à gauche (nécessite d'être connecté au compte Unity propriétaire du pack).
3. Chercher le pack par son nom → **Download** → **Import**.
4. Dans la fenêtre d'import : tout cocher (ou au minimum `Models/`, `Materials/`, `Prefabs/`, `Textures/`) → **Import**.

**Diagnostic d'un pack manquant :** prefabs roses dans la scène, messages "Missing Reference" en Console au démarrage, ou couleurs aberrantes (souvent dues à des materials Built-in dans un projet URP). Refaire la procédure d'import du pack concerné.

**Conversion URP des materials (si nécessaire) :** certains packs anciens sont livrés avec des materials Built-in qui apparaissent **roses** sous URP. Fix : `Window` → `Rendering` → `Render Pipeline Converter` → choisir `Built-in to URP` → cocher `Material Upgrade` → `Initialize and Convert`. Opération locale, pas committée.

**Ajouter un nouveau pack au projet** (checklist pour les contributeurs) :
1. Vérifier que son chemin d'import est couvert par `.gitignore` (`/Assets/ThirdParty/` ou path-spécifique).
2. Ajouter une ligne dans le tableau ci-dessus.
3. Acter la décision dans `CLAUDE.md` (journal des décisions).

---

#### 🎬 Mixamo (animations de personnages)

Les animations gameplay (locomotion, saut, attaques…) viennent de [**mixamo.com**](https://www.mixamo.com) (Adobe, **gratuit** avec un compte Adobe). Contrairement aux packs Store, les FBX Mixamo **sont versionnés** dans le repo (`Assets/Animation/Mixamo/`, en Git LFS via `*.fbx`). Pas de réimport après clone.

**Pourquoi versionner Mixamo et pas l'Asset Store ?** Mixamo permet la redistribution dans un projet, l'Asset Store EULA non.

**Procédure pour télécharger une nouvelle anim :**

1. Aller sur [mixamo.com](https://www.mixamo.com) (connecté avec un compte Adobe).
2. Chercher l'anim désirée (ex: `Walking`, `Punching`, `Crouch Idle`).
3. Sur la page de l'anim, panneau droit :
   - **In Place** : ✅ coché (pas de déplacement racine — on bouge via `CharacterController`)
   - **Trim** : laisser par défaut sauf si l'anim a du temps mort en début/fin
   - **Overdrive** : 100 (vitesse standard ; calibrage fin via le `Speed` du Blend Tree dans Unity)
4. **Download** → format `FBX for Unity` → **Skin** = `Without Skin` (on a déjà nos persos) → **Frames per Second** = 30 → **Keyframe Reduction** = none.
5. Sauvegarder dans `Assets/Animation/Mixamo/<NomAnim>.fbx`.

**Configuration Unity après import** (checklist incontournable) :

| Étape | Réglage | Pourquoi |
|---|---|---|
| 1. Onglet `Rig` | `Animation Type` = **Humanoid** | Active le retargeting Humanoid |
| 2. Onglet `Rig` | `Avatar Definition` = **Create From This Model** | Rigs Mixamo ≠ rigs Synty → `Copy From Other Avatar` plante |
| 3. Onglet `Rig` | **Apply** | Génère l'avatar sous-asset du FBX |
| 4. Onglet `Animation` | `Loop Time` ✅ + `Loop Pose` ✅ pour les anims qui bouclent (Idle/Walk/Run) | Mixamo ne les active pas par défaut → l'anim joue une fois et fige ("personne se fige et glisse") |
| 5. Onglet `Animation` | `Loop Time` ❌ pour les one-shots (Jump, Punch, Attack) | Pas de boucle souhaitée |
| 6. **Apply** | — | Sauve la config |

**Test rapide après import :** preview FBX en bas de l'Inspector → joue l'anim → vérifie que le cycle reboucle (loopable) ou s'arrête (one-shot) comme attendu.

**Pour brancher l'anim dans le projet :** voir l'`AnimatorController` `Assets/Animation/PlayerAvatar.controller` comme exemple (Blend Tree de locomotion + state Jump + state Punch, paramètres `speed`/`isGrounded`/`isJumping`/`isHarvesting`).

---

## 📁 Structure du projet

```
Survain-POC/
├── Assets/                  # Contenu Unity (scripts, scènes, assets)
│   ├── Scripts/             # Code C# gameplay
│   ├── Prefabs/             # Préfabriqués réutilisables
│   ├── ScriptableObjects/   # Configs data-driven (recettes, biomes, PNJ…)
│   ├── Scenes/              # Scènes Unity
│   ├── Art/                 # Modèles, textures, matériaux
│   ├── Audio/               # Sons et musiques
│   └── UI/                  # Prefabs et scripts UI
├── Packages/                # Dépendances Unity (géré par Unity)
├── ProjectSettings/         # Config du projet Unity
├── docs/                    # Documentation
│   ├── INSTALL.md           # Guide joueur (installer et lancer)
│   ├── CONTRIBUTING.md      # Règles de contribution
│   └── Vision_du_Jeu.md     # Vision produit
├── CLAUDE.md                # Contexte persistant pour les sessions Claude
├── README.md                # Ce fichier
├── .gitignore               # Fichiers ignorés par Git
└── .gitattributes           # Règles Git LFS + fins de ligne
```

---

## 🔄 Workflow de développement

### Branches

On suit un **GitHub Flow** simple :

- `main` → toujours stable, buildable à tout moment
- `feat/<nom-court>` → nouvelle fonctionnalité (ex: `feat/player-controller`)
- `fix/<nom-court>` → correction de bug
- `chore/<nom-court>` → tâche technique (refacto, CI, docs…)

**Règle :** jamais de push direct sur `main`. Toujours passer par une Pull Request.

### Commits — Conventional Commits

Format : `<type>(<scope>): <description>`

Types usuels : `feat`, `fix`, `chore`, `refactor`, `docs`, `test`, `perf`, `build`.

Exemples :

```
feat(player): ajout du déplacement WASD
fix(inventory): corriger crash quand slot vide
refactor(npc): extraire la logique besoins dans un service
docs(readme): préciser la procédure de build
```

Les commits sont en français pour la partie description ; les types restent en anglais (convention standard).

### Pull Requests

1. Créer une branche à partir de `main`
2. Développer et committer
3. Pousser et ouvrir une PR vers `main`
4. Lier l'issue GitHub correspondante (`Closes #42`)
5. Self-review + demande de relecture
6. Merge en **Squash and merge** pour garder `main` propre

### Issues

Toutes les tâches sont tracées sur [les issues GitHub](https://github.com/Alessandro-Bonnafous/Survain-POC/issues) et rattachées à un **milestone** (Sprint 0 → Sprint 5).

Labels : `sprint:*`, `type:*` (feature, bug, architecture, docs…), `priority:*`, `scope:poc`.

---

## 📦 Packaging d'un build

### Build Windows Standalone depuis l'éditeur

1. `File` → `Build Profiles`
2. Plateforme : **Windows**
3. `Add Open Scenes` pour inclure la scène courante
4. `Build` → choisir un dossier de destination (ex: `Builds/win64/vX.Y.Z/`)
5. Unity produit un exécutable `SURVAIN.exe` et ses dépendances

### Build en ligne de commande

Le script `Assets/Scripts/Editor/BuildScript.cs` expose une méthode statique `BuildScript.BuildWindows()` invocable en batch mode Unity (sans ouverture de l'éditeur graphique).

**Comportement :**
- Lit la version depuis `PlayerSettings.bundleVersion` (configurable dans `Edit` → `Project Settings` → `Player`)
- Produit `Builds/win64/<version>/SURVAIN.exe`
- Inclut la scène `Assets/Scenes/Main.unity`
- Exit code `0` si succès, `1` si échec (utilisable en CI)

**Commande type (PowerShell, depuis la racine du repo) :**

```powershell
& "C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "." `
  -buildTarget Win64 `
  -executeMethod Survain.Editor.BuildScript.BuildWindows `
  -logFile Builds/build.log
```

**Équivalent Bash / Git Bash :**

```bash
"/c/Program Files/Unity/Hub/Editor/<version>/Editor/Unity.exe" \
  -batchmode -nographics -quit \
  -projectPath "." \
  -buildTarget Win64 \
  -executeMethod Survain.Editor.BuildScript.BuildWindows \
  -logFile Builds/build.log
```

Remplace `<version>` par la version Unity installée localement (ex: `6000.4.3f1`). Les logs détaillés du build atterrissent dans `Builds/build.log` (gitignored). Le dossier `Builds/` lui-même est gitignored.

⚠️ **Important : ferme l'éditeur Unity avant de lancer la commande.** Unity batch mode pose son propre lock sur `Temp/UnityLockfile` ; si l'éditeur graphique est ouvert, le batch quitte immédiatement avec exit code 1 et un log très court qui se termine par `Exiting without the bug reporter`. Vérifie aussi dans le Task Manager qu'aucun processus `Unity.exe` orphelin ne traîne.

Le build met **1 à 5 minutes** la première fois (recompile shaders + scripts) ; le terminal paraît figé — c'est normal en batch mode. Pour suivre la progression en temps réel, ouvre une autre fenêtre et lance :

```powershell
Get-Content Builds/build.log -Wait -Tail 30
```

> **Note :** ce script utilise volontairement `UnityEngine.Debug.Log/LogError` (et non le wrapper `SurvainLog`) car il tourne en batch mode où les defines `UNITY_EDITOR`/`DEVELOPMENT_BUILD` ne sont pas garantis. C'est l'unique exception autorisée à la convention de logging du projet — réservée au code Editor de build pipeline.

---

## 🏷️ Versioning et release

On utilise **SemVer** avec préfixe `v` : `vMAJOR.MINOR.PATCH`. Pendant le POC, on tague à la fin de chaque sprint :

| Tag | Signification |
|---|---|
| `v0.1.0` | Fin Sprint 0 — Fondations |
| `v0.2.0` | Fin Sprint 1 — Récolte & Craft |
| `v0.3.0` | Fin Sprint 2 — Construction |
| `v0.4.0` | Fin Sprint 3 — PNJ & Village |
| `v0.5.0` | Fin Sprint 4 — Combat & Zone Sauvage |
| `v1.0.0-poc` | Fin Sprint 5 — POC complet |

Des **builds intermédiaires** (preview de fin d'issue, validation visuelle avec le PO) peuvent être taggués avec un suffixe : `v0.1.1-preview`, `v0.2.0-rc1`, etc.

### Procédure de release (manuelle)

End-to-end, du build local à la GitHub Release avec binaire attaché. **L'éditeur Unity doit être fermé** pendant tout le processus (cf. section précédente).

#### 1. Bumper la version

Ouvrir Unity → `Edit` → `Project Settings` → `Player` → `Version` → entrer la nouvelle valeur (ex: `0.2.0`). Sauver (`Ctrl+S` de la scène ou Project Settings auto-save). Commit le diff `ProjectSettings/ProjectSettings.asset` :

```powershell
git add ProjectSettings/ProjectSettings.asset
git commit -m "chore(release): bump version 0.2.0"
git push
```

#### 2. Lancer le build batch mode

Fermer Unity. Depuis PowerShell, à la racine du repo :

```powershell
& "C:\Program Files\Unity\Hub\Editor\<version-unity>\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "." `
  -buildTarget Win64 `
  -executeMethod Survain.Editor.BuildScript.BuildWindows `
  -logFile Builds/build.log
```

Vérifier en fin de build : `Builds/win64/<version>/SURVAIN.exe` doit exister.

#### 3. Zipper le dossier de build

```powershell
$version = "0.2.0"  # adapter
Compress-Archive -Path "Builds/win64/$version/*" `
                 -DestinationPath "Builds/SURVAIN-v$version-win64.zip" `
                 -Force
```

⚠️ Inclure **tout le dossier** `Builds/win64/<version>/` (pas seulement le `.exe`) — Unity génère aussi un dossier `*_Data/` et des DLLs nécessaires au runtime.

#### 4. Tagger et pousser

```powershell
git tag -a "v0.2.0" -m "Sprint 1 — Récolte & Craft"
git push origin v0.2.0
```

#### 5. Créer la GitHub Release avec le binaire

```powershell
gh release create v0.2.0 `
  --title "v0.2.0 — Sprint 1 (Récolte & Craft)" `
  --notes-file Builds/RELEASE_NOTES.md `
  Builds/SURVAIN-v0.2.0-win64.zip
```

Variantes utiles :
- `--draft` : la release n'est visible que par les collaborateurs du repo (utile pour partager un build privé avec le PO sans le rendre public).
- `--prerelease` : marque la release comme « pre-release » dans GitHub UI (utile pour les `*-preview`, `*-rc*`).
- `--notes "Texte court"` : remplace `--notes-file` si tu veux passer les notes inline.

#### 6. (Optionnel) Tester le téléchargement

Aller sur la page Release → cliquer le `.zip` → extraire → lancer `SURVAIN.exe`. Le jeu doit démarrer comme en éditeur.

### Pourquoi pas (encore) de CI ?

La convention CLAUDE.md acte : *« CI/CD : à discuter à partir du Sprint 2 »*. Une fois le projet stabilisé, l'idée est de **basculer cette procédure vers GitHub Actions** (build cloud déclenché sur push de tag, release auto). Issue de suivi : voir le board GitHub (label `tech / infra`).

---

## 🤝 Contribuer

Voir [`docs/CONTRIBUTING.md`](docs/CONTRIBUTING.md) pour les conventions de code, la procédure de soumission et le code de conduite.

En deux mots :

1. Regarder les [issues ouvertes](https://github.com/Alessandro-Bonnafous/Survain-POC/issues) (notamment celles du milestone courant)
2. Ouvrir une branche, développer, ouvrir une PR
3. Attendre relecture, merger

---

## 📚 Documentation

| Document | Public | Contenu |
|---|---|---|
| [`README.md`](README.md) | Dev | Ce fichier |
| [`CLAUDE.md`](CLAUDE.md) | Claude | Contexte persistant pour l'IA pair-programmer |
| [`docs/INSTALL.md`](docs/INSTALL.md) | Joueur | Installer et lancer le jeu |
| [`docs/CONTRIBUTING.md`](docs/CONTRIBUTING.md) | Contributeur | Règles de code et process |
| [`docs/Vision_du_Jeu.md`](docs/Vision_du_Jeu.md) | Tout public | Vision produit complète |

---

## 📜 Licence

**Tous droits réservés** — voir [`LICENSE`](LICENSE).

Le code source est consultable publiquement sur GitHub à des fins de lecture uniquement. Aucun usage, copie, modification ou redistribution n'est autorisé sans accord écrit préalable des auteurs. Cette licence pourra évoluer si le projet bascule vers un modèle open-source à l'avenir.

---

## 🙏 Équipe

- **Pascal** — Product Owner, design
- **Alessandro** — Développement
- **Claude** (Anthropic) — Pair-programmer IA

---

*« Dans Midgard comme ailleurs, on ne bâtit rien seul. »*
