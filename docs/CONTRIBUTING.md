# 🤝 Contribuer à SURVAIN

Merci de t'intéresser au projet ! Ce document explique comment contribuer efficacement au POC.

---

## 🎯 Avant de commencer

1. **Lire la [vision du jeu](Vision_du_Jeu.md)** pour comprendre les piliers de design.
2. **Consulter les [issues ouvertes](https://github.com/Alessandro-Bonnafous/Survain-POC/issues)** pour voir ce qui est priorisé.
3. **Lire le [README](../README.md)** pour le setup technique.

---

## 💡 Proposer une idée

Avant d'écrire du code, ouvrir une **issue de type _discussion_ ou _feature_** pour valider la pertinence avec le Product Owner. Cela évite de développer quelque chose qui ne sera pas retenu.

Template minimal :

```markdown
## Contexte
Pourquoi cette feature / cette idée ?

## Proposition
Ce que je propose concrètement.

## Alternatives envisagées
Autres pistes et pourquoi elles ont été écartées.

## Impact
Sur le gameplay, l'architecture, le planning, etc.
```

---

## 🐛 Signaler un bug

Template minimal :

```markdown
## Version
- SURVAIN : vX.Y.Z (ou commit SHA)
- OS : Windows 11
- Matériel : GTX 1070, 16 Go RAM

## Comportement attendu
...

## Comportement observé
...

## Étapes pour reproduire
1. ...
2. ...

## Logs / screenshots
Joindre Player.log et captures si pertinent.
```

---

## 🧑‍💻 Contribuer du code

### 1. Choisir une issue

Préférer les issues du **milestone en cours**, labellisées `good first issue` ou `help wanted` pour les contributeurs externes.

Assigner l'issue à soi-même (ou commenter « je prends ») pour éviter les doublons.

### 2. Créer une branche

Toujours brancher à partir de `main` à jour :

```bash
git checkout main
git pull
git checkout -b feat/<nom-court>
```

Préfixes autorisés : `feat/`, `fix/`, `chore/`, `refactor/`, `docs/`, `test/`.

### 3. Développer

- Respecter les [conventions de code](../CLAUDE.md#-conventions-de-code-c--unity) listées dans `CLAUDE.md`.
- Garder les commits **petits et cohérents**. Un commit = une idée.
- Tester en local dans l'éditeur Unity avant de pousser.

### 4. Commit — Conventional Commits

Format : `<type>(<scope>): <description courte en français>`

Types valides : `feat`, `fix`, `chore`, `refactor`, `docs`, `test`, `perf`, `build`.

Bons exemples :

```
feat(player): ajout du déplacement WASD avec course
fix(terrain): corriger les trous aux bords du monde
docs(readme): préciser la procédure de build Windows
refactor(inventory): extraire la logique de slot dans un service
```

Mauvais exemples :

```
update              ❌ trop vague
fix bug             ❌ quel bug ?
WIP                 ❌ ne pas pousser de WIP sur main via PR
```

### 5. Pull Request

Avant d'ouvrir la PR :

- [ ] Le code compile sans warning.
- [ ] Les tests (quand ils existeront) passent.
- [ ] J'ai joué la scène concernée dans l'éditeur.
- [ ] J'ai relu mon propre diff.
- [ ] Les nouveaux fichiers lourds (textures, modèles) passent bien par Git LFS.

Titre de la PR = titre de l'issue principale.

Corps de la PR :

```markdown
Closes #<numéro-issue>

## Ce qui change
...

## Comment tester
1. ...
2. ...

## Points d'attention pour la relecture
...

## Captures (si UI ou visuel)
...
```

Merge en **Squash and merge** pour garder `main` propre.

---

## 🏗️ Conventions d'architecture

Détaillées dans [`CLAUDE.md`](../CLAUDE.md#️-décisions-architecturales). À ne pas contourner sans discussion.

Points clés :

- **URP** (pas HDRP, pas Built-in).
- **New Input System** (pas l'ancien `Input.GetKey`).
- **ScriptableObjects** pour les data assets (recettes, biomes, configs).
- **Pas de singletons** sauf `GameManager`.
- **Pas de `FindObjectOfType`** en runtime.
- **Namespace** sous `Survain.<Domain>`.

---

## 📁 Où ranger quoi

| Type | Dossier |
|---|---|
| Scripts C# | `Assets/Scripts/<Domain>/` |
| Prefabs | `Assets/Prefabs/<Catégorie>/` |
| ScriptableObjects configs | `Assets/ScriptableObjects/<Catégorie>/` |
| Scènes | `Assets/Scenes/` |
| Art 3D | `Assets/Art/Models/` |
| Textures | `Assets/Art/Textures/` |
| Matériaux | `Assets/Art/Materials/` |
| Sons | `Assets/Audio/SFX/` ou `Assets/Audio/Music/` |
| UI | `Assets/UI/` |
| Scripts Éditeur | `Assets/Editor/` |

---

## 🔐 Ce qu'on **ne commit pas**

- Secrets (clés API, tokens, credentials) → utiliser un `.env` local hors VCS
- Fichiers volumineux non destinés au jeu (screenshots de debug, vidéos de test) → partager hors repo
- Assets sans licence claire (modèles trouvés sur internet sans attribution)
- Le dossier `Library/` ou `Temp/` (déjà couvert par `.gitignore`)

---

## 💬 Communication

- Discussions techniques courtes : commentaires d'issue ou de PR.
- Discussions de design plus larges : issue dédiée avec label `discussion`.
- Décisions structurantes : journalisées dans [`CLAUDE.md`](../CLAUDE.md#-journal-des-décisions-append-only).

---

## 📜 Code de conduite

Être respectueux. Critiquer le code, pas la personne. Préférer les questions aux affirmations quand on n'est pas sûr. Donner du contexte quand on propose un changement.

---

*Dernière mise à jour : 2026-04-17*
