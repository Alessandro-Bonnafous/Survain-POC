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

### Assets tiers à réimporter

Certains assets utilisés dans le projet proviennent du **Unity Asset Store** et ne sont **pas versionnés** dans le repo (la Standard Unity Asset Store EULA interdit leur redistribution publique). Il faut donc les réimporter manuellement après le clone, avec ton propre compte Unity.

| Asset | Source | Dossier d'import attendu |
|---|---|---|
| SimpleNaturePack | _Asset Store — lien à compléter par le PO_ | `Assets/ThirdParty/SimpleNaturePack/` |

**Procédure d'import :**

1. Ouvrir le projet dans Unity.
2. Ouvrir le **Package Manager** : `Window` → `Package Manager`.
3. En haut à gauche, sélectionner **« My Assets »** (nécessite d'être connecté à ton compte Unity).
4. Chercher le pack par son nom, cliquer **Download** puis **Import**.
5. Dans la fenêtre d'import, **tout cocher** (ou au minimum les dossiers `Models/`, `Materials/`, `Prefabs/`, `Textures/`).
6. Cliquer **Import**.

> Si un asset apparaît dans une scène mais que ses prefabs sont absents (icônes roses ou messages d'erreur de référence manquante), c'est qu'un pack n'a pas été importé. Refais la procédure ci-dessus.

> **Note pour les contributeurs :** ne jamais committer le contenu d'un pack du Store. Le `.gitignore` couvre les packs déjà connus ; si tu en ajoutes un nouveau, ajoute son chemin dans `.gitignore` ET dans le tableau ci-dessus, puis acte la décision dans `CLAUDE.md`.

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

### Build en ligne de commande (pour CI future)

```bash
"C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe" \
  -batchmode -nographics -quit \
  -projectPath "<chemin>/Survain-POC" \
  -buildTarget Win64 \
  -executeMethod BuildScript.BuildWindows \
  -logFile build.log
```

> Le script `BuildScript.cs` reste à créer dans `Assets/Editor/` (prévu au Sprint 0).

---

## 🏷️ Versioning et tags

On utilise **SemVer** avec préfixe `v` : `vMAJOR.MINOR.PATCH`.

Pendant le POC, on tague à la fin de chaque sprint :

| Tag | Signification |
|---|---|
| `v0.1.0` | Fin Sprint 0 — Fondations |
| `v0.2.0` | Fin Sprint 1 — Récolte & Craft |
| `v0.3.0` | Fin Sprint 2 — Construction |
| `v0.4.0` | Fin Sprint 3 — PNJ & Village |
| `v0.5.0` | Fin Sprint 4 — Combat & Zone Sauvage |
| `v1.0.0-poc` | Fin Sprint 5 — POC complet |

Créer et pousser un tag :

```bash
git tag -a v0.1.0 -m "Sprint 0 — Fondations"
git push origin v0.1.0
```

Une **GitHub Release** est créée à chaque tag, avec notes de version et build attaché quand pertinent.

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
