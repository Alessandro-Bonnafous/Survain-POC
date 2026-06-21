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

**Sprint en cours :** Sprint 5 — Boucle Complète & Polish (épic combat #16, Phase A livrée — Phase B en cours sur B4 #84)
_(Sprint 4 livré ci-dessous ; Sprint 1 reste ouvert sur #8 craft, bloqué sur arbitrage Pascal — voir Décisions en attente)_

**Objectifs Sprint 4 (livré)** : combat (placeholder), ennemis PVE, zone sauvage, mort & perte de stuff.
- [~] Système de combat à l'énergie (**épic #16**, rattaché **Sprint 5**) — la vraie mécanique n'est plus « décalée » : elle est **cadrée et lancée**. Spec PO : [`docs/Spec_combat.md`](docs/Spec_combat.md). #16 décomposé en **plan agile** : **Phase A** énergie & ressenti (#81 réserve+HUD, #82 auto-attack, #83 esquive/course → build jouable `v0.6.0`) · **Phase B** profondeur (#84 dégâts typés, #85 armures _dépend craft #8_, #86 kit de compétences, #87 finition) · **Phase C** équilibrage (#88). **Gate structurelle Pascal par chunk** (Q1→Q5, voir Décisions en attente). **Phase A bouclée : A1 (#81) + A2 (#82) + A3 (#83) livrés** (réserve + barre HUD ; auto-attack pilotée par l'énergie — remplace le placeholder hache/pioche `v0.5.0` ; esquive i-frames + course consommant l'énergie). **Build jouable du combat à l'énergie taggé `v0.6.0-preview`** (validation d'étape ; `bundleVersion` 0.6.0). **Q1 acté provisoirement : réserve unique partagée (modèle A), en placeholder** (confirmation Pascal souhaitée, non bloquante). **Phase B lancée sur B4 (#84) : modèle de dégâts typés** (`DamageType` + `DamageInfo`, décomposition biome/physique 80/20 en placeholder) branché sur `PlayerEnemyStrike` → `EnemyController.TakeDamage(DamageInfo)`, crochet `WeaponData.BuildHit()` prêt pour le craft #8. Suite : **B5 armures** (dépend craft #8), **B6 kit de compétences** (gated Q4), **B7 finition**, puis **Phase C** équilibrage (#88).
- [x] Ennemis PVE & IA hostile (issue #17) — `EnemyData` + state machine Patrol→Chase→Attack→Return + aggro (ph.1) ; HP + mort + loot + frappe placeholder clic gauche (ph.2A) ; variété loup/troll/bandit + densité/respawn (ph.2B). Livré. ⚠️ **Attaque ennemie = telegraph SANS dégâts** (en attente vie joueur #19 / combat #16).
- [x] Zone sauvage & exploration (issue #18) — terrain adjacent distinct + ressources (ph.1) ; frontière franchissable (edge falloff) + entrée/ambiance (`WildZone`) + danger (ph.2). Livré.
- [x] Système de mort et perte de stuff (issue #19) — vie joueur (`PlayerHealth` + barre HUD) + **dégâts ennemis branchés** (#17) (ph.1) ; mort (écran + décompte) + **tombe lootable** (timer 5 min) + respawn (ph.2) ; **lit posable** + respawn au lit activable (E) + faisceau & marqueur de tombe (ph.3). Livré. `SetSheltered` (PNJ) laissé à l'habitation PNJ.
- [x] Zone sauvage instanciée via PNJ (issue #74) — instance régénérée à l'accès (nouveau seed → terrain/ressources/NavMesh/ennemis) via PNJ portail ; **barrière** (accès portail only) ; **tombe préservée** si présente (récup loot) ; snap au sol des points. Livré (version B). Vraie scène séparée = post-POC (dépend d'un save).
- [ ] CI release auto via GitHub Actions (issue #37) — transverse, en fond
- [ ] Vrais prefabs visuels des bâtiments (issue #46) — gated arbitrage pack Synty (Pascal), non bloquant

**Livrable visé :** survivre en zone sauvage — affronter des ennemis, mourir (perdre son stuff en tombe) et réapparaître.

**Sprint 3 — PNJ & Village (clôturé) :** IA PNJ (#12), besoins faim/moral/abri (#13), métiers via contremaître (#14), routines & recrutement (#15). **Release `v0.4.0` publiée.**

**Sprint 2 — Construction (clôturé) :** placement/chantier (#9), bâtiments fonctionnels (#10), destruction/réparation (#11), vrais prefabs (#46). **Release `v0.3.0` publiée.**

**Sprint 1 — Récolte & Craft (en cours, 4/5) :**
- [x] Items & ScriptableObjects (#5), récolte (#6), avatar Synty + Mixamo (#33), inventaire (#7)
- [ ] Craft basique tier gris (#8) — ⚠️ bloqué arbitrage Pascal (mécanique d'engagement non-répétitive, pilier clé)

**Sprint 0 — Fondations (clôturé le 2026-05-16) :** setup Unity (#1), terrain forêt tempérée (#2), contrôleur 3e personne (#3), caméra (#4), cycle jour/nuit (#28).

**Dernière décision en date :** _voir le journal ci-dessous._

**Prochain milestone :** Phase A du combat livrée (`v0.6.0-preview`). **Phase B en cours** : B4 (#84) modèle de dégâts typés en PR ; ensuite B5 armures (bloqué craft #8), B6 kit de compétences (gate Q4 Pascal), B7 finition, puis Phase C équilibrage (#88).

---

## ⏳ Décisions en attente (à arbitrer)

> Petites décisions remontées du chat / Discord qui n'ont pas encore été tranchées. À traiter avant la phase qui en dépend pour ne pas bloquer.

- **Gates structurelles du combat (épic #16) — Q1→Q5 envoyées à Pascal (Discord)** — chaque gate porte sur le **modèle**, pas sur les chiffres (les chiffres restent des placeholders SO, voir Q5). À trancher **avant de coder le chunk concerné** :
  - **Q1 — Énergie = réserve unique partagée** (course + esquive + compétences puisent dans les mêmes 100 pts) ? → consommée en **A2 (#82) / A3 (#83)**. ⚠️ **A1 (#81) est neutre vis-à-vis de Q1** : la réserve + la barre HUD ne présupposent pas le modèle partagé. **Acté provisoirement (2026-06-19) : modèle A = pool unique partagé, en placeholder** — pour débloquer A2/A3 ; risque de refonte faible (la spec pointe clairement vers A ; seul un « non » → pools séparés/hybride coûterait). Confirmation Pascal toujours souhaitée.
  - **Q2 — Split des dégâts** : 80 % biome / 20 % physique sur chaque arme, et 75 % / 25 % côté résistances d'armure, confirmés ? → **B4 (#84) / B5 (#85)**.
  - **Q3 — Mix d'armures = builds** : modèle 5 pièces + résistances cumulées (builds spécialisés/polyvalents) confirmés ? → **B5 (#85)**.
  - **Q4 — Kit de compétences** : lié à l'**arme** (changer d'arme change le kit) ou lié au **personnage** ? → **B6 (#86)**.
  - **Q5 — Ordre « ressenti d'abord »** : tous les chiffres restent en placeholders SO ajustables jusqu'à une session d'équilibrage dédiée (**#88**) ?
  - ⚠️ **La Phase B (armures, #85) dépend du craft #8**, lui-même bloqué arbitrage Pascal.
- **Équilibrage arme « Montagnes » : 8 dmg vs 6 dmg** — à arbitrer par Pascal **avant la première table d'armes craftables** (Sprint 1 ne touche qu'aux outils — bois, pierre, fibre, hache/pioche en pierre — donc plus bloquant pour Sprint 1). Probable horizon : Sprint 2+. Pas de code à toucher tant que la décision n'est pas prise.
- **Mécanique de craft non-répétitive (issue #8)** — à arbitrer avec Pascal **avant implémentation de #8**. Proposition de l'issue : QTE/timing simple pour le tier gris, qualité du résultat dépendante de la performance joueur. Choix structurant pour tout le système de craft (les tiers vert/bleu hériteront du pattern). Pas de code Sprint 1 sur le craft tant que pas tranché.
- **Modèle de construction « chantier » (issue #9)** — tranché par Aless (la construction n'est pas un pilier non-négociable), **à valider a posteriori par Pascal**. Si le PO préfère un autre modèle, le `ConstructionSite` reste le socle le plus flexible (la pose instantanée en est un cas dégénéré). Non bloquant.
- **Ratios économiques de construction** — coût des bâtiments (`building-hut` = 8 bois + 4 pierre, `building-shed` = 6 bois, `building-chest` = 4 bois, `building-campfire` = 3 bois + 2 pierre, `building-workshop` = 6 bois + 4 pierre, `building-bed` = 6 bois), **% remboursé à la destruction** (`BuildingData.RefundRatio`, défaut **50%**) et **facteur de coût de réparation** (`PlayerBuildingTool._repairCostFactor`, défaut 1). Tous placeholders paramétrables en `.asset`/Inspector. À arbitrer par Pascal au pass d'équilibrage (levier « économie fermée », non bloquant).
- **Équilibrage mort & survie joueur (#19)** — PV max joueur (`PlayerHealthConfig`, défaut **100**), régén (4/s après 6 s), invulnérabilité post-coup (0,3 s), **dégâts d'attaque des ennemis** (`EnemyData.AttackDamage`, ex. 8), **délai de respawn** (5 s) et **timer de disparition de la tombe** (`PlayerDeath._graveDespawnSeconds`, défaut **5 min**). Placeholders paramétrables en `.asset`/Inspector. À arbitrer par Pascal au pass d'équilibrage (non bloquant).

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
- **Construction NON-MODULAIRE (bâtiments entiers)** : on pose des structures **finies en un bloc**, jamais de construction pièce-par-pièce (mur/sol/toit) façon Valheim/Rust. Pas de système de sockets/snap entre pièces. Le détail et les raisons : cf. journal 2026-05-30. Les catégories `Foundation/Wall/Roof/Door/Window` de `BuildCategory` restent inertes (réservées si T3 était un jour réactivé).

---

## 🧠 Journal des décisions (append-only)

> **Format** : `YYYY-MM-DD — <titre court>` puis contexte, décision, alternatives considérées, conséquences.
> **Ordre** : antéchronologique (plus récent en haut).

### 2026-06-21 — Combat : synchro anim/dégât (1 swing = 1 coup) + récup des bulles perdues au merge

**Contexte.** Session de polish de l'état actuel (avant Phase B suivante). Bug constaté par le PO : l'auto-attack appliquait **3-4 dégâts pour 2 animations**. Cause : les dégâts partaient **immédiatement au clic** (raycast caméra), gatés par un simple cooldown de 0,4 s, tandis que l'anim (Chop/Mine) était purement **cosmétique et réactive** (event `Swung` → trigger `isHarvesting`) sur sa propre horloge — deux timelines indépendantes, le clip durant plus que le cooldown. **Les dégâts n'étaient pas appliqués au contact de la hache.**

**Incident merge (corrigé dans la même PR).** La PR #93 (bulles de dégâts) avait été mergée dans sa branche parente `feat/combat-degats-types` au lieu de `main` ; après le squash-merge de #92, son contenu **n'avait jamais atteint `main`** (piège PR stackées + squash). Réintégré à l'identique depuis `origin/feat/combat-bulles-degats` en tête de cette PR (commit `fix(combat): réintègre les bulles…`).

**Objections du PO (qui ont fait pivoter l'approche).** Une première version « délai d'impact en code » (durée de swing + frame d'impact en constantes) a été écartée sur deux remarques justes du PO : (a) **hache et pioche n'ont pas la même durée d'anim** → une durée unique en dur est fausse ; (b) **risque de conflit avec les compétences (B6)** : le verrou « occupé » et le timing ne doivent pas être enterrés dans l'auto-attack. Conclusion : la **source de vérité du timing, c'est l'animation**, et le verrou « occupé » doit être un concept **partageable**.

**Décision (timing piloté par l'animation, choisie par le PO).**
1. **Le clic lance un swing, il ne fait plus de dégât direct.** `PlayerEnemyStrike` passe d'un coup instantané à un **modèle de swing** : au clic (ennemi visé + énergie OK), on **verrouille** (`IsSwinging`) et on émet `Swung` (l'anim part). → **1 swing = 1 coup**.
2. **Impact piloté par l'animation, verrou piloté par une durée** (séparation clé). L'**impact** (frame où la hache touche, sensible par arme : hache ≠ pioche) vient d'un **Animation Event à la frame de contact** → `NotifyAnimationImpact()` (dégât une seule fois, re-raycast au contact → coup dans le vide si la cible s'est échappée). Le **verrou/cadence** vient d'une **durée gameplay** `_swingDurationSeconds` (= vitesse d'attaque). Relais par **`PlayerAttackAnimationRelay`** posé sur l'avatar (l'Animator est sur l'enfant, la logique sur _Player).
3. **Pourquoi le verrou n'est PAS dérivé d'un event de fin de clip** (correction après test PO) : une 1ʳᵉ version déverrouillait sur un Animation Event `AnimSwingEnd`. En pratique, un event de fin mal placé (même légèrement avant la fin visuelle) **libérait le verrou trop tôt** → en martelant, un nouveau swing relançait le clip par-dessus l'anim en cours = coups rapprochés (et l'anim qui « repart »). Le verrou par durée est **robuste au placement des events**. `AnimSwingEnd` est conservé en **no-op** (ne casse pas un event déjà posé). Le **filet de sécurité** : si `AnimImpact` est absent, le coup tombe en fin de `_swingDurationSeconds`. Garde `_impactApplied` → impact **exactement une fois**.
   - **Bug connexe corrigé** : `EnemyController.Die()` **désactive les colliders immédiatement** (Destroy est différé en fin de frame) → le raycast de frappe ne retrouve plus un cadavre, fini les swings « dans le vide » sur un ennemi déjà mort.
4. **`IsSwinging` exposé = socle réutilisable** par le futur **coordinateur de compétences (B6)** (exclusion auto-attack ↔ compétences). **Le coordinateur lui-même n'est PAS construit maintenant** (sa forme dépend de Q4, non tranché) — seule la primitive de timing, non jetable, est posée.
5. **Énergie consommée au commit** (au clic, ennemi visé) — un swing engagé coûte même s'il rate ensuite. Cohérent A2. Note : les clips Chop/Mine servent aussi à la récolte → les events s'y déclenchent mais sont ignorés hors swing de combat (garde `IsSwinging`).
6. **Anti-martelage.** Le verrou tient `_swingDurationSeconds` (à régler ≈ durée du clip le plus long) → marteler le clic ne peut pas relancer un swing avant ce délai. C'est la future **vitesse d'attaque** (→ WeaponData, per-arme).

**Tâche éditeur (Aless, côté Unity).** Poser `PlayerAttackAnimationRelay` sur l'avatar (celui qui a l'Animator) ; ajouter sur les clips Chop et Mine **un** Animation Event `AnimImpact` à la frame de contact (un `AnimSwingEnd` éventuel devient inutile/no-op). Régler `_swingDurationSeconds` ≈ durée du clip le plus long. Sans `AnimImpact`, seul le filet de sécurité tourne (fonctionnel, non synchro).

**Alternatives écartées.** Délai d'impact en code (durée unique fausse pour 2 clips, timeline parallèle à re-synchroniser à la main) ; durées par arme en code (2 sources de vérité code↔clip, fragile) ; construire le coordinateur multi-actions tout de suite (prématuré, dépend de Q4) ; garder le dégât au clic en allongeant le cooldown (masque le problème, pas de synchro).

**Conséquences.**
- DoD : impossible d'appliquer plus d'un coup par animation ; le dégât tombe à la frame de contact (events) ou au filet de sécurité ; aucune régression énergie/esquive/bulles.
- Patterns dégagés : **timing piloté par Animation Events + relais sur l'avatar** (`PlayerAttackAnimationRelay`) ; **verrou `IsSwinging`** = socle du coordinateur de combat B6 ; **filet de sécurité temporisé** pour ne jamais casser le build avant câblage éditeur. Réutilisables tels quels par les compétences.
- ⚠️ **Modif de scène requise côté éditeur** (composant relais sur l'avatar + events sur les clips) — contrairement aux étapes précédentes ; c'est intrinsèque au pilotage par animation.
- Polish restant identifié (hors session, non bloquant) : **esquive** = vraie roulade (event `Dodged` + clip Mixamo) ; **ennemis** = vrais modèles (capsules → prefabs, **gated assets/budget Pascal** comme #46).

### 2026-06-21 — Combat : bulles de dégâts typées (feedback visuel B4)

**Contexte.** Le modèle de dégâts typés (B4, #84) n'était observable qu'en Console — invisible pour le PO sur un build taggé. Besoin d'un feedback visuel : des **bulles de dégâts flottantes** au-dessus de l'ennemi, **colorées par type**, pour rendre la décomposition biome/physique lisible en jeu. PR dédiée (séparée de #84 = le modèle).

**Décisions.**
1. **`DamageNumberOverlay`** (`Survain.UI`) calqué trait pour trait sur `NpcStatusOverlay` : Canvas screen-space singleton auto-créé via `[RuntimeInitializeOnLoadMethod]`, libellés `Text` legacy (`LegacyRuntime.ttf`, pas de dépendance TMP), positionnés via `WorldToScreenPoint`. **Zéro setup scène** (`Main.unity` intact).
2. **Deux nombres séparés et colorés** (choix PO) : part de biome (couleur du biome) + part physique (gris), dans une même bulle via **rich text** (`<color=#…>`). Couleurs : Forêt = vert, Plaines = doré, Montagnes = bleu froid, Côte maritime = rouge. Parts arrondies à 0 omises.
   - **Démo deux cas (choix PO)** : au POC, `PlayerEnemyStrike.ResolveBiomeType()` dérive le biome de l'outil-arme équipé — **hache → Forêt (vert)**, **pioche → Montagnes (bleu)** — pour visualiser deux types distincts. Fallback sur le champ placeholder ; migrera sur `WeaponData.BiomeDamageType` au craft #8.
3. **Bulles fire-and-forget, position monde capturée** à l'instant du coup → elles continuent de monter/s'estomper **même après la destruction de l'ennemi** (même logique que le burst détaché de `ResourceNodeJuice`). **Pool interne réutilisé** (pas d'instanciation par coup) ; fade via `CanvasGroup.alpha` (le rich text impose sa propre couleur RGB → la transparence passe par le CanvasGroup, pas par `Text.color`).
4. **Hook = `EnemyController.TakeDamage(DamageInfo)`** (le seul point qui a la décomposition typée) : un appel `DamageNumberOverlay.Show(transform.position, hit)`. La surcharge `int` (récolte placeholder, attaques bâtiment) n'en émet pas → bulles réservées au combat typé. Couleurs/tailles/durée = placeholders (#88).

**Alternatives écartées.** Texte world-space / TextMesh billboard (l'overlay screen-space est le pattern maison dominant) ; un seul nombre total coloré (le PO veut voir le split biome/physique) ; instanciation/`Destroy` par coup (préféré un pool) ; fade via `Text.color` (incompatible avec les tags de couleur rich text → `CanvasGroup`).

**Conséquences.**
- Le PO peut **valider visuellement** les dégâts typés sur le build (à noter en release note). Aucune régression (additif, off du chemin `int`).
- Pattern dégagé : **overlay de texte flottant fire-and-forget + pool + anchor monde** (réutilisable pour les soigns, le crit, le « miss/parry » du combat, les gains de ressources).

### 2026-06-21 — Combat Phase B / B4 : modèle de dégâts typés (biome + physique)

**Contexte.** Phase A livrée et taguée `v0.6.0-preview` (A1/A2/A3). Démarrage de la **Phase B** par **B4 (#84)** : poser le **modèle de dégâts typés** — un coup décompose son total en **part de biome + part physique** (spec Q2 : 80/20). Visible en debug. Stub sur les armes actuelles (outils hache/pioche), car les **vraies armes craftables dépendent du craft #8** (bloqué Pascal). **Q2 non confirmée par Pascal** → on code le *modèle*, le 80/20 reste en placeholder ajustable.

**Décisions.**
1. **Enum `DamageType` dédié**, et non réemploi de `BiomeConfig.BiomeType`. Deux raisons : (a) le combat exige un membre `Physical` qui n'a aucun sens côté worldgen ; (b) le **roster de biomes de combat arbitré par le PO — Forêt / Plaines / Montagnes / Côte maritime** — ne mappe pas 1:1 sur le roster worldgen (ForetTemperee/Plaine/Toundra/DesertAride). Découpler évite un couplage load-bearing worldgen↔combat. Membres : `Foret`/`Plaines`/`Montagnes`/`CoteMaritime` (+ `Physical`). Valeurs de dégâts = placeholders ajustables (#88).
2. **`DamageInfo` = readonly struct immuable** (`BiomeType` + `BiomeAmount` + `PhysicalAmount`, `Total = somme`), construite via la factory `DamageInfo.Split(total, biomeFraction, biomeType)`. Zéro allocation. Nouveau namespace **`Survain.Gameplay.Combat`** (dossier miroir `Assets/Scripts/Gameplay/Combat/`).
3. **`EnemyController.TakeDamage(DamageInfo)` = nouvelle surcharge**, l'`int` historique conservé (back-compat A2 — seul `PlayerEnemyStrike` l'appelait). En B4, **sans armures**, on applique `hit.TotalRounded` tel quel et on **logge la décomposition** (`SurvainLog`, catégorie AI). **Crochet B5** documenté dans la méthode : l'atténuation par résistances typées viendra ici, avant le retrait des PV.
4. **`PlayerEnemyStrike` produit un coup typé** : `DamageInfo.Split(_damagePerHit, _biomeDamageFraction, _biomeDamageType)` → `enemy.TakeDamage(hit)`. L'énergie (A2) est intacte (split en aval de `TryConsume`). Biome + fraction = **placeholders sérialisés sur le composant** (ajustables Inspector), pas de SO jetable — même raisonnement qu'A2 : ces champs migreront sur `WeaponData`. Les armes du POC étant des **outils** (hache/pioche), c'est le composant qui porte les placeholders actifs.
5. **`WeaponData` enrichi (crochet, pas finalisé)** : champs `_biomeDamageType` + `_biomeDamageFraction` + méthode `BuildHit()` qui fabrique le `DamageInfo`. C'est le **futur foyer** des dégâts du joueur ; quand le craft #8 équipera de vraies `WeaponData`, la source lira `weapon.BuildHit()` au lieu des placeholders du strike. Non câblé tant qu'aucune `WeaponData` n'est équipée.
6. **Zéro modif de scène** : nouveaux `SerializeField` à valeur par défaut (Unity applique le défaut au load, pas de re-save), références combat auto-résolues côté struct/factory. `Main.unity` diff nul.

**Alternatives écartées.** Réutiliser `BiomeConfig.BiomeType` (pas de `Physical`, couplage worldgen/combat, roster qui diverge de la spec) ; SO `DamageSplitConfig` jetable (les valeurs sont par-arme → vont sur `WeaponData`, comme A2 a refusé `PlayerCombatConfig`) ; remplacer l'`int` de `TakeDamage` (casserait la back-compat — on ajoute une surcharge) ; appliquer les résistances dès B4 (B5 dépend du craft #8, hors scope).

**Conséquences.**
- DoD B4 : frapper un ennemi applique un coup **décomposé biome/physique** (vérifiable au log debug) ; aucune régression A1/A2/A3 ; `Main.unity` intact ; tout ajustable en Inspector.
- Patterns dégagés : **coup typé `DamageInfo` + factory `Split`** (réutilisable par compétences B6, attaques ennemies, pièges) ; **surcharge typée non-cassante** sur un récepteur de dégâts ; **crochet de résistances** posé dans `TakeDamage(DamageInfo)` pour B5.
- Gates : **Q2 (80/20) reste à confirmer Pascal** — chiffre en placeholder, aucun figé en dur. **Q4 (kit lié arme/perso)** à trancher avant B6. **B5 armures bloqué craft #8.**

### 2026-06-19 — Combat A3 : esquive (i-frames) + course consommant l'énergie

**Contexte.** Dernière étape de la Phase A (#83) : livrer l'**arbitrage mobilité vs attaque** (cœur anti-zerg) — course qui draine l'énergie + esquive (dash + invulnérabilité) qui la consomme (spec : 40 %). Boucle le ressenti du combat à l'énergie → **build jouable candidat `v0.6.0`**.

**Décisions.**
1. **Esquive + course logées dans `PlayerController`** (il possède le `CharacterController`, la map `Player` et la direction de mouvement) plutôt qu'un composant satellite — la locomotion est son scope.
2. **Course = drain continu** : sprint en mouvement consomme `SprintEnergyPerSecond * dt` via `PlayerEnergy.TryConsume` ; à sec → retour marche (pas de blocage dur). Pas de drain pendant le dash (évite la double conso).
3. **Esquive = dash bref + i-frames** : `TryConsume(DodgeEnergyCost)` (40) ; succès → dash (`DodgeSpeed`/`DodgeDurationSeconds`) dans la direction d'input (sinon `forward`) + **i-frames** via une nouvelle API `PlayerHealth.GrantInvulnerability(seconds)` (fenêtre dédiée, distincte de l'invuln post-coup ; ne raccourcit jamais une fenêtre active). À sec → pas d'esquive (log). Pas de relance en plein dash.
4. **Nouvelle action `Dodge`** dans l'`InputActionAsset` (clavier **Left Ctrl**, manette **East/B**). **Optionnelle** côté `PlayerController` (absente → esquive désactivée, warn, locomotion intacte) — pas un échec dur comme Move/Jump/Sprint.
5. **`PlayerEnergy`/`PlayerHealth` auto-résolus via `GetComponent`** (même `_Player`) → **zéro modif de `Main.unity`**. Tuning (drain, coût, i-frames, vitesse/durée du dash) = **placeholders sur `PlayerMovementConfig`** (#88).

**Alternatives écartées.** Composant `PlayerDodge` séparé (le dash a besoin du CC + vitesse privés du controller) ; esquive sur double-tap directionnel (moins lisible au POC qu'une touche dédiée) ; réutiliser `InvulnerabilitySeconds` post-coup pour les i-frames (préféré une fenêtre dédiée `GrantInvulnerability`) ; bloquer la course à sec par une vitesse nulle (préféré le retour marche).

**Conséquences.**
- **Phase A bouclée** une fois #83 mergé → **build jouable du combat à l'énergie** (ennemis PVE #17) : candidat tag `v0.6.0` (décision Pascal). Reste Phase B (gates + craft #8) et Phase C (équilibrage #88).
- Patterns dégagés : **`PlayerHealth.GrantInvulnerability`** (i-frames réutilisables : parry, compétences) ; **action input optionnelle** (warn au lieu de désactiver le composant) ; **dash via override de la vitesse horizontale** dans le controller.
- Crochet : une anim d'esquive (roulade) viendra au polish — un event `Dodged` s'ajoutera comme `Jumped` quand le clip existera.

### 2026-06-19 — Combat Phase A : A1 livré, A2 lancée (auto-attack à l'énergie) + Q1 acté (pool partagé)

**Contexte.** A1 (#81, réserve d'énergie + barre HUD) mergé (PR #89) et testé OK. Enchaînement sur A2 (#82) : faire de l'auto-attack la **vraie source de dégâts joueur, pilotée par l'énergie** (spec : 5 % par coup). A2/A3 consomment le **modèle d'énergie** (Q1), encore en attente d'arbitrage Pascal.

**Décisions.**
1. **Q1 acté provisoirement = modèle A (pool unique partagé)**, en placeholder, pour ne pas bloquer A2/A3. La spec (« après 2 esquives, plus assez d'énergie pour les compétences ») n'a de sens qu'avec un pool partagé → risque de refonte faible. Confirmation Pascal toujours souhaitée (un « non » → pools séparés/hybride serait une refonte d'archi, pas un tuning).
2. **A2 = enrichir `PlayerEnemyStrike` en place** (pas de renommage : la scène le référence par GUID + `m_EditorClassIdentifier`). Le coup ne part que si `PlayerEnergy.TryConsume(coût)` réussit ; à sec → feedback (log throttlé), pas de dégât ni d'anim.
3. **Énergie consommée uniquement sur un coup de combat** (ennemi visé), **pas sur un clic à vide** — sinon une frappe de récolte (même action `Attack`) drainerait l'énergie de combat.
4. **`PlayerEnergy` auto-résolu via `GetComponent`** (même GameObject `_Player`) → **zéro modif de scène** pour A2. Coût (`_energyCostPerAttack`, défaut 5) + dégâts/cooldown restent des **placeholders sur le composant** : ils migreront sur `WeaponData` avec les vraies armes (Phase B, #84) — donc pas de SO `PlayerCombatConfig` jetable créé maintenant.
5. **`[ContextMenu]` debug de `PlayerEnergy` retiré** : la vraie consommation (A2) le rend caduc.

**Alternatives écartées.** Attendre Pascal sur Q1 avant tout code A2 (préféré acter A + avancer, refonte improbable) ; créer un SO `PlayerCombatConfig` pour dégâts/vitesse/cooldown (jetable — ces champs vont sur `WeaponData` en Phase B) ; consommer l'énergie à chaque swing y compris à vide (drainerait la récolte) ; renommer `PlayerEnemyStrike` (casserait la réf de scène).

**Conséquences.**
- DoD A2 : attaquer un ennemi PVE consomme l'énergie ; impossible d'attaquer à sec. Reste A3 (#83 esquive i-frames + course) pour le **build jouable candidat `v0.6.0`**.
- Crochet Phase B : `_damagePerHit`/`_hitCooldown`/`_energyCostPerAttack` → futurs champs de `WeaponData` (#84).

### 2026-06-14 — Cadrage du système de combat : épic #16 décomposé en plan agile (Phases A/B/C)

**Contexte.** Spec PO mise à jour ([`docs/Spec_combat.md`](docs/Spec_combat.md)), bien plus riche que le pilier « endurance » initial : armes/armures **typées biome + physique** (80/20 et 75/25), **énergie partagée** (réserve 100 pts, conso 5 %→50 %), kit **auto + 4 compétences + ultimate**, **2 armes** équipées + switch, course/esquive consommant l'énergie. #16 était une issue monolithique bloquée arbitrage Pascal.

**Décisions.**
1. **Découper #16 en épic + sous-issues** (Phases A/B/C, créées via `setup_combat_issues.sh`) : A=#81/#82/#83, B=#84/#85/#86/#87, C=#88. Épic + A1 rattachés au **Sprint 5**. Gate **structurelle** Pascal **par chunk** — on tranche le *modèle* (Q1→Q5), pas les chiffres.
2. **Tous les « à définir » = placeholders SO** réglés au **pass d'équilibrage (#88)**, non bloquant.
3. **Séparer ressenti et profondeur** : **Phase A** (énergie/ressenti) démarrable maintenant ; **Phase B** (dégâts typés, armures, kit) gated + **couplée au craft #8** (armures #85, bloqué Pascal).
4. **A1 (#81) démarrée** : réserve d'énergie + barre HUD, **neutre vis-à-vis de Q1** (le modèle « réserve partagée » ne sera consommé qu'en A2/A3).

**Alternatives écartées.** Garder #16 monolithique et bloqué ; attendre Pascal sur l'intégralité de la spec avant tout code ; figer les chiffres maintenant (préféré placeholders SO + pass dédié).

**Conséquences.**
- Q1→Q5 envoyées à Pascal (Discord) — voir « Décisions en attente ». Phase A lancée sur A1 (`PlayerHealth` trio cloné → `PlayerEnergy`).
- Phase B gated craft #8. Le placeholder hache/pioche (`v0.5.0`) sera remplacé par l'auto-attack #82.
- Pattern réutilisé : trio `PlayerHealth` / `PlayerHealthConfig` / `PlayerHealthBar` (#19) → `PlayerEnergy` / `PlayerEnergyConfig` / `PlayerEnergyBar` (satellite + SO + HUD singleton lazy).

### 2026-06-12 — Placeholder de combat (hache/pioche comme armes) pour la démo v0.5.0 (#16 décalé)

**Contexte.** #16 (combat à l'endurance, pilier « anti-zerg à effectifs fixes ») reste bloqué arbitrage Pascal. Pour tagger une démo `v0.5.0` montrant le Sprint 4, on met un placeholder jouable (même logique que le craft #8) et on décale la vraie mécanique.

**Décision.** Pas d'arme dédiée : **la hache et la pioche servent d'armes**. `PlayerEnemyStrike` ne blesse que si une arme (hache/pioche) est équipée et émet un event `Swung` ; `PlayerVisualAnimator` rejoue l'anim de l'outil (Chop/Mine via `harvestType`) sur `Swung`, exactement comme `HitLanded` pour la récolte. Réutilise tout le pipeline outil-en-main + anim (2026-05-31) : **zéro nouvel asset, zéro modif Animator**, références auto-résolues (`GetComponent`) → pas de câblage scène.

**Alternatives écartées.** Épée dédiée (nouveau `ToolType` + mesh en main + état Animator « Slash ») → préféré réutiliser hache/pioche (plus rapide, zéro asset) ; « tout clic blesse » (placeholder #17) → préféré gater les dégâts à l'arme équipée pour un ressenti combat.

**Conséquences.**
- **Démo `v0.5.0` taggable** : Sprint 4 livré (#17/#18/#19/#74 + ce placeholder de combat). **#16 reste ouvert** (vraie mécanique d'endurance, arbitrage Pascal).
- Pattern : un event de « coup » (`Swung`) qui réutilise le trigger d'anim existant (`isHarvesting` routé par `harvestType`) — la vraie attaque #16 remplacera `PlayerEnemyStrike` en gardant ce branchement visuel.
- ⚠️ Incident scène (rappel ré-appris) : une sauvegarde de `Main.unity` **en mode Play** a re-contaminé la scène (~309k lignes, avatar `Combined Character` + meshes bakés). Discard nécessaire (`git restore`), qui a fait perdre des réglages de scène **non committés** (personnage du `NpcPortal` + ref `WildInstanceManager._exitPortal`) — refaits proprement ensuite. **Ne jamais `Ctrl+S` la scène en mode Play.**

### 2026-06-09 — Zone sauvage instanciée via PNJ portail (Sprint 4, #74 clos)

**Contexte.** Dernière brique fonctionnelle du Sprint 4 : transformer la zone sauvage adjacente (#18) en **instance régénérée à l'accès** via un PNJ portail. Niveau **(B) « instance simulée »** retenu pour le POC (régénération in-place, mono-scène) ; le niveau (A) vraie scène séparée reste post-POC (dépend d'un save). Découpé en 2 phases (orchestrateur/régénération, puis portails d'interaction). PR unique `Closes #74`.

**Décisions.**
1. **`WildInstanceManager`** (sur `_WildZone`) orchestre l'instance : `EnterWild()` régénère (nouveau seed → terrain → ressources → rebake NavMesh → reset ennemis) puis téléporte le joueur à l'entrée ; `ExitWild()` ramène au village.
2. **Tombe vs régénération (arbitrage, réflexion Aless)** : `EnterWild` **ne régénère pas s'il existe une tombe dans l'emprise de la zone** (préserve le layout pour récupérer le loot) ; la régén reprend une fois la tombe vidée/expirée. Détection via `Grave.All` + bounds XZ du terrain sauvage.
3. **Accès portail uniquement (arbitrage)** : **barrière** périmétrique (colliders invisibles créés en code autour du terrain sauvage) → plus de passage à pied ; le PNJ portail (`PortalNpc`, `IInteractable`) est le seul accès, un `WildExitPortal` (totem) pour ressortir. La frontière franchissable de #18 est neutralisée.
4. **Régénération runtime** : `TerrainGenerator.GenerateWithSeed` / `ResourceNodeSpawner.GenerateWithSeed` (injection de seed) ; `NavMeshRuntimeBaker.Rebake` (surface unique re-bakée) ; `EnemySpawner.ResetSpawns` (suivi interne + destroy/respawn). **Fix** `ResourceNodeSpawner.ClearNodes` : renomme le root « Nodes » avant `Destroy` (différé) pour qu'un `Generate` enchaîné la même frame ne reparente pas les nouveaux nœuds au root mourant.
5. **Snap au sol** (terrains générés au runtime) : entrée + portail de sortie recollés au terrain sauvage **à chaque régén** (le relief change) ; point de retour + `PortalNpc` recollés au terrain village au Start. `WildInstanceManager.SnapToGround` (raycast descendant filtré `MeshCollider` → terrain uniquement), exposé en static et réutilisé par `PortalNpc`.

**Alternatives écartées.** Niveau (A) vraie scène instanciée maintenant (post-POC, dépend d'un save) ; régénération systématique à chaque entrée (perdrait la tombe → cassait la récupération du loot) ; garder le passage à pied + portail (instance « leaky ») ; bloquer la frontière en désactivant l'edge falloff (préféré une barrière de colliders, fiable) ; placement manuel des points en éditeur (impossible : terrain généré au runtime → snap par code).

**Conséquences.**
- **#74 clos → Sprint 4 fonctionnellement bouclé** (reste **#16 combat**, bloqué arbitrage Pascal — game design). Release `v0.5.0` à la clôture (décision Pascal).
- Patterns dégagés : **régénération d'instance in-place** (terrain+ressources+NavMesh+ennemis sur nouveau seed) ; **`SnapToGround`** (poser tout objet statique sur un terrain généré au runtime) ; **barrière de colliders** (borner une zone) ; **préservation conditionnelle** d'une instance selon un état persistant (tombe).
- Crochet post-POC : la vraie instance multi-scène (A) réutilisera `WildInstanceManager` (seed + orchestration) une fois le save en place.
- Limite POC : la zone sauvage reste physiquement adjacente (offset) ; le vrai dépaysement viendra avec la scène séparée (A).

### 2026-06-09 — Mort du joueur & perte de stuff (Sprint 4, #19 clos)

**Contexte.** 3ᵉ brique du Sprint 4 : introduire la **vie du joueur** (inexistante jusque-là), la mort punitive-mais-récupérable (vision « Mort = perte de stuff + tombe ») et le respawn. Découpé en 3 phases (data→logique→UI, 1 commit code + 1 commit câblage scène par phase) : ph.1 vie + dégâts + barre HUD ; ph.2 mort + tombe + respawn ; ph.3 lit + respawn au lit + marqueur. PR unique `Closes #19`.

**Décisions.**
1. **Vie joueur = `PlayerHealth`** (satellite sur `_Player`, distinct de `PlayerController`) : HP, `TakeDamage`/`Heal`, régén après délai, fenêtre d'invulnérabilité, events `HealthChanged`/`Died`/`Revived`, `Instance` statique. Config SO `PlayerHealthConfig` (`Survain.Data`). Barre de vie HUD `PlayerHealthBar` auto-construite (pattern singleton type `InteractionPrompt`).
2. **Source de dégâts branchée maintenant (arbitrage)** : l'`EnemyAttackState` (#17, telegraph sans dégâts) inflige enfin `EnemyData.AttackDamage` en fin de windup → la zone sauvage devient réellement dangereuse, sans attendre le combat #16 (orthogonal : #16 = endurance côté joueur).
3. **Perte de stuff = tout (sac + hotbar) en tombe (arbitrage)** : à la mort, `PlayerDeath` (abonné à `Died`) déverse tout l'inventaire dans une **`Grave`** = conteneur lootable autonome (réutilise `IInteractable` + `ContainerUI`, comme un coffre). **Timer** de disparition du loot (défaut 5 min).
4. **Gel pendant la mort** : `PlayerController` désactivé (stoppe le déplacement ; effet de bord voulu — `Instance` passe à null → les ennemis se désengagent du cadavre) + `UiMode.Push` (neutralise récolte/frappe, fige la caméra). `DeathScreen` auto-construit (voile + « VOUS ÊTES MORT » + décompte). Tout rendu au respawn.
5. **Respawn (arbitrage)** : priorité au **lit « maison » activé** (`RespawnPoint.Active`), sinon **feu de camp le plus proche** du village (`Building.FindNearest` sur `EmitsLight`), sinon **position de spawn initiale**. `PlayerController.Teleport` (désactive le CC le temps de forcer la position + reset vitesse).
6. **Lit posable (arbitrage « lit à activer »)** : nouveau champ fonctionnel `BuildingData.ProvidesRespawn` → `ConstructionSite.Complete` ajoute un `RespawnPoint` (`IInteractable`). Le joueur l'**active via E** pour en faire son point de repos (un seul actif, `static Active`). Asset `building-bed` (6 bois) via `BuildingsBootstrap`, ajouté au catalogue `BuildModeController` (Inspector). Réutilise le système chantier #9.
7. **Tombe « facile à retrouver »** : **faisceau lumineux** vertical (colonne Unlit + point light) sur la tombe **+** `GraveMarker` (overlay écran auto-construit : distance + clamp aux bords quand hors champ, registre `Grave.All`).

**Alternatives écartées.** Attendre #16 pour les dégâts (préféré brancher maintenant) ; perte partielle / hotbar conservée (préféré tout en tombe) ; tombe = items au sol en vrac ou sans timer (préféré conteneur + timer) ; **auto-destruction de la tombe au vidage** (implémentée puis retirée : détruire en plein drag laissait le ghost collé à la souris → on s'appuie sur le timer) ; respawn au lit le plus proche auto (préféré lit à activer via E) ; respawn centre village fixe / contremaître. Branchement `NpcNeeds.SetSheltered` (PNJ abrités par un lit) **laissé hors #19** (nécessite une assignation lit↔PNJ — chantier habitation PNJ).

**Conséquences.**
- **#19 clos → Sprint 4 fonctionnellement bouclé** (reste #74 instance zone sauvage planifiée fin de sprint, puis release `v0.5.0`). Le pilier « Mort = perte de stuff + tombe » est livré.
- Patterns dégagés : **`PlayerHealth.Instance`** (source de dégâts ciblant le joueur sans `FindObjectOfType`) ; **séquence de mort via event `Died` + gel par désactivation du controller + `UiMode`** ; **conteneur lootable autonome** (`Grave`, réutilisable pour d'autres drops persistants) ; **champ fonctionnel `BuildingData.ProvidesRespawn`** (comme `StorageCapacity`/`EmitsLight`) ; **marqueur d'écran clampé** (`GraveMarker`, réutilisable pour tout objectif à pointer) ; **registre statique `Grave.All`**.
- Crochets : la vraie attaque de combat #16 remplacera la frappe placeholder ET pourra moduler les dégâts au joueur ; `SetSheltered` reste à brancher quand l'habitation PNJ arrivera.
- ⚠️ **Incident scène évité** : une sauvegarde de `Main.unity` en mode Play a contaminé la scène (avatar `Combined Character` + os, ~3000 lignes) ; détectée au diff, restaurée et recâblée proprement en mode Édition (rappel : ne jamais Ctrl+S la scène en Play).

**Reste-à-faire (tracé).** Équilibrage PV/dégâts/timer tombe/coût lit (Pascal) ; `NpcNeeds.SetSheltered` à l'habitation PNJ.

### 2026-06-09 — Zone sauvage : terrain adjacent, frontière franchissable, entrée & danger (Sprint 4, #18 clos)

**Contexte.** 2ᵉ brique du Sprint 4. Direction validée (Aless) : les zones sauvages seront à terme des **scènes instanciées** (façon Guild Wars 1), accessibles via un PNJ et **régénérées à chaque accès**. En attendant, on livre une zone **adjacente générée**, conçue **scene-ready**. Découpé en 2 phases (#72 terrain+ressources, #73 frontière+entrée+danger).

**Décisions.**
1. **Terrain adjacent distinct** : un 2ᵉ `TerrainGenerator` sous un root **`_WildZone` self-contained** (extractible en scène plus tard), positionné à côté du village, avec des settings « sauvages » dédiés (`WildTerrainGeneration` : gradient sombre, relief + accidenté, seed distinct). Le mesh étant construit en coords locales centrées sur le GameObject, une simple position décalée suffit.
2. **`ResourceNodeSpawner` centré sur le terrain** : les tirages se font autour de `_terrainCollider.bounds.center` (et non l'origine du monde) → permet de peupler un terrain **décalé**. Rétro-compatible.
3. **Edge falloff** (`TerrainGenerationSettings.EdgeFalloff/Height/Width`, smoothstep dans `TerrainGenerator.SampleHeight`) : aplanit le relief vers les bords jusqu'à une **altitude commune**. Deux terrains partageant la même baseline se rejoignent à plat → **jointure franchissable** par le `CharacterController` (résout la transition brusque). Désactivé = comportement historique.
4. **NavMesh** : **surface unique** couvrant les deux terrains (Collect All, bake runtime via `NavMeshRuntimeBaker`) → agents franchissent la jointure. Le NavMesh par scène viendra avec la vraie instanciation.
5. **`WildZone`** (détection par **polling** des bounds, pas `OnTriggerEnter` — peu fiable entre `CharacterController` et trigger statique sans Rigidbody) + **`WildZoneBanner`** (UI auto-construite façon `InteractionPrompt` : bannière transitoire + voile rouge tant qu'on est dans la zone).
6. **Danger & difficulté progressive** par **placement manuel** d'`EnemySpawner` (#17) dans la zone (loup en lisière → troll au fond) — pas de scaling codé (game design à arbitrer Pascal).

**Alternatives écartées.** Vraie scène instanciée maintenant (nécessite une couche save/persistance multi-scène, hors POC → issue #74) ; zone = sous-région distance-based d'un terrain unique (préféré un vrai 2ᵉ terrain) ; surface NavMesh dédiée + NavMeshLink (préféré surface unique au POC) ; rampe physique / chevauchement pour la frontière (préféré l'edge falloff, propre et réutilisable) ; `OnTriggerEnter` (remplacé par polling) ; points d'intérêt camps/grottes/ruines (#4 de l'issue) **reportés post-POC** (level design/prefabs).

**Conséquences.**
- **#18 clos.** Patterns dégagés : **edge falloff** (jointure de zones / lissage de bordure réutilisable) ; **détection de zone par polling de bounds** ; **`_WildZone` self-contained** (anticipe la migration en scène). `ResourceNodeSpawner` désormais terrain-agnostique.
- **Reste-à-faire** (tracés) : lisser davantage la transition village↔sauvage (blend de terrain) ; **vraies ressources rares** dédiées (le spawner sauvage pointe pour l'instant des ressources existantes) ; **#74** instance via PNJ (fin Sprint 4) ; points d'intérêt (post-POC).

### 2026-06-09 — Ennemis PVE & IA hostile (Sprint 4, #17 clos)

**Contexte.** 1ʳᵉ brique du Sprint 4, attaquée avant #16 (combat, bloqué Pascal) car non bloquante. Architecture **calquée sur les PNJ** (#12). Découpé en 3 phases (#69 IA, #70 mort+loot, #71 variété+respawn).

**Décisions.**
1. **Namespace `Survain.AI.Enemies`** : `EnemyData` (SO : locomotion, aggro rayon/désaggro/laisse, combat, HP, loot, teinte+échelle visuelles), `EnemyController` + `IEnemyState`, états **Patrol → Chase → Attack → Return**. Registre statique `All`, NavMesh = autorité de position, Animator optionnel (`speed`). Même socle que `NpcController`.
2. **Aggro/désaggro centralisé** dans le controller (les états ne le testent pas) ; cible **`PlayerController.Instance`** (accès statique ajouté pour éviter `FindObjectOfType`). Layer **`Threat`** → les PNJ fuient automatiquement (`NpcPerception`, #12).
3. **Mort + loot** : `EnemyController.TakeDamage`/`Die` → déverse la loot table (`WorldItemSpawner`) + event `Died` (consommé par le spawner pour le **respawn**, maintien de densité). **Source de dégâts POC = clic gauche** via `PlayerEnemyStrike` (placeholder, coexiste avec récolte/démolition, exclusif par cible, gaté `UiMode.IsActive`) — en attendant le combat #16.
4. **`EnemyAttackState` = telegraph SANS dégâts** : windup visible, pas de dégât au joueur (vie joueur #19 / combat #16 à venir).
5. **Variété loup/troll/bandit** : plusieurs `EnemyData` tirés au hasard par `EnemySpawner` ; différenciation visuelle sur le prefab placeholder (Capsule) via teinte + échelle uniforme appliquées au `Start` (`Renderer.material`). Assets rangés dans `ScriptableObjects/Enemies/`.

**Alternatives écartées.** Attendre #16 pour tester mort/loot (préféré la frappe placeholder clic gauche) ; modèles d'ennemis dédiés (préféré Capsule placeholder teintée/scalée, self-contained, pas de dépendance Synty) ; aggro testée par chaque état (préféré centralisée) ; scaling de difficulté codé (reporté, placement manuel #18).

**Conséquences.**
- **#17 clos.** Patterns réutilisables : **architecture ennemie calquée sur les PNJ** ; **`PlayerController.Instance`** ; **frappe placeholder gatée** (à remplacer par #16) ; **respawn via event `Died`**. Crochet prêt : l'attaque ennemie deviendra la **source de dégâts du joueur en #19** (brancher le telegraph).
- Limite placeholder : l'échelle agrandit visuel + collider mais pas le rayon du `NavMeshAgent` (collision identique) — sans impact POC.

### 2026-06-05 — Fixes POC : carving bâtiments, click-through UI, fusion de stacks (#64, #65, #67)

**Contexte.** Trois bugs repérés en testant le village vivant (#15), traités en lots `fix/` distincts (1 PR chacun).

**Décisions / implémentation.**
1. **Carving NavMesh des bâtiments** (#64) : `Building` ajoute un `NavMeshObstacle` Box (`carving` + `carveOnlyStationary`) à son empreinte (`data.Size`, pivot au sol) à la complétion. Le NavMesh est baké une seule fois au runtime (`NavMeshRuntimeBaker`) et ne contient pas les bâtiments posés ensuite → le carving creuse le trou dynamiquement ; libéré au `Destroy` (démolition rouvre le passage). Même pattern que `ResourceNode.SetupNavObstacle` (#12).
2. **Click-through UI** (#65) : un clic gauche sur un slot d'inventaire/coffre déclenchait aussi l'action monde (démolition `PlayerBuildingTool`, récolte `PlayerHarvester`) « à travers » l'UI. `UiMode` expose désormais `IsActive` (vrai dès qu'un panneau est ouvert, via son comptage de références) ; les deux outils ne ciblent ni n'agissent tant que `IsActive`. Pattern « gating par flag lu », comme le mode construction.
3. **Fusion de stacks au drag & drop** (#67) : déposer un stack sur un slot portant le même item stackable échangeait au lieu de cumuler. `Inventory.MergeOrSwap` / `MergeOrSwapAcross` (+ helper `TryMerge`) : même item stackable → fusion jusqu'à `MaxStackSize` (reliquat conservé à la source), sinon échange. `Swap`/`SwapAcross` restent inchangés (usage général) ; seul `InventoryDragController.OnDropOnSlot` bascule sur les variantes.

**Conséquences.** Conventions réutilisables : **carving sur tout bâtiment statique** ; **`UiMode.IsActive` pour neutraliser les actions monde sous un panneau** (à répliquer pour tout futur outil sur clic) ; **merge-or-swap** pour le drag d'items stackables (le merge intelligent laissé en suspens en #7 est désormais fait).

### 2026-06-05 — Routines quotidiennes, planning de vie & recrutement (Sprint 3, #15 clos)

**Contexte.** Dernière brique du Sprint 3 : donner au village un rythme de vie et la capacité de grandir. Découpé data→logique→UI, 1 PR/phase (#62 routine nocturne, #63 planning + social, #66 recrutement).

**Décisions.**
1. **`WorldClock`** (service statique découplé) : `IsNight` / `Phase` / `Time01` / `IsMealTime` / `HasClock` + event `OnPhaseChanged`, alimenté par `DayNightCycle`. L'IA des PNJ lit l'heure sans dépendre du composant visuel ni de `FindObjectOfType` (`DayNightCycle` reste l'autorité visuelle et publie son état). Préféré à un statique posé directement sur `DayNightCycle`.
2. **Routine nuit** : `SleepingState` (squelette de #12) piloté — le PNJ rentre à son **foyer (`HomePosition`)** et se repose jusqu'au jour, anti-blocage façon `EatingState`. **Sommeil sans lit nominal** au POC : l'abri/lit assigné + `SetSheltered` restent **#19** (Sprint 4).
3. **Planning via interruptions priorisées** (extension du pattern #13) : `fuite > désertion > faim > nuit (repos) > repas > travail`. La nuit et le repas **n'interrompent pas** un `EatingState`/repas en cours (auto-terminant à satiété) — sinon le PNJ quitte le feu dès que la faim repasse le seuil de recherche et boucle feu ⇄ foyer.
4. **`MealGatheringState`** : repas groupé au feu pendant des **créneaux** (midi + crépuscule, fenêtres `time01` tunables sur `TimeOfDayConfig`). Distinct d'`EatingState` (faim individuelle) : le PNJ reste au feu toute la durée du créneau. **Bonus moral social** s'il a de la compagnie.
5. **Événements moraux décroissants** : `NpcNeeds._eventOffset` converge vers 0 (vitesse tunable sur `NPCData`) → les bonus social/idle (et tout événement) deviennent **transitoires**. Bonus social + décroissance tunables (équilibrage Pascal).
6. **Idle social** : deux PNJ oisifs proches se tournent l'un vers l'autre et « discutent » brièvement (petit bonus social). Local à `IdleState`, pas une interruption globale.
7. **Recrutement via le contremaître** : bouton « Recruter (coût) » dans `NpcManagementPanel`, coût prélevé dans le **coffre le plus proche du contremaître** (cohérent avec les récolteurs qui y déposent), **plafond** de villageois vivants (tunable, défaut 8) → remplacement naturel des déserteurs (un départ libère une place). `NpcSpawner.SpawnVillager()` réutilisable + accès statique `Instance` ; coût prélevé **après** un spawn réussi (pas de perte de ressources). Le **contremaître est exempté** des routines (reste le hub accessible, même de nuit).

**Alternatives écartées.** Statique sur `DayNightCycle` (préféré le service `WorldClock` découplé) ; bonus moral social plat/persistant (préféré décroissant) ; lit assigné dès #15 (report #19) ; sommeil sur place (préféré retour au foyer) ; recrutement par quête de collecte ou PNJ candidat errant (préféré le bouton dans le hub) ; coût depuis le sac du joueur ou agrégé sur tous les coffres (préféré le coffre le plus proche) ; repas piloté par la seule faim (préféré un créneau dédié + état distinct).

**Conséquences.**
- **#15 clos → Sprint 3 fonctionnellement bouclé.** Suite : Sprint 4 (combat & survie).
- Patterns dégagés : **`WorldClock`** (horloge gameplay découplée, réutilisable pour météo/événements datés) ; **interruptions priorisées étendues aux routines** ; **événements moraux décroissants** (réutilisables : deuil, fête, bénédiction divine) ; **`SpawnVillager()` + recrutement** (réutilisable pour d'autres sources de PNJ).
- Crochets prêts : `SetSheltered` (#19 — le sommeil au lit assigné remplacera le retour au foyer), `SetWorkQuality` (#14).
- **Incident scène** : `Main.unity` contaminée par une **sauvegarde en mode Play** (avatar `Combined Character` + UI runtime « Fallback » bakés, ~2900 lignes) ; restaurée, seule la config de coût de recrutement committée. **À retenir : ne jamais `Ctrl+S` la scène en mode Play.**
- Limitation NavMesh en espaces serrés (#14) inchangée.

### 2026-06-05 — Métiers PNJ & gestion par le contremaître : implémentation (Sprint 3, #14 clos)

**Contexte.** Suite du modèle contremaître (décision 2026-06-04). Phases 2 (comportements de métier) et 3 (panneau de gestion + dialogue) livrées → #14 clos.

**Décisions / implémentation.**
1. **Récolteurs** (`GatherJobState`) : ciblent le nœud compatible le plus proche (`ResourceNode.FindNearest` par `RequiredTool`), récoltent via `HarvestHit(into)` — **crédit direct dans l'inventaire porté** (le chemin joueur garde le drop au sol), portent au **coffre le plus proche** et déposent. Ne travaillent **que s'il existe un coffre** (sinon oisifs).
2. **Constructeur** (`BuildJobState`) : cible le chantier actif le plus proche, **puise au coffre** les ressources requises (`ConstructionSite.RemainingFor`) et alimente (`Deposit`). Boucle de village fermée (récolte→coffre→construction).
3. **Récolte PNJ sans `ToolData`** : le PNJ ne cible que des nœuds compatibles avec son métier → pas besoin d'outil réel. Productivité modulée par le moral (`WorkSpeedMultiplier`, #13).
4. **Registres statiques** `ResourceNode.All` / `ConstructionSite.All` (+ `Building.All` de #13) + `FindNearest` — requêtes spatiales sans `FindObjectsOfType`.
5. **Travail = priorité la plus basse** (`NpcController` : fuite > désertion > faim > travail) ; un affamé sans feu **cesse de travailler**.
6. **`NpcManagementPanel`** : roster live (nom, faim/abri/moral, productivité) + assignation par cycle ◀ Métier ▶, ouvert par le contremaître (E). **Dialogue** : le contremaître pivote vers le joueur pendant l'échange (`BeginTalk/EndTalk`).
7. **`UiMode`** (mode UI centralisé à comptage de références) : curseur + gel de l'orbite caméra partagés par tous les panneaux → corrige le conflit multi-menus ET la caméra qui tournait pendant inventaire/coffre. Concrétise le `CursorOwnershipStack` anticipé en #7.
8. **Anti-blocage** : `EatingState` recalcule le chemin s'il devient partiel/invalide (nœud qui respawn et carve le NavMesh en travers) + timeout ; timeouts de déplacement sur les états de travail. Plus de PNJ figé (fini le déblocage par DEBUG).

**Note d'historique.** La phase 2A (récolteurs) a été fusionnée dans `main` **sous la PR #58 « fix(player) »** (squash), suite à un cafouillage de branche (2A committée sur `main` puis embarquée par la branche du fix ciblage). Le code est correct ; seul le libellé du commit ne reflète pas la 2A.

**Alternatives écartées.** Drop au sol + ramassage PNJ (préféré crédit direct) ; téléportation des ressources ; stock de village dédié (préféré le coffre le plus proche) ; donner un vrai `ToolData` aux PNJ ; SphereCast d'interaction (faux positifs d'obstruction) ; gestion curseur par panneau (conflit multi-menus → `UiMode`).

**Conséquences.**
- **#14 clos.** Sprint 3 → **#15** (routines quotidiennes via `SleepingState` + cycle jour/nuit, recrutement).
- Crochets prêts : `SetWorkQuality` (qualité de travail → moral, à brancher), `SetSheltered` (#19).
- **Limitation connue** : évitement NavMesh en espace étroit (bâtiments serrés) → saccades / blocage occasionnel récupérable. Polish possible (rayon de carving des nœuds, réglage d'évitement, étalement du spawn).
- Patterns dégagés : **registre statique + `FindNearest`** (réutilisable #15) ; **`UiMode`** (tout futur panneau) ; **états de métier à sous-phases + timeouts**.

### 2026-06-04 — Gestion du village via un contremaître + métiers PNJ (Sprint 3, issue #14)

**Contexte.** Pivot de vision (validé Pascal) : plutôt que de micro-gérer chaque villageois, le joueur passe par un **contremaître** — PNJ manager présent au démarrage et **point d'interaction unique** du village (gérer métiers, moral, productivité). #14 re-scopé autour de ce modèle, découpé data → logique → UI (1 PR/phase).

**Décisions.**
1. **Contremaître = PNJ manager spécial unique**, spawné au démarrage, **ne déserte jamais** (ancre de gestion) → flag `NpcNeeds.CanDesert=false`.
2. **Toutes les interactions passent par lui** : `NpcInteractable` retiré des villageois, porté par le seul contremaître. Les bulles de besoin au-dessus des têtes restent (lecture passive).
3. **`NpcJob`** (enum : SansEmploi/Bûcheron/Mineur/Constructeur/Contremaître) sur `NpcController` + `SetJob`. Assignation via DEBUG en ph.1, via le panneau du contremaître en ph.3.
4. **Spawn** : `NpcSpawner` crée 1 contremaître (`_foremanPrefab`) puis N villageois (tirage sans remise inchangé).
5. **Découpage** : ph.1 data + spawn + interaction réservée (livré) ; ph.2 comportements de métier (bûcheron/mineur/constructeur via `WorkingState` + `WorkSpeedMultiplier` + dépôt au coffre le plus proche) ; ph.3 panneau de gestion du contremaître (roster + assignation en jeu), ferme #14.

**Alternatives écartées.** Contremaître = villageois lambda pouvant déserter (risque de perdre le hub) ; contremaître qui travaille aussi (complexité de comportement) ; garder l'examen individuel de chaque villageois en plus (redondant avec le hub) ; assignation directe PNJ par PNJ sans contremaître (contraire au modèle de gestion indirecte validé).

**Conséquences.**
- **#14 re-scopé** (issue GitHub mise à jour). `WorkSpeedMultiplier` (#13) et `Building.FindNearest` consommés en ph.2 ; `ConstructionSite.Deposit` reste le crochet du constructeur.
- **E examine le contremaître** → convergence avec le futur dialogue (Sprint 3+) à gérer comme noté en #13.
- Reportés hors POC : cuisinier (économie nourriture), « max PNJ par bâtiment de travail », « skill » individuel (efficacité = moral).

### 2026-05-31 — Besoins des PNJ : faim/abri/moral, comportement, UI (Sprint 3, issue #13)

**Contexte.** 2ᵉ brique du Sprint 3 (après #12) : doter les PNJ de besoins qui pilotent leur comportement et leur productivité. Découpé en data → comportement → UI, 1 PR par phase (#53, #54, #55).

**Décisions.**
1. **`NpcNeeds`** : 3 jauges Faim/Abri/Moral en [0..1]. Faim décroît ; Moral converge vers une cible ; multiplicateur de productivité dérivé du moral (exposé pour #14). Tuning sur **NPCData** (section Besoins, équilibrage Pascal).
2. **Scope arbitré (Aless)** vu les dépendances non encore livrées :
   - **Abri** : jauge modélisée mais satisfaction (lit assigné) **différée à #19** → stub neutre + `SetSheltered()`.
   - **Faim critique** : pas de HP PNJ (système de vie = Sprint 4) → la faim basse effondre le moral et ralentit ; **mort littérale différée**. La désertion (moral 0) reste implémentée.
   - **Repas** : au **feu de camp le plus proche** (bâtiment `EmitsLight`), **sans item nourriture** (économie plus tard).
3. **Moral cible multiplicatif** (socle survie `faim+abri` **×** qualité de travail **+** événements), et non additif : indispensable pour que des besoins au plus bas puissent réellement effondrer le moral jusqu'à la désertion (l'additif créait un plancher ~0.4 → désertion inatteignable). Crochets `SetWorkQuality` (#14), `ApplyMoraleEvent`.
4. **Comportement = interruptions globales dans `NpcController`**, par priorité **fuite > désertion > faim** (les états ne testent pas eux-mêmes ; même pattern que l'interruption de fuite de #12). `EatingState` va manger au feu le plus proche puis repart ; `DesertingState` = éloignement puis despawn (terminal).
5. **Registres statiques** `Building.All` et `NpcController.All` + `FindNearest(from, filtre)` pour les requêtes spatiales des PNJ — alternative à `FindObjectsOfType` (proscrit). Réutilisables en #14 (chantier/coffre le plus proche).
6. **UI auto-construite en code** (lazy singletons façon `InteractionPrompt`, zéro setup scène) : `NpcStatusOverlay` (bulles « Faim »/« Moral bas » en **overlay écran projeté** via `WorldToScreenPoint`) et `NpcDetailPanel` (nom + 3 jauges, barres en `RawImage`+`whiteTexture`).
7. **Examen PNJ via E / `IInteractable`** (réutilise `PlayerInteractor`, pas d'interacteur dédié). Choix Aless ; E étant la future touche de **dialogue (Sprint 3+)**, on fusionnera (menu Examiner/Parler) ou on basculera l'examen à la convergence.

**Alternatives écartées.** HP PNJ + mort par faim dès maintenant (refait au combat Sprint 4) ; abri par proximité placeholder ou assignation de lit anticipée (#19) ; item nourriture consommé (hors scope) ; Canvas world-space par PNJ pour les bulles (overhead, le projet privilégie l'overlay) ; interacteur dédié ou touche dédiée pour le panneau ; formule de moral additive (plancher artificiel).

**Conséquences.**
- **#13 clos.** Sprint 3 → **#14** (métiers : `WorkSpeedMultiplier` prêt à consommer, `Building.FindNearest` pour chantier/coffre, `ConstructionSite.Deposit` crochet du bâtisseur) puis **#15** (routines via `SleepingState` + cycle jour/nuit, recrutement, désertion).
- Crochets prêts : `SetSheltered` (#19), `SetWorkQuality` (#14).
- Patterns dégagés : **registre statique + `FindNearest`** réutilisable ; **UI utilitaire auto-construite** étendue aux bulles/panneau ; **interruptions globales priorisées** dans la machine à états.

### 2026-05-31 — Visuel d'équipement : outil en main + grip + icônes générées (anticipation Sprint 5 Polish)

**Contexte.** Travaillé **en parallèle du Sprint 3** (clone `repositories-bis`) pour anticiper le Polish du Sprint 5 sans bloquer l'implémentation des features. Objectif : *voir* l'équipement — l'outil équipé apparaît dans la main de l'avatar, la main se ferme dessus, l'anim de récolte correspond à l'outil, et la hotbar/l'inventaire affichent de vraies icônes. Aucun mesh d'outil n'existait → import d'un pack d'outils (hache/pioche) côté Unity ; **tout le code est pack-agnostique**.

**Décisions.**
1. **Visuel en main = `PlayerToolHolder` (satellite sur `_Player`)** abonné à `PlayerEquipment.OnCurrentToolChanged`. Il instancie le `HeldPrefab` du `ToolData` courant, parenté à l'os de la main droite via `Animator.GetBoneTransform(HumanBodyBones.RightHand)` — **pas de nom de bone codé en dur** (robuste au swap de perso, cf. retargeting Humanoid 2026-05-22). Pose de prise réglée à l'inspecteur ; physique du prefab neutralisée (colliders off, rigidbodies kinematic).
2. **Champs « held » sur `ToolData`** (`HeldPrefab` + `GripLocalPosition/Euler/Scale`), pas sur la base `ItemData` (les ressources n'ont pas de prise ; `WeaponData` héritera du pattern plus tard).
3. **Main fermée = layer Animator override masqué sur la main droite** (`RightHandGrip` + `RightHandMask`), poids piloté par code (`SetLayerWeight` 1/0 selon qu'un outil est tenu), pose figée d'un clip à main fermée (state Speed 0). Ferme la main quelles que soient les anims de corps, sans toucher la locomotion. **Piège** : un mask non limité à la main droite (ou layer sans mask) → override du buste → perso penché en avant.
4. **Anim de récolte par type d'outil** : paramètre Animator `harvestType` = `(int)ToolData.ToolType`, renseigné par `PlayerVisualAnimator` au changement d'outil ; transitions `Any State → Chop` (Equals 1 = hache) / `→ Mine` (Equals 2 = pioche). Clips Mixamo dédiés (Axe Swing, Pickaxe Bash). **Couplage assumé** : les valeurs de l'enum `ToolType` sont load-bearing pour les transitions (ne pas réordonner).
5. **Icônes générées par snapshot 3D** : `EquipmentIconGenerator` (Editor, menu `Survain/Items/Generate Equipment Icons`) rend le `HeldPrefab` via `AssetPreview` (compatible URP, fond transparent), écrit un PNG **committé** sous `Assets/Art/Icons/`, l'importe en **Sprite *Single*** (le mode *Multiple* par défaut n'expose aucun sous-asset → `LoadAssetAtPath<Sprite>` null → icône jamais assignée : bug clé corrigé), et l'assigne à `ItemData.Icon`. Une fois générées, les icônes ne dépendent plus du pack (gitignoré) ; `InventorySlotView` les consomme déjà.

**Alternatives écartées.** Mesh en main via primitive placeholder (préféré un vrai pack) ; `HeldPrefab` sur `ItemData` base (inutile aux ressources) ; main fermée par pose full-body (penche le corps) ou curl procédural des doigts (fragile) ; icônes en fallback couleur+texte (voulu de vraies icônes) ou rendu caméra+RenderTexture maison (`AssetPreview` suffit et gère URP/transparence) ; un Override Controller par outil pour l'anim (un `harvestType` suffit).

**Conséquences.**
- **Patterns dégagés** : *satellite piloté par event + attache sur bone Humanoid* (réutilisable arme/bouclier/torche) ; *layer override masqué piloté par `SetLayerWeight`* (autres poses partielles) ; *génération d'icône depuis le visuel 3D* (tout item ayant un prefab).
- `ToolData` gagne `HeldPrefab` + pose de prise ; `PlayerVisualAnimator` gagne `harvestType` ; `PlayerAvatar.controller` gagne états `Chop`/`Mine`, layer `RightHandGrip`, param `harvestType`.
- **Conflit de scène au rebase** sur Sprint 3 : collision de fileID entre `PlayerToolHolder` et le `PlayerInteractor` de #10 sur `_Player` (les deux branches ont ajouté un composant à `_Player`). Résolu en gardant la scène amont → **`PlayerToolHolder` est à re-attacher dans Unity sur `_Player`** après ce merge (Unity lui donnera un fileID neuf ; ses champs s'auto-résolvent). Merge-driver `UnityYAMLMerge` configuré localement pour les futurs merges de scène.
- Les `.asset` outils référencent le `HeldPrefab` du pack (gitignoré) → ref manquante sur clone neuf ; les **icônes PNG committées** restent indépendantes du pack.

### 2026-05-31 — IA des PNJ : state machine, avatar, perception (Sprint 3, issue #12)

**Contexte.** Première brique du Sprint 3 : doter les PNJ d'une IA fondamentale extensible. Découpé comme #6/#7 : phase 1 (locomotion NavMesh), phase 2a (avatar visuel), phase 2b (états étendus + perception). 1 PR par phase (#50, #51, #52).

**Décisions.**
1. **Machine à états polymorphe** (`INpcState` + une classe par état : Idle/Wander/Working/Eating/Sleeping/Fleeing). Chaque état isole Enter/Tick/Exit → extensible sans toucher au `NpcController`. `ChangeState` orchestre Exit→Enter.
2. **NavMesh baké au runtime** (`NavMeshRuntimeBaker`, ordre +150) : le terrain est généré au Play (mesh non versionné), impossible de baker en éditeur. Spawner en +200 (après bake). `NavMeshObstacle` (carving) sur les `ResourceNode` pour le contournement (désactivé à la récolte, réactivé au respawn).
3. **Avatar = réemploi du setup #33** (Synty Sidekick + anims Mixamo). **`NpcAvatar.controller` dédié** (copie de `PlayerAvatar.controller`) : seuil Walking du blend tree recalé sur la vitesse d'errance des PNJ (sinon marche molle/glissée), seuil Running calé sur `FleeSpeed`. Chaque avatar Synty a son propre `-avatar.asset` humanoïde (pas de rig partagé). `NavMeshAgent` = autorité position (Apply Root Motion off).
4. **Variété des villageois = liste de prefabs + tirage sans remise** (Fisher-Yates dans `NpcSpawner`) : villageois tous distincts tant qu'il y a assez de modèles, recyclage au-delà. 3 prefabs `NPC_Villager_02/03/04` (Starter_02/03/04). Pas de swap d'avatar au runtime (config self-contained par prefab).
5. **Perception = interruption globale, pas par état** : `NpcPerception` (OverlapSphere throttlé 0.2 s sur un `LayerMask` de menaces, hystérésis 1.25× anti-flicker) ; `NpcController.Update` force `FleeingState` dès qu'une menace est perçue, quel que soit l'état. Les états ne testent pas la menace. Layer `Threat` créé (vide au POC ; ennemis Sprint 4).
6. **`FleeingState` = seul état étendu réellement piloté en #12** ; Working/Eating/Sleeping sont des **squelettes** branchés en #13 (faim→Eating), #14 (métier→Working), #15 (nuit→Sleeping). `[ContextMenu]` DEBUG sur `NpcController` pour valider anim/transitions sans ces déclencheurs.
7. **Anim de travail isolée** : `NpcWork.fbx` (copie du Punching, bouclée) plutôt que de modifier l'import du Punching one-shot du joueur. `NpcAnimParams` centralise les hashes (`speed`, `isWorking`).

**Alternatives écartées.** Réemploi de `PlayerAvatar.controller` (polluerait le controller joueur) ; un seul avatar pour tous (village de clones) ; swap d'avatar au runtime (fragile vs prefabs self-contained) ; tirage avec remise (doublons) ; perception testée dans chaque état (duplication — l'interruption globale est plus simple) ; boucler le clip Punching partagé (casserait le one-shot joueur).

**Conséquences.**
- **#12 clos.** Sprint 3 continue sur #13 (besoins), #14 (métiers — `ConstructionSite.Deposit` est le crochet du PNJ bâtisseur), #15 (routines + recrutement).
- Patterns dégagés : **interruption globale** au niveau machine pour les transitions prioritaires (réutilisable combat/dialogue) ; `NpcPerception` réutilisable ; `NpcAvatar.controller` dédié évolutif ; **bake NavMesh runtime** pour tout monde généré au Play.
- Layer `Threat` prêt pour les ennemis du Sprint 4 (il suffira de les y placer → fuite des PNJ automatique).
- Squelettes Eating/Sleeping/Working prêts à recevoir leurs déclencheurs (#13/#14/#15) sans refonte de la machine.

### 2026-05-30 — Bâtiments fonctionnels + destruction/réparation (Sprint 2, issues #10 & #11)

**Contexte.** Suite du modèle chantier (#9). #10 = rendre les bâtiments *utiles* (coffre, feu de camp, atelier) + leur donner des HP ; #11 = les détruire et les réparer. Découpé en phases (data→UI, puis dégâts→réparation) comme #6/#7. Le combat n'existant pas encore (Sprint 4), la **source de dégâts du POC est le clic gauche** sur un bâtiment.

**Décisions.**
1. **Interaction générique `IInteractable` + `PlayerInteractor`** (touche E). Un seul interacteur vise par raycast caméra n'importe quel `IInteractable` et appelle son `Interact` : dépôt dans un chantier (`ConstructionSite`), ouverture d'un coffre (`StorageContainer`). Remplace `PlayerConstructionInteractor`. Évite la multiplication d'interacteurs concurrents sur la même touche. Pattern à réutiliser (PNJ dialogue Sprint 3, portes…).
2. **Coffre de stockage** = `StorageContainer` sur le bâtiment + `Inventory` secondaire **créé en runtime** (`Inventory.ConfigureCapacity`, capacité depuis `BuildingData.StorageCapacity`). Persistant en session. À la destruction, `SpillContents()` déverse le contenu au sol (pas de perte silencieuse).
3. **UI coffre** = `ContainerUI` (singleton) : panneau de slots **clonés depuis un template** au runtime, bindés à l'`Inventory` du coffre, + ouverture simultanée du backpack (`InventoryUI.SetOpen`). Le **drag inventaire ↔ coffre** réutilise tel quel `InventoryDragController`/`SwapAcross` de #7 (zéro code de transfert spécifique). Layout empilé (coffre haut / inventaire bas).
4. **Feu de camp** = `BuildingLight` (point light + scintillement Perlin) créé en code à la complétion si `BuildingData.EmitsLight`. Toujours allumé — le gating jour/nuit viendra au Sprint 5.
5. **HP & destruction** : `Building` porte HP + `TakeDamage`/`Repair`/`OnHpChanged`. Démolition = **clic gauche** via `PlayerBuildingTool` (raycast caméra, cooldown), qui **coexiste avec `PlayerHarvester`** sur la touche `Attack` — mutuellement exclusifs par le type de cible (nœud vs bâtiment), aucun double-effet. À 0 HP : remboursement partiel (`RefundRatio`) + déversement coffre + burst de particules **détaché** (`BuildingDestructionFx`, survit au `Destroy`, pattern `ResourceNodeJuice`).
6. **Dégradation visuelle** = teinte progressive du matériau selon les HP (couleur d'origine → sombre), via le clone `Renderer.material` partagé avec la surbrillance. Vrais états de mesh = avec les prefabs (#46).
7. **Réparation** = action **R** (`PlayerBuildingTool`) : consomme des ressources **proportionnelles aux dégâts** (coût construction × fraction × `_repairCostFactor`) depuis le sac, restaure les HP. Bindée sur **R** (pas E) pour ne pas entrer en conflit avec l'ouverture des coffres.
8. **Prompt « last-writer-wins » contourné** : chaque système n'affiche le prompt que pour *sa* cible et ne le masque que s'il l'affichait lui-même (`_showingPrompt`). Sur un coffre, `PlayerInteractor` possède le prompt (« [E] Ouvrir ») et `PlayerBuildingTool` se tait (la démolition y reste fonctionnelle).
9. **Pattern « champ fonctionnel sur `BuildingData` »** : `StorageCapacity`, `EmitsLight`(+params), `MaxHp`, `RefundRatio`. Le bon comportement est ajouté en code à la complétion selon ces champs. Suffisant au POC ; généralisable en « modules » si la liste grossit.

**Alternatives écartées.** Plusieurs interacteurs E concurrents (un par type) ; réparation via E (conflit coffre) ; barre de vie flottante world-space (overhead UI/orientation par bâtiment, reportée) ; démolition instantanée via interaction (ne testerait ni dégradation ni réparation sans source de dégâts) ; panneau coffre avec ses propres slots backpack (re-setup lourd — on réutilise `InventoryUI`).

**Conséquences.**
- **#10 et #11 clos.** Reste **#46** (vrais prefabs Synty, gated arbitrage pack Pascal) : il suffira de brancher `BuildingData.Prefab`, sans toucher au code.
- **Sprint 4 (combat)** alimentera `Building.TakeDamage` comme nouvelle source de dégâts ; la dégradation/réparation est déjà prête.
- Ratio de remboursement (50%) et facteur de réparation (1) = placeholders → **décisions en attente** Pascal (économie fermée).
- `IInteractable`/`PlayerInteractor` et `ContainerUI` (réemploi du drag #7) sont les briques pour les futurs conteneurs/interactions (marchand, dépôt de village pour PNJ bâtisseurs).

### 2026-05-30 — Granularité de construction : on reste NON-MODULAIRE (bâtiments entiers)

**Contexte.** En explorant l'import d'un asset visuel pour les bâtiments (issue #10, prefabs), la question de fond a resurgi : faut-il basculer vers de la construction **modulaire pièce-par-pièce** (le joueur compose ses structures à partir de murs/sols/toits avec snap par sockets, façon Valheim/Rust), qui offre plus de liberté ? L'analyse a dégagé que « modulaire vs entier » est un **faux binaire** : il y a en réalité deux axes indépendants — la **granularité de pose** (décor pré-fait → bloc fini → pièces à assembler) et le **modèle d'interaction** (externe : on s'approche + UI ; interne : on entre dans le bâtiment). Notamment, « grand bâtiment où l'on entre » (le marché, une halle à PNJ) **n'implique pas** « construit pièce par pièce » : c'est un **bloc fini avec un intérieur creux**, posé en un coup.

**Taxonomie dégagée (4 niveaux).**
- **T0 — Décor** : maisons lointaines, ruines, monuments de la Capitale. Posé par le worldgen / level design (prefab fini). Aucun système de construction.
- **T1 — Petit fonctionnel** : forge, coffre, feu de camp, établi. Le joueur pose **un bloc fini** ; interaction **externe** (E → UI). **= le système chantier actuel, déjà livré (#9).**
- **T2 — Grand habitable** : marché, longhouse à PNJ. Le joueur pose (ou pré-fab) **un bloc fini** ; interaction **interne** (on entre physiquement, on parle aux PNJ). Pose identique à T1 ; la différence est dans le **contenu du prefab** (intérieur + porte traversable, fade du toit éventuel) et les systèmes de jeu à l'intérieur (habitation PNJ = Sprint 3).
- **T3 — Modulaire** : abri/maison/fort **composé par le joueur** à partir de pièces, avec snap par sockets. **Le seul niveau** qui exige le sous-système de sockets.

**Décision.** **On reste sur du NON-MODULAIRE** : le POC (et le jeu tant que rien ne le remet en cause) ne livre que **T0, T1 et T2**, tous sur le système de pose de **bâtiments entiers existant**. **T3 (construction pièce-par-pièce) est abandonné pour le POC** — non développé, pas de système de sockets. Décision prise par Aless, cohérente avec la vision « assisté / city-builder, pas un bac-à-sable Minecraft » (cf. journal 2026-05-29) et avec le fait que la construction n'est pas un pilier non-négociable.

**Raisons.**
1. **3 niveaux sur 4 ne nécessitent aucun système modulaire.** T0 = art d'environnement, T1 = déjà fait, T2 = système actuel + prefab à intérieur. Seul T3 coûterait le sous-système de sockets (~un sprint : snap-points, placement vertical, refonte de la validation collision).
2. **Cohérence vision.** SURVAIN vise une expérience assistée façon city-builder ; le marché central est une structure **fixe/pré-faite à la Capitale** (pilier économie fermée), pas un objet que le joueur brique lui-même. La forge est un T1 classique.
3. **Précédents du genre.** Beaucoup de jeux survie-village (Going Medieval, Foundation, Manor Lords) ne font que du placement de blocs finis, jamais du brique-à-brique, sans nuire à la profondeur.
4. **Coût/risque maîtrisé.** Le sous-système de sockets est le seul vrai morceau d'ingénierie nouveau ; l'éviter garde le scope de construction borné et le système actuel (générique sur `BuildingData`) suffit.

**Alternatives écartées.** Migration complète vers le modulaire pièce-par-pièce (T3) : plus de liberté joueur mais c'est un **choix de game design structurant** (sandbox de construction) non aligné sur la vision assistée, et le seul qui impose le sous-système de sockets — disproportionné pour le POC. Approche hybride (T3 optionnel activable plus tard) : la porte n'est pas définitivement fermée côté données (`BuildCategory` garde les familles de pièces), mais **aucun code T3 n'est écrit** tant que le sandbox de construction n'est pas validé comme pilier par Pascal.

**Conséquences.**
- **#10 reste cadré sur des bâtiments entiers** (T1/T2) : catalogue de structures finies + bâtiments fonctionnels (coffre, forge…), prefabs Synty branchés via le champ `BuildingData.Prefab`. Pas de pièces modulaires à assembler.
- Le **système de pose actuel** (`BuildModeController` + `ConstructionSite` + `Building`), déjà générique sur `BuildingData`, **suffit pour T0/T1/T2 sans refonte**. Pas de système de sockets à développer.
- **T2 « enterable »** : à traiter quand on l'abordera comme du **contenu de prefab** (intérieur creux, porte traversable, éventuel fade du toit via volume trigger) + l'habitation PNJ (Sprint 3) — **pas** comme une évolution du système de construction.
- Affinage data possible plus tard (non requis maintenant) : un champ `InteractionMode { External, Enterable }` sur `BuildingData` pour distinguer T1/T2 proprement.
- Choix de **pack visuel** (cf. exploration du jour) : un pack Synty cohérent — Vikings Pack (~30 $) couvre T0/T1/T2 ; reste un **arbitrage budget + direction artistique pour Pascal**, indépendant de cette décision de granularité.

### 2026-05-29 — Système de construction : modèle chantier (Sprint 2, issue #9)

**Contexte.** Démarrage du Sprint 2. Première implémentation en **placement libre pièce-par-pièce** (grille 1 m, fantôme vert/rouge, snap, rotation 90°, validation pente/collision/ressources). Jugée trop fastidieuse en test et contraire à la vision (« assisté », city-builder, pas un bac-à-sable Minecraft). Pivot en cours de session vers un modèle **chantier** (blueprint + dépôt de ressources), tranché par Aless (la construction n'est pas un pilier non-négociable ; validation Pascal a posteriori — cf. Décisions en attente). Le code de placement a été réutilisé quasi tel quel.

**Décisions.**
1. **Modèle « chantier ».** Poser une structure ne consomme/n'exige **pas** de ressources : ça crée un `ConstructionSite` (fantôme bleu→vert qui réserve l'emplacement). Le joueur dépose ensuite les ressources (touche E, visée caméra) jusqu'à complétion → le bâtiment fini apparaît. Choisi pour l'UX assistée **et** parce que c'est le crochet direct des PNJ bâtisseurs du Sprint 3 : `ConstructionSite.Deposit(Inventory)` sera appelable depuis un stock de village. La pose instantanée (consommation immédiate) est un cas dégénéré du chantier → aucune porte fermée.
2. **Bâtiments entiers** au catalogue (hutte, abri de stockage), pas de micro-pièces. 1 bâtiment = 1 chantier = 1 prefab. L'idée « demi-bâtiments à accoler » a été écartée (snap fin inutile).
3. **Namespace `Survain.Gameplay.Buildings`** (pluriel — type cardinal `Building` éponyme, cf. convention `Inventories`). Dossier miroir `Assets/Scripts/Gameplay/Buildings/`.
4. **`BuildModeController` owner du mode** ; expose `IsActive`. **Exclusion mutuelle** du clic gauche (récolte) et de la molette (zoom caméra) via le pattern « flag lu, pas d'appel mutuel » : `PlayerHarvester` suspend la récolte et `PlayerCameraRig` neutralise le zoom tant que `IsActive`.
5. **Input** : `ToggleBuildMode` (B), `RotateBuild` (Q/E, 1D axis), `CancelBuild` (clic droit + Échap), molette = cycle de structure (zoom gaté), `[ / ]` en alternative clavier (la molette est le moyen principal — `[ / ]` sont introuvables en AZERTY). Action `Interact` ajoutée sur **E** = touche d'interaction générique réservée (cf. 2026-05-22 §2), utilisée ici pour le dépôt dans un chantier.
6. **Visuels code-only.** `BuildingVisualFactory` génère un cube URP/Lit dimensionné par `BuildingData.Size` (placeholder avant les vrais prefabs modulaires de #10). Ghost de placement et blueprint de chantier en URP **transparent** appliqué via `Renderer.material` (jamais `sharedMaterial`). Grille au sol générée en code (`BuildGridVisual` : quad + texture de grille tuilée).
7. **Snap = grille 1 m** (alignement naturel des bâtiments voisins) ; pas de snap par sockets (reporté post-POC).
8. **Données.** `BuildingData` enrichi (catégorie, `Size`, `BuildCost[]`, prefab optionnel). `BuildCost` = struct sérialisable top-level (même raison qu'`InventorySlot`). `BuildCategory` enum. Bootstrap Editor idempotent qui crée les bâtiments et les ajoute au Registry **sans écraser** les items existants.
9. **Respawn cède aux constructions.** Un nœud épuisé a son collider désactivé → invisible au test de collision du placement, donc on pouvait bâtir sur un arbre coupé qui repoussait ensuite à travers le bâtiment. `ResourceNode.RespawnAfterDelay` teste désormais l'occupation (`Building`/`ConstructionSite` via `OverlapBox`) et **diffère** la réapparition tant qu'une structure occupe le lieu (re-check 5 s ; retour possible si la structure est détruite #11). Couplage assumé `Gameplay.Items → Gameplay.Buildings` (même assembly, pas de cycle).

**Alternatives écartées.** Placement libre pièce-par-pièce (fastidieux, contre la vision) ; pose instantanée avec consommation immédiate (aucune prise pour les PNJ bâtisseurs Sprint 3) ; assemblage de demi-bâtiments (snap fin disproportionné) ; molette de cycle sans gating du zoom (zoom + cycle simultanés) ; `[ / ]` seuls (inaccessibles en AZERTY) ; bloquer la pose sur les nœuds épuisés (les colliders désactivés échappent à `OverlapBox` → mécanisme de détection séparé plus lourd, le respawn-cède couvre le cas).

**Conséquences.**
- `ConstructionSite` devient le **point d'extension officiel des PNJ bâtisseurs** (Sprint 3, #14) : un métier « bâtisseur » alimentera `Deposit` depuis un stock de village.
- **#9 re-scopé** : les critères « snap entre pièces » / « rotation de murs » disparaissent au profit du modèle chantier. **#10 devient** « catalogue de bâtiments entiers + bâtiments fonctionnels (coffre, feu de camp) » plutôt que pièces modulaires ; l'atelier reste une coque (fonction craft après #8) ; le lit/point de respawn est déplacé vers #19 (Sprint 4).
- Pattern **gating par `IsActive`** (flag lu par les consommateurs, pas d'appel mutuel) à répliquer pour les futurs modes exclusifs (combat, dialogue).
- Les visuels placeholder code-only sont à **remplacer par de vrais prefabs en #10** ; il suffira de brancher le champ `Prefab` de chaque `BuildingData`, sans toucher au système.

### 2026-05-22 — Polish input : clic gauche récolte, F pickup manuel, surbrillance nœuds + items (Sprint 5 anticipé, issue #40 close)

**Contexte.** Issue #40 attaquée en avance (planifiée Sprint 5) pendant que le craft #8 reste bloqué sur arbitrage Pascal. Refonte des interactions monde pour tendre vers le standard FPS/survie : clic gauche pour frapper/récolter, F pour ramasser, surbrillance émissive pour signaler ce qui est interactable. Rentable immédiatement (gros confort UX) sans dépendance scope critique.

**Décisions.**
1. **Action `Interact` (touche E) renommée `Attack` (clic gauche)**. Sémantique cohérente : `Attack` couvre à la fois la frappe d'un nœud (Sprint 1) et le combat avec un ennemi (Sprint 4) — c'est la même action gameplay « frapper avec l'outil/arme équipé ». Quand le dialogue PNJ arrivera (Sprint 3), on créera un `Interact` ou `Talk` dédié sur une autre touche, sans conflit avec `Attack`.
2. **Nouvelle action `PickupItem` bindée sur `F`** (un seul binding, pas E+F comme initialement envisagé). Le choix de Aless : E sera réservé aux futures interactions génériques (parler, ouvrir, lire). F dédié au pickup pour pas avoir à le partager.
3. **`InventoryPickupZone` refondu en mode manuel** : `_autoPickup = false` par défaut, lit l'action `PickupItem`, maintient un `HashSet<WorldItem>` des items en zone, et affiche le prompt `"[F] Ramasser X"` pour l'item le plus proche. Le toggle `_autoPickup` reste en option pour bascule rapide vers le mode auto (legacy phase 1 de #7).
4. **Décision « pickup auto vs manuel » résolue en faveur du manuel.** Aless avait assumé la responsabilité du choix auto en phase 1 de #7 ; ici il l'arbitre vers manuel + F. Cohérent avec la convention FPS/survie. La décision est retirée de la section « Décisions en attente ».
5. **Surbrillance émissive (`_EmissionColor` URP Lit)** sur deux cibles :
   - `WorldItem` (items au sol) : activée à l'entrée dans la zone de pickup, désactivée à la sortie. Comme le material est créé en code (cf. décision 2026-05-22 phase 1 §7), on peut le modifier directement sans cloner.
   - `ResourceNode` (arbres, roches) : activée au survol via le raycast caméra de `PlayerHarvester`, désactivée au changement de cible. Les prefabs Synty ont leur sharedMaterial partagé entre toutes les instances, donc on utilise `Renderer.material` (clone auto au premier accès) au lieu de `sharedMaterial` pour ne pas corrompre les autres nœuds.
6. **Pattern `Renderer.material` (clone auto) au lieu de `MaterialPropertyBlock`** pour ResourceNode. Le MPB avait du sens initialement (pas de copie GPU) mais il ne supporte pas l'activation de keywords — et activer `_EMISSION` sur un sharedMaterial Synty produit un bug catastrophique : tous les prefabs partageant ce material deviennent blancs au prochain Play (la nouvelle variant du shader cherche une `_EmissionMap` absente). `Renderer.material` clone à la volée, instance par renderer, zéro corruption. Coût mémoire négligeable (1-3 materials par nœud × ~50-100 nœuds = <1 MB).
7. **Bug `Destroy(gameObject)` différé** : Unity ne détruit pas immédiatement l'objet, il le marque pour destruction et nettoie à la fin de la frame. Après pickup, le `HashSet<WorldItem>` contenait un WorldItem « fantôme » techniquement non-null, ce qui maintenait le prompt visible jusqu'à un second appui F. Fix : filtrer le `RemoveWhere` sur `item == null OR item.Quantity <= 0` (l'item est considéré consommé même si pas encore détruit).
8. **Prompts UI** : `PlayerHarvester` affiche `"[Clic gauche] Récolter X"` au survol d'un nœud ; `InventoryPickupZone` affiche `"[F] Ramasser X"` quand un item est en zone. Pas de gestion de priorité — si les deux conditions sont vraies simultanément, le dernier qui écrit dans `InteractionPrompt` gagne. Acceptable POC ; à raffiner si conflit récurrent observé.

**Alternatives écartées.**
- **Keyword `_EMISSION` activé au Awake sur sharedMaterial** : tentative initiale qui corrompt tous les prefabs au démarrage (rendu blanc). Persiste même après Stop/Play car Unity Editor cache les modifs aux assets.
- **Lazy keyword activation au premier survol** : améliorait le bug initial mais le corrompait au premier survol (tous les prefabs deviennent blancs et restent corrompus).
- **MaterialPropertyBlock pur (sans toucher au keyword)** : marche en théorie si les materials Synty ont déjà `_EMISSION` activé, mais cas non garanti. `Renderer.material` est plus safe.
- **Pickup via raycast caméra (comme la récolte)** : alternative au trigger sphérique. Plus précis mais demande un visement, peu pratique pour des items au sol qui peuvent être petits. Le trigger sphérique 1.5m reste plus ergonomique pour un POC.
- **Outline shader custom (Renderer Feature URP)** : alternative à l'émissive pour la surbrillance. Plus joli mais demande de coder un shader custom et de configurer une Renderer Feature dans Project Settings. Disproportionné pour POC, à reconsidérer si Pascal trouve l'émissive trop subtile.
- **Prompt prioritarisé (PlayerHarvester > PickupZone ou inverse)** : reporté tant qu'on n'observe pas le conflit en jeu.

**Conséquences.**
- **Issue #40 close** — refonte input livrée en avance de phase (initialement Sprint 5 polish).
- L'API publique `WorldItem.SetHighlighted(bool)` et `ResourceNode.SetHighlighted(bool)` est posée. **Pattern dégagé** : pour highlighter un objet de jeu, on expose une méthode `SetHighlighted(bool)` qui modifie l'émissive via `Renderer.material` (clone auto). À répliquer pour les futurs objets interactables (PNJ ciblables Sprint 3, bâtiments construisibles Sprint 2, ennemis Sprint 4).
- Convention **`Renderer.material` (et non `sharedMaterial`)** acquise pour toute modification runtime d'un material partagé. À signaler en revue de code si du `.sharedMaterial = ...` ou `EnableKeyword(...)` sur sharedMaterial apparaît.
- Convention **`Destroy()` différé** : filtrer sur l'état logique (`Quantity`, `IsDepleted`, etc.) plutôt que sur `item == null` quand on parcourt une collection qui peut contenir des objets en attente de destruction. Pattern à répliquer pour les futurs systèmes qui maintiennent des listes d'entités potentiellement destroyed.
- L'`InputActionAsset` perd `Interact`, gagne `Attack` (clic gauche) et `PickupItem` (F). `PlayerHarvester` et `InventoryPickupZone` sont les seuls consommateurs ; mis à jour en même temps. Pas de breaking change côté autres systèmes.
- Sprint 1 reste à `#8` (craft, en attente Pascal). Le reste du polish est en place avant le craft (récolte propre, pickup propre, inventaire complet, drag & drop).

---

### 2026-05-22 — Inventaire joueur phase 3/3 : drag & drop + drop manuel (Sprint 1, issue #7 close)

**Contexte.** Phase 3 et dernière de l'issue #7 : ajouter la couche "modifier l'état via interaction UI" par-dessus la couche "voir l'état" de la phase 2. Le joueur peut maintenant drag des items entre slots du backpack, entre backpack et hotbar, et drop des items au monde depuis l'inventaire. Issue #7 close.

**Décisions.**
1. **Drag & drop natif uGUI** via `IBeginDragHandler` / `IDragHandler` / `IEndDragHandler` / `IDropHandler` sur `InventorySlotView`. Pas de plugin, pas de système custom. Cohérent avec le choix uGUI Canvas (cf. phases 1 §4 et 2 §2).
2. **`InventoryDragController` singleton** posé sur le Canvas `UI`. Pilote le ghost visuel (suit la souris pendant le drag) + l'état du drag courant (source slot, item dragué, `_droppedOnSlot` flag). Au `EndDrag` sans `OnDrop` → drop hors UI → spawn `WorldItem` près du joueur. Pattern singleton + référence locale (drag de la racine UI) pour ne pas dépendre de `FindObjectOfType`.
3. **`Inventory.SwapAcross(int, Inventory, int)` ajouté** pour le swap inter-conteneur (backpack ↔ hotbar). Pas de validation MaxStackSize côté destination (les slots échangés ont été créés par `TryAdd` qui respecte déjà la contrainte). Si source == destination, délègue à `Swap` classique. L'API publique `Inventory` passe à 9 méthodes (`TryAdd`/`TryRemove`/`Get`/`Count`/`Contains`/`Swap`/`SwapAcross`/`Transfer` + `OnSlotChanged`/`OnInventoryChanged`).
4. **`WorldItemSpawner` static helper** introduit pour factoriser la création d'un `WorldItem` au monde. Utilisé par `ResourceNode.SpawnDrop` (fallback sans prefab) ET par `InventoryDragController.EndDrag` (drop manuel). Évite la duplication "new GameObject + AddComponent<WorldItem> + Configure".
5. **Drop hors UI = stack entier** (pas 1 unité). Cohérent Minecraft/Terraria, plus simple côté code, et le joueur peut re-ramasser le drop si besoin. Modal "split quantity" reporté post-POC si nécessaire.
6. **Drop sur slot occupé = swap des deux slots** (et non annulation du drag). Réutilise `Swap`/`SwapAcross`. Si les deux slots contiennent le même item stackable, on aurait pu faire un merge intelligent ; ici on fait un swap simple — c'est légèrement sous-optimal mais cohérent.
7. **Transfer libre backpack ↔ hotbar** (pas de filtre type). Le joueur peut mettre un `raw-wood` dans la hotbar si ça lui chante. Standard sandbox. Si Pascal veut restreindre la hotbar aux outils plus tard, on ajoutera un prédicat `CanAccept(ItemData)` côté `Inventory`.
8. **`HotbarBootstrap` renommé `InventoryBootstrap`** (générique). La pré-injection se fait désormais dans le **Backpack** (et non plus la Hotbar) : le joueur démarre avec hache + pioche dans son sac à dos, et les drag vers la hotbar via UI. Plus naturel pédagogiquement (le joueur voit l'inventaire dès le 1er Tab). À supprimer définitivement quand le save loader sera en place.
9. **`Mouse.current.position.ReadValue()` (Input System Package)** pour récupérer la position souris dans `Update`. `UnityEngine.Input.mousePosition` plante au runtime car le projet est sur le new Input System depuis l'origine. Convention : tout accès souris/clavier passe par `Mouse.current` / `Keyboard.current`, pas par l'ancien API.
10. **Ordre Hierarchy UI critique** : `HotbarPanel` doit être listé **après** `InventoryPanel` sous le Canvas `UI` pour que les drops sur la hotbar (quand le panel inventaire est ouvert) ne soient pas interceptés par le dim noir fullscreen de l'`InventoryPanel`. Bug rencontré pendant le dev, fixé en réordonnant les enfants. **Pattern à retenir** : dans uGUI, l'ordre des enfants du Canvas définit le z-order rendu ET le z-order raycast — un `Image` fullscreen avec `Raycast Target ✅` placé après les autres éléments les rend incliquables.
11. **`CanvasGroup` sur `DragGhost` avec `Blocks Raycasts = false`** pour que le ghost ne bloque pas les events de drop alors qu'il suit la souris. Pattern standard uGUI pour les overlays dynamiques.

**Alternatives écartées.**
- **Drop 1 unité à la fois (ou split modal)** : trop POC, pattern stack-entier suffit. À ré-évaluer post-MVP si le confort le demande.
- **Slot occupé = annuler le drag** : frustrant pour l'utilisateur. Swap est plus ergonomique.
- **Filtre hotbar = outils/armes uniquement** : restreint la flexibilité, complique le code. Standard sandbox = transfer libre.
- **Garder `HotbarBootstrap` tel quel** : nom ne reflète plus la réalité (cible Backpack désormais). Rename `InventoryBootstrap` pour générique.
- **Ghost visuel piloté par `OnDrag` côté slot** au lieu d'`Update` du controller : OnDrag est appelé seulement quand la souris bouge, donc moins fluide. `Update` lit la position chaque frame, plus régulier.
- **Drop modal "voulez-vous vraiment drop ?"** : friction utilisateur inutile pour le POC.
- **Décocher `Raycast Target` sur `InventoryPanel.Image`** au lieu de réordonner la Hierarchy : marche aussi mais perd le pattern "dim noir bloque les clics sur la scène 3D quand le panel est ouvert" (utile plus tard pour les menus modaux).

**Conséquences.**
- **Issue #7 close** — 3 phases livrées en une journée. Statut `priority:critical` levé.
- L'API publique `Inventory` est stabilisée (9 méthodes + 2 events). Contrat sur lequel les futurs systèmes UI/save/multi-joueur peuvent se brancher sans risque de refactor.
- `WorldItemSpawner` devient le **point d'entrée standard pour faire apparaître un item dans le monde**. À utiliser pour : drops PNJ tués (Sprint 4), drops bâtiments détruits (Sprint 2), récompenses de quête (post-POC).
- Pattern **uGUI `IDragHandler`/`IDropHandler` + Controller singleton + ghost CanvasGroup** validé. À répliquer pour : panneau de craft (drag ingrédient → slot recette), échange avec PNJ marchand (Sprint 3), partage entre joueurs (Sprint 5 multi).
- Convention **`Mouse.current` / `Keyboard.current` (Input System Package)** acquise pour le projet. Pas de `UnityEngine.Input.*` autorisé. À signaler en revue de code.
- Convention **ordre Hierarchy UI Canvas dicte le raycast** acquise. À surveiller quand on ajoute des panels overlays (menu pause, menu craft).
- Sprint 1 : reste seulement **#8 craft** (en attente d'arbitrage Pascal sur la mécanique d'engagement non-répétitive). Une fois #8 livré → Sprint 1 clôturable → release `v0.2.0`.

---

### 2026-05-22 — Inventaire joueur phase 2/3 : UI hotbar + panel inventaire (Sprint 1, issue #7 partiel)

**Contexte.** Phase 2 de l'issue #7 : matérialiser l'inventaire data layer de la phase 1 en interface uGUI. Hotbar visible en permanence en bas d'écran (4 slots), panel inventaire ouvrable Tab/I (24 slots en grille 6×4). Phase 3 (drag & drop + drop d'items depuis l'inventaire) à enchaîner. **Validation Pascal sur le pickup auto reportée volontairement par Aless** (responsabilité assumée) — basculer en pickup manuel reste possible sans refonte si Pascal arbitre dans ce sens en phase 3.

**Décisions.**
1. **3 composants C# distincts, SRP strict.** `InventorySlotView` (vue d'un slot unique, s'abonne à `Inventory.OnSlotChanged` et filtre sur son index), `HotbarUI` (binding hotbar + matérialisation slot équipé via `PlayerEquipment.OnCurrentToolChanged`), `InventoryUI` (binding backpack + toggle panel + gestion curseur). Pattern MVVM léger : la data (`Inventory`) ignore la vue, la vue s'abonne ponctuellement. Permet de remplacer toute la couche UI plus tard (UI Toolkit, mod, console UI) sans toucher au data.
2. **uGUI Canvas (confirmé)** — décision phase 1 §4 maintenue. Cohérent avec `InteractionPrompt` (uGUI, cf. 2026-05-21 phase 3 §3). `Canvas` Screen Space Overlay + `CanvasScaler` Scale With Screen Size (1920×1080 ref, match 0.5).
3. **Pas de prefab Slot template.** Aless construit le premier slot dans la scène puis duplique (`Ctrl+D × N`) — `Horizontal Layout Group` (hotbar) et `Grid Layout Group` (backpack) gèrent le placement auto. Tradeoff : un prefab serait plus DRY pour modifications futures mais demande un workflow Unity supplémentaire ; le `Ctrl+D` reste pragmatique au POC. Si la phase 3 demande de toucher au visuel des slots, on reprefabricera à ce moment.
4. **Fallback visuel quand `ItemData.Icon` est null** (décision 2026-05-17 §8 conservée : icônes vides au POC). Le `InventorySlotView` calcule :
   - **Couleur** : `Color.HSVToRGB(hash(id) / 65535f, 0.55f, 0.8f)` — saturation/luminosité fixes pour lisibilité, teinte distribuée sur tout le cercle. Déterministe (même item = même couleur cross-session).
   - **Texte 3 lettres** : prend la partie après le dernier tiret de l'Id (`stone-axe` → "AXE", `raw-wood` → "WOO", `raw-stone` → "STO"). Première version naïve "3 premières lettres" donnait "STO"/"STO" indistinguable pour `stone-axe`/`stone-pickaxe` ; le pivot après le dernier tiret exploite la convention kebab-case `domaine-nom` où le nom est plus discriminant que le domaine.
5. **Input : touches numériques directes 1-4 pour la hotbar** (et non Previous/Next molette/flèches). Standard FPS/survie, simple à binder. Les actions `Previous`/`Next` ont été **supprimées de l'`InputActionAsset`**, remplacées par `EquipSlot1`/`EquipSlot2`/`EquipSlot3`/`EquipSlot4` (1 action par slot, bindée sur `<Keyboard>/N`). Implémentation `PlayerHarvester` : tableau de 4 `InputAction` + tableau de 4 `Action<CallbackContext>` (delegate cached pour pouvoir désabonner) avec capture de l'index dans une lambda. Pattern à répliquer si on a un jour 10+ slots.
6. **Toggle inventaire = `ToggleInventory` avec 2 bindings (Tab + I).** L'utilisateur peut utiliser l'une ou l'autre touche — pratique pour les habitudes différentes (Tab = MMO, I = Minecraft). Stocké comme 1 action dans la map `Player` avec 2 bindings sur la même action (et non 2 actions séparées). Évite le code de coalescing côté script.
7. **Curseur libéré quand panel ouvert.** `InventoryUI.SetOpen(true)` met `Cursor.lockState = None` + `Cursor.visible = true` ; `SetOpen(false)` re-locke. Décision CLAUDE.md 2026-04-26 §6 (PlayerCameraRig autorité du curseur) **assouplie** : InventoryUI prend l'autorité temporairement quand le panel est ouvert, PlayerCameraRig la reprend implicitement à la fermeture (pas d'appel mutuel — chacun écrit ce qu'il veut, le dernier écrit gagne). Convention à formaliser quand on aura plus de menus (UI menu pause, menu craft, dialogue) : créer un `CursorOwnershipStack` si nécessaire.
8. **Pas de pause du jeu pendant inventaire ouvert** (survie temps réel). Le joueur reste vulnérable. À reconsidérer pour les futurs sous-menus stratégiques (carte du royaume Sprint 4) qui demandent de figer le temps.
9. **Bug désabonnement OnDisable** (rencontré pendant le dev) : `InventorySlotView` désabonnait `OnSlotChanged` dans `OnDisable`. Au `Start` de `InventoryUI`, on fait `Bind` puis `SetActive(false)` du panel → tous les slots reçoivent OnDisable → désabonnement immédiat → les slots ne sont plus jamais notifiés. Fix : désabonner uniquement dans `OnDestroy`. Les `RefreshFromSlot` continuent à tourner sur GameObject désactivé sans souci. **Pattern à retenir** : si un composant a besoin de rester abonné en arrière-plan (panel caché mais data à jour pour le re-open), désabonner à `OnDestroy` plutôt qu'à `OnDisable`.

**Alternatives écartées.**
- **UI Toolkit (UIElements)** : déjà rejeté en phase 1 §4. Cohérence uGUI maintenue.
- **Prefab Slot réutilisé** : trop de plomberie POC, le `Ctrl+D` suffit. À reprefabricer en phase 3 si on touche au visuel.
- **Slot avec `Image` jaune full-cover + alpha bas pour la sélection** : alternative au `SelectionFrame` outline. Choisie au final pour sa simplicité visuelle et la facilité de setup (pas de sprite custom à fabriquer).
- **CanvasGroup + alpha 0 au lieu de SetActive(false)** pour le panel : aurait évité le bug désabonnement mais demande un setup supplémentaire. Le fix OnDestroy est plus propre côté code.
- **Cycle hotbar via molette en complément des touches** : option discutée et écartée. À ré-ajouter si Pascal trouve le confort manquant.
- **Pause du jeu pendant inventaire ouvert** : option écartée (cohérent survie temps réel).
- **`Previous`/`Next` conservés en complément** : retirés totalement de l'InputActionAsset. Si besoin futur, ré-ajouter proprement.

**Conséquences.**
- L'API publique `Inventory.OnSlotChanged` est désormais consommée par 2 systèmes en aval (`PlayerEquipment` côté gameplay, `InventorySlotView` côté UI). Pattern fan-out validé : un event C# `Action` peut alimenter N consommateurs sans coupler les consommateurs entre eux.
- `InventoryUI` est le **premier composant UI** du projet à manipuler le curseur. Convention assouplie « le dernier qui écrit gagne, pas de stack formel tant qu'on n'en a pas besoin ». À surveiller quand on ajoutera d'autres menus.
- Convention « désabonnement à OnDestroy plutôt qu'OnDisable » pour les composants UI dont la data doit rester à jour même en arrière-plan. Pattern à répliquer pour les futurs HUD craft, dialogue, journal.
- L'`InputActionAsset` perd 2 actions (`Previous`, `Next`) et en gagne 5 (`ToggleInventory`, `EquipSlot1-4`). Net +3 actions. Pas de breaking change côté code consommateur car `PlayerHarvester` est le seul à les utiliser et il a été refondu en même temps.
- Phase 2 termine la couche "voir l'état". Phase 3 ajoutera la couche "modifier l'état via interaction UI" (drag & drop, drop au monde).
- Issue #7 reste OPEN, phase 3 à enchaîner. Décision « pickup auto » toujours en attente d'arbitrage Pascal (non bloquant).

---

### 2026-05-22 — Inventaire joueur phase 1/3 : data model + pickup (Sprint 1, issue #7 partiel)

**Contexte.** Phase 1 de l'issue #7 : poser la fondation fonctionnelle de l'inventaire (data model + pickup auto + équipement via hotbar) **sans UI**, observable via Console et Inspector. Phases 2 (UI inventaire + hotbar visible) et 3 (drag & drop + drop d'items depuis l'inventaire) à enchaîner sur la même issue. Découpage en phases validé pour permettre des validations intermédiaires.

**Décisions.**
1. **Structure inventaire : capacité fixe en slots, paramétrée par instance.** `Inventory : MonoBehaviour` avec `_capacity` sérialisé. Backpack = capacité 24, Hotbar = capacité 4. Pattern Minecraft/Terraria — la même classe sert les deux usages, pas de sous-classe. Stack max lu sur `ItemData.MaxStackSize` (champ existant depuis #5). Alternative écartée : capacité par poids/volume (immersif survie, mais UI moins intuitive et sur-engineering POC).
2. **Hotbar et backpack = deux instances distinctes d'`Inventory`** (pas un seul conteneur avec slots 0-3 réservés). Chaque instance vit sur un GameObject enfant dédié (`_Player/Backpack/`, `_Player/Hotbar/`) pour pouvoir cohabiter sans conflit `GetComponent`. Permet le transfert via `Inventory.Transfer(slot, target)` qui réutilise `TryAdd` côté cible (stacking automatique).
3. **`InventorySlot` struct readonly top-level dans le namespace** (et non `Inventory.Slot` nested). Évite l'ambiguïté C# entre le type `Inventory` et le namespace `Survain.Gameplay.Inventories` quand on écrit `Inventory.Slot` depuis un autre fichier. Pattern à répliquer si on a d'autres types nested utilisés cross-namespace.
4. **Namespace `Survain.Gameplay.Inventories` au pluriel** (et non `Inventory` singulier) pour éviter le conflit avec la classe `Inventory`. Convention dégagée : *quand un type a un nom qui pourrait servir de namespace, on pluralise le namespace* (cf. `System.Collections` qui contient `Dictionary`/`List`/`Queue`...). Dossier physique `Assets/Scripts/Gameplay/Inventories/` aligné. Pattern à répliquer pour les futurs domaines (ex: `Survain.Gameplay.Buildings` contiendrait un type `Building`).
5. **Pickup auto via trigger sphérique 1.5m** sur `_Player/PickupZone/` (GameObject enfant car la racine porte déjà un `CharacterController` non-trigger). `InventoryPickupZone.OnTriggerEnter` détecte un `WorldItem`, tente `TryAdd`, appelle `WorldItem.Consume(absorbé)`. Si tout est rentré → `Destroy(WorldItem)` ; sinon reste au sol avec quantité réduite (cas inventaire plein partiel). **Toggle `_autoPickup` exposé en Inspector** : permet de basculer en pickup manuel (touche E + raycast) sans refonte si Pascal arbitre dans ce sens. Cf. décisions en attente.
6. **`PlayerEquipment` refondu pour consommer la hotbar.** L'API publique (`CurrentTool`, `CurrentSlotIndex`, `SetTool`, `OnCurrentToolChanged`) reste identique → `PlayerHarvester` n'a pas eu à être touché (pattern API stable / impl libre). Le `_toolSlots[]` interne disparaît au profit d'une référence vers un `Inventory` (la hotbar). `OnCurrentToolChanged` est re-émis quand la hotbar change son slot équipé (cas : future drag & drop).
7. **`WorldItem` remplace `WorldItemDropPlaceholder`.** Visuel cube URP/Lit (réutilise le pattern du fix drop URP de #36). API : `Configure(item, qty)` (init après Instantiate), `Consume(int)` (retrait partiel par PickupZone). Destruction auto quand quantité tombe à 0. `ResourceNode.SpawnDrop` modifié pour instancier `WorldItem` au lieu du placeholder.
8. **`HotbarBootstrap` provisoire pour pré-injecter les outils de base** (hache + pioche dans `_Player/Hotbar`). `[DefaultExecutionOrder(-50)]` pour tourner avant `PlayerEquipment.Start` qui équipe le slot initial. À supprimer en phase 3 (drop d'items depuis l'inventaire) ou à reconvertir en *save loader* quand on aura la sauvegarde.
9. **Pas d'UI en phase 1.** Validation via Console (logs `Pickup: ...`, `Outil équipé: ...`) et menu `⋮ → Log inventory state` sur le composant `Inventory` dans l'Inspector. La phase 2 introduira le panel ouvrable Tab/I + la hotbar visible bas d'écran.

**Alternatives écartées.**
- **Capacité par poids/volume** : immersif survie mais sur-engineering POC, UI moins intuitive. À reconsidérer post-POC si la direction artistique survie devient core.
- **Un seul conteneur Inventory avec slots 0-3 = hotbar** : économise une instance mais demande 2 vues sur le même data, complique la sémantique de transferts. Le coût d'une 2e instance est négligeable.
- **Pickup manuel via touche E + raycast** : cohérent avec la récolte (même action) mais alourdit le flow (chaque drop = pression E). Risque de conflit avec la récolte si nœud et drop sont proches. Reportable sans refonte via le toggle `_autoPickup`.
- **UI Toolkit (UIElements)** : moderne mais casse la cohérence avec `InteractionPrompt` (uGUI, cf. 2026-05-21 phase 3 §3). À reconsidérer Sprint 5 (polish) si justifié.
- **`Inventory.Slot` nested** : a déclenché l'erreur compile `'Inventory' is a namespace but is used like a type`. Sortir `InventorySlot` top-level dans le namespace résout sans alias ni refactor utilisateur.

**Conséquences.**
- L'API publique `Inventory.TryAdd/TryRemove/Get/Swap/Transfer + OnSlotChanged/OnInventoryChanged` est le contrat sur lequel l'UI de phase 2 va se brancher. Stable, pas de mutation prévue.
- L'API publique `PlayerEquipment` est inchangée → tout code existant qui lisait `CurrentTool` (PlayerHarvester, futurs systèmes combat) continue de fonctionner sans modif.
- Pattern « event C# pur `Action<int, before, after>` sur l'Inventory + composant satellite (PlayerEquipment) qui s'y abonne pour invalider son état dérivé » devient le template pour les futurs systèmes qui dérivent leur état d'un conteneur (ex: futur HUD qui montre la quantité d'un item spécifique).
- Convention « namespace pluriel pour éviter le conflit avec le type principal » dégagée. À appliquer dès qu'un nouveau domaine a un type cardinal éponyme.
- `WorldItemDropPlaceholder` supprimé. Le pattern « MonoBehaviour code-only avec Rigidbody + visuel URP/Lit créé en code » est consolidé entre `WorldItem` et le fix du drop URP de #36.
- Issue #7 reste OPEN : phases 2 et 3 à enchaîner. Le statut « critical » de l'issue est conservé.

---

### 2026-05-22 — Avatar joueur Synty Sidekick + anims Mixamo + punch de récolte (Sprint 1, issue #33)

**Contexte.** Issue #33 ajoutée en cours de Sprint 1 pour remplacer la capsule placeholder du `_Player` par un avatar visuel humanoïde animé. Périmètre initial : locomotion (idle/walk/run) + saut. Élargi en cours d'implémentation à une anim de récolte (punch générique) pour avoir un feedback visuel immédiat sur la mécanique de #6 — coût marginal très faible vu que le pipeline animator était déjà ouvert. Anim d'attaque combat (Sprint 4) reste hors scope.

**Décisions.**
1. **Avatar = Synty Sidekick Characters** (Asset Store, gratuit). Pivot en cours d'implémentation depuis l'idée initiale du POLYGON Starter Pack mentionnée dans l'issue : Sidekick est plus modulaire (parts détachées : tête/torse/jambes/outfit), avatar Humanoid pré-configuré dans le pack (`Starter_NN-avatar.asset`), perso `Starter_01` à `Starter_04` directement utilisables comme prefab. POLYGON Starter a été désinstallé une fois Sidekick validé. Coût du pivot : ~10 min (un seul ré-export du Source Avatar par FBX). Bénéfice : meilleur visuel + base de customisation pour plus tard. Le pack `PolygonGeneric` (décors/bâtiments) reste importé localement pour le Sprint 2 (construction) mais pas exploité maintenant.
2. **Animations gameplay = Mixamo** (Adobe, gratuit). 5 anims téléchargées en `In Place` + rig `Humanoid` : `Idle`, `Walking`, `Running`, `Jumping`, `Punching`. Versionnées dans `Assets/Animation/Mixamo/` (FBX en Git LFS via `*.fbx` du `.gitattributes`). **Pas dans `ThirdParty/`** car ce ne sont pas des assets Unity Store mais des téléchargements depuis un site web qu'on peut redistribuer dans le projet. Sidekick n'apporte AUCUNE anim gameplay (juste des FaceCycles/FacePoses faciales), donc Mixamo reste indispensable.
3. **Retargeting Humanoid : `Avatar Definition = Create From This Model`** sur chaque FBX Mixamo (PAS `Copy From Other Avatar`). Les rigs Mixamo (`mixamorig:Hips`, `mixamorig:Spine`, ...) et Sidekick (`pelvis`, `spine_01`, ...) ont des noms de bones différents ; `Copy` plante avec "Transform 'pelvis' for human bone 'Hips' not found". `Create From This Model` génère un avatar Humanoid par FBX → Unity retargete au runtime via les deux avatars (anim → Humanoid abstrait → perso). Pattern à appliquer pour toute future anim Mixamo destinée à un perso non-Mixamo. À retenir pour les futurs PNJ : si on importe un perso d'un autre pack que Synty, même règle — chaque source d'anim a son propre avatar.
4. **Apply Root Motion = OFF** sur l'Animator. Le `CharacterController` reste la seule source de vérité pour la position (décision 2026-04-26 §1 conservée). L'anim est purement visuelle. Conséquence directe : Mixamo doit être téléchargé en `In Place`, sinon la translation racine est ignorée mais le cycle de pied reste calibré sur une vitesse différente de notre `WalkSpeed=5`.
5. **`Loop Time` activé manuellement sur Idle/Walking/Running** (au niveau du clip dans l'onglet Animation du FBX). Mixamo n'active **pas** ce flag à l'import, ce qui produit un freeze caractéristique : l'anim joue une fois puis se fige sur la dernière frame pendant que le `CharacterController` continue à déplacer le perso → « personne se fige et glisse ». Première étape de troubleshooting à activer pour toute future anim Mixamo loopable. `Jumping` et `Punching` restent **non-loopées** (one-shots).
6. **Pattern event-driven sur `PlayerController` et `PlayerHarvester`** pour piloter le visuel. Deux events C# publics : `PlayerController.Jumped` (raisé au frame du décollage réel, pas à l'input du saut) et `PlayerHarvester.HitLanded` (raisé après `node.TryHit` réussi, avant le cooldown). Le `PlayerVisualAnimator` satellite s'abonne aux deux pour déclencher les triggers `isJumping` et `isHarvesting`. La locomotion (`speed`, `isGrounded`) reste en polling chaque frame depuis `CharacterController.velocity`/`isGrounded` — valeurs continues, pas besoin d'event. Pattern à répliquer pour le futur combat (events `Attacked`, `Damaged`, etc. consommés par un visual animator enrichi).
7. **Composant `PlayerVisualAnimator` distinct du `PlayerController` et du `PlayerHarvester`** (SRP). Les owners gameplay gardent leur scope ; le visuel est piloté par un satellite qu'on peut désactiver/échanger sans toucher au gameplay. Cohérent avec le pattern `ResourceNodeJuice` ↔ `ResourceNode` (2026-05-21 phase 2). Référence aux events optionnelle (`_harvester` est nullable : si absent, l'anim de récolte n'est juste pas déclenchée — pas d'erreur).
8. **Animator Controller versionné** (`Assets/Animation/PlayerAvatar.controller`, format YAML). Architecture : (a) Blend Tree 1D sur `speed` avec thresholds `(Idle=0, Walking=5, Running=8)` calibrés sur `PlayerMovementConfig.WalkSpeed=5` × `SprintMultiplier=1.6` = 8 ; (b) state `Jump` déclenché par trigger `isJumping` depuis n'importe où, retour au Blend Tree quand `isGrounded=true` ; (c) state `Punch` déclenché par trigger `isHarvesting` via `Any State → Punch` (`Has Exit Time` off, `Can Transition To Self` off pour éviter re-trigger parasite si spam), sortie sur `Punch → Blend Tree` via `Has Exit Time = 0.9` (laisse l'anim se terminer).
9. **`com.unity.shadergraph` + autres deps tirées par Sidekick** ajoutées au manifest automatiquement. **Ne change pas** la décision 2026-04-19 §3 : on continue à écrire nos propres shaders en HLSL pur. Shader Graph est juste présent comme dépendance d'un asset tiers.
10. **`.gitignore` enrichi** : ajout de `/Assets/Synty/` (Sidekick force son installation à ce path absolu, hors `ThirdParty/`) et `/Assets/DownloadCache/` (cache de `.unitypackage` du Package Manager). Conservation de la convention « assets Unity Store gitignored ».

**Alternatives écartées.**
- **POLYGON Starter Pack** (plan initial de l'issue) : fonctionnel mais Sidekick offre un perso plus stylé et un avatar Humanoid déjà calibré. Pivot vers Sidekick fait à mi-parcours sans gros coût.
- **Garder Mixamo + le PolygonStarter rig pour Copy From Other Avatar** : casse au moindre changement de perso. `Create From This Model` est plus robuste et permet de swap le perso cible sans toucher aux FBX d'anim.
- **Synty Sidekick anims natives** : pas d'anims de locomotion/combat dans le pack — uniquement des FaceCycles/FacePoses pour les expressions faciales. Mixamo reste indispensable.
- **Anim Punch dédiée hache/pioche** : reportée au Sprint 4 (combat) avec les vrais outils visibles. Punch générique main nue suffit au POC tant qu'on ne voit pas encore les outils.
- **Root Motion ON** : conflit avec `CharacterController.Move`. Incompatible avec le pattern de contrôle actuel.
- **Wrapper C# pour les paramètres Animator** : pattern `static readonly int Hash = Animator.StringToHash("name")` suffit pour 4 paramètres, pas de génération de code.
- **Reset complet de la scène pour repartir d'une base propre lors du pivot** : choisi car le bordel local d'Aless rendait le swap manuel plus risqué que `git checkout -- Main.unity` + refaire le setup avatar en 5 min. À retenir : quand on se perd dans la Hierarchy après plusieurs essais, le reset git est souvent plus rapide que le nettoyage manuel.

**Conséquences.**
- L'API publique `PlayerController.Jumped` et `PlayerHarvester.HitLanded` sont désormais des points d'extension officiels. Tout futur système qui réagit (juice caméra, audio, particules de poussière au saut, ricochets visuels à la récolte) s'y abonne sans polling.
- Pattern « composant racine pilote la logique + composant satellite pilote le visuel via events + paramètres Animator hashés en `static readonly int` » devient le template pour les futurs avatars (PNJ recrutés au Sprint 3, ennemis sauvages au Sprint 4).
- Convention checklist anti-patinage pour toute nouvelle anim Mixamo : (a) `In Place` coché à Mixamo ; (b) FBX → Rig → `Animation Type = Humanoid` + `Avatar Definition = Create From This Model` ; (c) FBX → Animation → `Loop Time` + `Loop Pose` cochés (sauf one-shots) ; (d) Apply puis tester via la preview FBX avant de l'utiliser dans le Blend Tree.
- Le pivot Sidekick a montré qu'**on peut changer de perso cible sans toucher aux FBX d'anim** tant qu'ils sont en `Create From This Model`. Seul le Source Avatar de l'Animator du perso change. Bon pour la customisation future (PNJ variés partageant les mêmes anims).
- Issue #33 close. Sprint 1 reste à `#7` (inventaire) et `#8` (craft, en attente d'arbitrage Pascal sur la mécanique d'engagement).
- `PlayerCameraConfig.PivotHeightOffset` n'a pas eu besoin d'être ajusté (le perso Sidekick est de taille proche de la capsule placeholder, ~1.6m).

---

### 2026-05-21 — Récolte : phase 3 / respawn + prompt UI (Sprint 1, issue #6 complète)

**Contexte.** Phase 3 et dernière de l'issue #6 : respawn des nœuds après épuisement, prompt UI "[E] Récolter X" quand le joueur vise un nœud. L'outline d'objet interactif (Renderer Feature URP) a été reporté hors phase 3 — le prompt seul suffit visuellement, l'outline pourra revenir en post-#6 si Pascal le juge utile.

**Décisions.**
1. **Respawn = réutilisation in-place via coroutine** (P1). `ResourceNode.Deplete()` ne fait plus `SetActive(false)` : il cache le visuel (`_visualInstance.SetActive(false)`) et désactive le collider. Le GameObject reste actif → la coroutine `RespawnAfterDelay` continue de tourner. Au délai écoulé : reset des HP, réactivation visuel/collider, event `OnRespawned`. Avantage : zéro allocation, état conservé (transform position, abonnements existants).
2. **Délai paramétré par nœud** (P2). Nouveau champ `_respawnSeconds` sur `ResourceNodeData`, convention `0 = pas de respawn` (le défaut C# pour les SO existants → comportement actuel préservé sans tuning). Valeurs initiales : `tree=30s`, `rock=60s`, `fibre-bush=15s`, `ore-deposit=90s`. Variation par type pour rythmer l'exploration (fibres abondantes, minerais rares).
3. **`InteractionPrompt` singleton screen-space overlay auto-créé** (P3). Lazy init au premier accès via `InteractionPrompt.Instance`, `DontDestroyOnLoad`. Construit son propre Canvas + RectTransform + Text au runtime. Zéro setup Unity côté scène — pattern "UI utilitaire qui se débrouille seul". Utilise `UnityEngine.UI.Text` (et non `TMP_Text`) pour éviter la dépendance aux TMP Essentials (pas garanti importés). À migrer vers TMP au Sprint UI quand on touchera à l'esthétique HUD.
4. **Pas d'outline en phase 3** (P4). L'outline rim shader URP propre demande une Renderer Feature URP (config Project Settings + shader custom) — disproportionné pour le POC. Le prompt UI est suffisamment explicite. Solution alternative légère (tint via `MaterialPropertyBlock`) reportée si feel pauvre.
5. **`PlayerHarvester` raycast en continu pour le hover**. Refactor : extraction de `RaycastForNode()` réutilisée par `Update` (toggle prompt selon `_currentTarget`) et `TryHarvest` (utilise `_currentTarget` cached). Une seule source de vérité, ~60 raycasts/s acceptables au POC. Le raycast retourne null si le premier hit non-joueur n'est pas un nœud (vue obstruée par terrain/drop/etc.) — pas de "voir à travers".
6. **Event `OnRespawned` sur `ResourceNode`** consommé par `ResourceNodeJuice` pour reset `_targetHpScale`, `_currentHpScale`, `_scalePunchFactor`, `_shakeEndAt` et la position/échelle du visuel. Sans ça, un nœud respawned reprendrait son apparence rétrécie (~0.6×) d'avant destruction.

**Alternatives écartées.**
- **Destruction + re-spawn par le spawner** : sémantiquement plus propre mais demande de tenir une liste de "slots" à respawner côté spawner, plus de code, plus d'allocs.
- **Object pool** : optimal sur grand nombre de nœuds mais overkill au stade POC.
- **Range min/max sur le délai** (anti-synchronisation) : reportable si on observe que tous les nœuds reviennent en même temps et que ça fait moche visuellement.
- **Canvas world-space par nœud** : 1 Canvas par nœud = overhead non négligeable (centaines de nœuds), et orientation à gérer (billboard). L'overlay global est plus économique.
- **TextMeshPro** : meilleure typo mais demande l'import "TMP Essentials" (assets dans le projet) — pas garanti. uGUI Text marche partout, on migrera plus tard.
- **Recheck raycast dans `TryHarvest`** au lieu du cache : fraîcheur garantie, mais coût d'un raycast supplémentaire au moment du press, et le `_currentTarget` du dernier Update est au pire à 1/60s de retard — négligeable.

**Conséquences.**
- **Pattern "mort logique sans destruction physique"** : pour tout objet qui doit revenir (nœud, futur PNJ ressuscitable, structure réparable), garder le GameObject actif + cacher visuel + désactiver collider permet de coder le retour via une simple coroutine sur l'objet lui-même.
- **Pattern "UI utilitaire singleton lazy"** à répliquer : un service UI (prompt, notif, toast) peut s'auto-instancier au premier accès, créer son Canvas, et exposer une API impérative `Show/Hide`. Aucun composant à pré-placer en scène. À utiliser pour les futurs HUD légers (combat hit feedback, dialogue indicator, etc.).
- Le respawn modifie la sémantique de "fin de vie" d'un nœud : `IsDepleted == true` ne signifie plus "GameObject inactif", mais "HP à 0, en attente du timer". Les abonnés à `OnDepleted` doivent en tenir compte (en pratique : seul `ResourceNodeJuice` est concerné, et il s'en sort avec `OnRespawned`).
- **Issue #6 complète** : la case du Sprint 1 est désormais cochée.
- Pattern dégagé pour les SO data : ajouter un champ avec `défaut = comportement actuel` (ici `0 = pas de respawn`) permet d'introduire une nouvelle mécanique sans casser les SO existants. Pas besoin de migration de YAML.

---

### 2026-05-21 — Récolte : phase 2 / juice (Sprint 1, issue #6 partiel — suite)

**Contexte.** Phase 2 de l'issue #6, dédiée au feedback "juice" : à chaque coup réussi sur un nœud, le visuel doit réagir (shake + scale punch), rapetisser progressivement à mesure que les HP descendent, émettre des particules colorées par type, et jouer un son si disponible. À l'épuisement, un burst plus généreux marque la destruction et doit **survivre** à la disparition du nœud.

**Décisions.**
1. **Composant `ResourceNodeJuice` séparé du `ResourceNode`** (J6). SRP : le core reste pur logique (HP, drop, events) ; le juice est une couche optionnelle attachée par le `ResourceNodeSpawner` à chaque spawn. On peut désactiver tout le feedback en retirant un seul composant. Communication via deux events C# publics sur `ResourceNode` (`OnHit`, `OnDepleted`).
2. **Shake position + scale punch combinés** (J1). Shake amplitude 0.05m, durée 0.15s, intensité décroissante linéaire. Scale punch pic à 1.08×, retour exponentiel (`_scaleReturnRate = 25`). Les deux jouent sur le `VisualInstance` (exposé en lecture via un getter sur `ResourceNode`, sans coupler le Juice à l'init du visuel).
3. **Scale décroissant proportionnel aux HP restants** (J2). Le visuel rapetisse à chaque coup vers une cible smooth-lerpée. Échelle min paramétrée par nœud (`_minScaleAtLastHit`, défaut 0.6). Formule : `Lerp(minScale, 1, CurrentHits / Hits)`.
4. **Particules code-only avec `ParticleSystem` créé au `Start`** (J3). Émission en mode `Emit()` manuel (pas d'emission rate automatique) pour avoir un burst pile au moment du hit. Material `Universal Render Pipeline/Unlit` partagé statiquement (avec fallback `Sprites/Default` si URP indisponible), mesh cube partagé statiquement aussi — évite les allocations à chaque nœud. Couleur lue depuis `ResourceNodeData.HitColor`, taille/vitesse depuis le composant Juice.
5. **Burst de destruction en GameObject standalone** (J5). Le `ResourceNode.Deplete` invoque `OnDepleted` AVANT `SetActive(false)`. Le Juice spawne alors un GameObject indépendant (pas enfant du nœud) avec son propre `ParticleSystem` + `stopAction = Destroy` + sécurité `Destroy(go, 5f)`. Les particules survivent à la disparition du nœud. Pattern à retenir pour tous les effets de "fin de vie" d'un objet : émettre ailleurs, pas en enfant.
6. **API audio prête, sans assets** (J4). Champ `_hitSound : AudioClip` (nullable) sur `ResourceNodeData`. Si null = pas de son joué (pas d'erreur, pas de log). `AudioSource` 3D spatialisée (rolloff linéaire 2-25m) créé au Start sur le nœud. Volume scalé : 1× au hit, 1.4× à la destruction. Pas d'assets audio créés en phase 2 ; on branchera des clips quand on en aura.
7. **Tuning par nœud dans `ResourceNodeData`** (J7). 5 nouveaux champs sous header "Juice" : `HitColor`, `HitParticleCount` (défaut 10), `DepleteParticleCount` (défaut 30), `MinScaleAtLastHit` (défaut 0.6), `HitSound` (nullable). Les params communs (durées, amplitudes shake, vitesse particules) restent sur le `ResourceNodeJuice` composant. Couleurs paramétrées : `tree` brun, `rock` gris, `fibre-bush` vert, `ore-deposit` ocre.

**Alternatives écartées.**
- **Tout dans `ResourceNode`** : viole SRP, fait grossir un composant déjà chargé (HP + drop + visuel + lifecycle).
- **Prefab `ParticleSystem` par type de nœud référencé en SO** : plus joli si les assets existaient, mais demande à créer 4 prefabs binaires en phase POC ; le code-only avec couleur paramétrée donne un résultat suffisant.
- **Bend directionnel du nœud** : joli pour arbres mais demande de connaître la direction d'impact (vector depuis caméra) ; pas universel (un rocher qui se penche, c'est bizarre). À reconsidérer si Pascal veut un feel plus "réaliste arbre".
- **Émettre les particules de destruction en enfant du nœud** : elles seraient coupées au `SetActive(false)` qui suit immédiatement. Le standalone GameObject est la solution propre.
- **`UnityEvent` au lieu d'`event System.Action`** : `UnityEvent` permet d'attacher des listeners dans l'Inspector, mais le `ResourceNodeJuice` est ajouté en code par le spawner — pas de path Inspector utile. Event C# pur, plus économe.

**Conséquences.**
- Pattern « event C# pur `Action` sur un MB + composant satellite qui s'y abonne pour les effets » devient le template pour le feedback. À répliquer pour le futur combat (hit/miss/parry sur un `CombatTarget`), les buildings (placement/destruction), etc.
- Pattern « burst final en GameObject standalone détaché » : utile dès qu'un objet meurt et qu'on veut un effet qui lui survit (drop, mort PNJ, destruction batiment).
- Les `ResourceNodeData.asset` ont leur champ `_hitColor` écrit dans le YAML ; les autres champs juice utilisent les valeurs par défaut C# (10, 30, 0.6, null) jusqu'à ce qu'Unity les sérialise à un prochain save. **Tuning futur via Inspector** : si Pascal veut un effet de destruction plus généreux sur un type, on touchera `_depleteParticleCount` directement dans le `.asset`.
- Les materials/mesh des particules sont cached statiquement (cache cross-instance) : zéro allocation au-delà de la première utilisation. Pattern à retenir si on instancie beaucoup de juice.
- L'issue #6 reste OPEN : phase 3 (respawn + prompt UI + outline) à venir.

---

### 2026-05-21 — Récolte : phase 1 (Sprint 1, issue #6 partiel)

**Contexte.** Phase 1 de l'issue #6, qui couvre la boucle minimale fonctionnelle de récolte : viser un nœud avec la caméra → appuyer sur E → coups successifs → nœud épuisé → drop au sol. Phases 2 (juice : feedback visuel/audio, réduction visuelle du nœud) et 3 (respawn + prompt UI + outline) à venir dans la même branche/PR. Découpage en phases validé pour permettre des validations visuelles intermédiaires avant d'investir dans le polish.

**Décisions.**
1. **Mécanique de récolte = clic discret** (D2). Chaque pression sur Interact = 1 coup ; le nœud porte un `_hits` (nombre de coups requis, ajouté à `ResourceNodeData`). Cooldown entre 2 coups = `HarvestSeconds / ToolData.HarvestSpeed`. Style Minecraft. Alternative écartée : jauge sur touche maintenue (plus immersif mais complique l'animation + tuning). On peut basculer en jauge sans refonte si Pascal préfère le feel.
2. **Ciblage = raycast caméra** (D3) avec `RaycastAll` + tri par distance + filtre des colliders enfants de `_playerRoot`. En 3e personne, le ray part de derrière le joueur et hit son `CharacterController` avant la cible — un simple `Raycast` ne suffit pas. Le filtre via référence Transform évite d'avoir à configurer un layer "Player" côté Unity (le setup reste 100% code).
3. **`ResourceNodeSpawner` séparé du `TerrainGenerator`** (D4). Le `TerrainGenerator` est désormais responsable uniquement du mesh terrain ; le spawn des nœuds est délégué à un nouveau composant. Les anciens placeholders (cubes verts / sphères grises) ont été retirés du `TerrainGenerator`, ainsi que les champs orphelins du `TerrainGenerationSettings` (`_placeholderDensityPer100SqM`, `_maxSlopeDegrees`, `_treeRatio`). Le spawner consomme le `MeshCollider` du terrain via raycast vertical, seed-cohérent avec `GameSettings.WorldSeed` (XOR avec un salt pour ne pas aligner les nœuds sur le bruit Perlin).
4. **Ordre d'exécution via `[DefaultExecutionOrder]`** : `-100` sur `TerrainGenerator`, `+100` sur `ResourceNodeSpawner`. Le `Start` du spawner s'exécute après celui du terrain, donc le `MeshCollider.sharedMesh` est déjà peuplé au moment du raycast. Garde-fou défensif : le spawner log une erreur explicite si `sharedMesh == null` (au cas où on l'utiliserait sans `TerrainGenerator` dans la scène).
5. **`PlayerEquipment` provisoire** (D5). MB léger sur `_Player` avec un tableau `ToolData[] _toolSlots` (2 slots POC = hache + pioche) et un `CurrentTool` exposé read-only + event `OnCurrentToolChanged`. Piloté pour l'instant par les touches `1`/`2` (actions `Previous`/`Next` déjà bindées dans l'`InputActionAsset`). Quand l'inventaire (#7) arrivera, il pilotera `SetTool(int)` à la place — l'API publique reste stable.
6. **`PlayerHarvester` distinct du `PlayerController`** (SRP). Le contrôleur garde son scope locomotion (Move/Jump/Sprint) et reste l'unique propriétaire du cycle Enable/Disable de la map `Player` (cf. décision 2026-04-26 §3). Le `PlayerHarvester` consomme `Interact`/`Previous`/`Next` sans toucher à l'activation. Pattern : un seul owner par map, autant de consumers que nécessaire.
7. **`Interact.started` au lieu de `performed`**. L'action `Interact` de l'`InputActionAsset` a une interaction `Hold` (binding existant). S'abonner à `started` court-circuite le délai Hold et donne le comportement de clic discret voulu sans modifier l'asset binaire. Pattern à retenir : pour les actions avec interactions complexes, on choisit le phase d'event (`started`/`performed`/`canceled`) plutôt que de toucher à l'asset.
8. **Drop = `WorldItemDropPlaceholder` jetable** (D6). Cube jaune avec Rigidbody qui tombe au sol et log son contenu (`Xx 'item-id'`) au premier impact. Sera remplacé en #7 par un vrai `WorldItem` (mesh propre, trigger de pickup vers inventaire). Le `ResourceNode` instancie le drop soit via un prefab référencé (`_dropPrefab`), soit en pur code si le champ est null — phase 1 utilise le mode code.
9. **`_visualPrefab` sur `ResourceNodeData`** (D7). Référence optionnelle au mesh à instancier comme enfant du nœud. Fallback automatique si null : primitive colorée (cube vert pour les nœuds requérant une hache, sphère pour les autres). Sur ce projet, branchement avec les prefabs du `SimpleNaturePack` (pack tiers non versionné). Note pratique : les materials Built-in du pack sont rose sous URP → conversion via `Window → Rendering → Render Pipeline Converter` (local à la machine, pas dans le repo).
10. **Punch caméra léger** (D8 partiel). Ajout d'une API publique `PlayerCameraRig.Punch(degrees)` qui ajoute un offset additif au pitch final, décroissant exponentiellement via `_punchDecayRate` (nouveau champ `PlayerCameraConfig`). À chaque coup réussi, le harvester appelle `Punch(2°)`. Pas d'animation avatar (la capsule placeholder du joueur n'est pas riggée — viendra Sprint 4 combat).

**Alternatives écartées.**
- **Une seule grosse PR couvrant les 7 chantiers #6 d'un coup** : review difficile, intégration risquée. Phases successives sur la même branche permettent des points de validation visuels intermédiaires.
- **Touche maintenue + jauge progressive** : plus immersif mais complique l'animation et le tuning ; basculement possible plus tard si feel insatisfaisant.
- **Configurer un layer "Player" exclusif** au raycast : standard Unity mais nécessite setup côté éditeur. Le filtre via Transform reste 100% code, plus self-contained pour un POC.
- **Étendre `TerrainGenerator` pour spawner aussi les nœuds** : couple deux responsabilités, casse SRP. Le spawner dédié est plus extensible (un jour : un spawner par biome).
- **Logique de récolte directement dans `PlayerController`** : viole SRP, gonfle un composant déjà chargé (Move/Jump/Sprint). Le composant dédié reste lisible et testable séparément.
- **Modifier l'`InputActionAsset` pour retirer l'interaction Hold de `Interact`** : nécessaire pour `performed` mais évitable via `started`. Mieux de ne pas toucher à un asset binaire si une solution code-only existe.
- **Pickup direct dans une "console inventory"** sans drop physique : casserait le critère d'acceptation `ressources tombent au sol`.

**Conséquences.**
- Pattern « SO data + MonoBehaviour runtime + référence par GUID via inspector » consolidé : `ResourceNodeData` ↔ `ResourceNode`, comme `BiomeConfig` ↔ (à venir) et `TerrainGenerationSettings` ↔ `TerrainGenerator`.
- Le `_playerRoot` du `PlayerHarvester` exposé en SerializeField nullable avec fallback `transform` au Awake : pattern utile pour tous les futurs systèmes d'interaction joueur (combat, dialogue, construction).
- `Survain.Gameplay.Items` est le nouveau sous-namespace pour les runtime MB des items dans le monde (à distinguer de `Survain.Items` qui reste pour les SO data). Cohérent avec `Survain.Gameplay.Player` ↔ `Survain.Items` au niveau data.
- Les ResourceNodeData.asset sont tunés à `_harvestSeconds = 2` pour la sensation phase 1 (le code de bootstrap garde ses valeurs originales 4/5/6 ; règle « code = origine, asset = tuning » conservée).
- L'ordre des composants via `[DefaultExecutionOrder]` devient le pattern par défaut pour les chaînes de dépendances Start() entre systèmes du monde. À répliquer dès qu'une chaîne similaire apparaît.
- `PlayerCameraConfig.PunchDecayRate` est paramétrable à l'asset ; valeur par défaut 8 (retour ~0.4s pour un punch de 2°). Réutilisable par d'autres systèmes (futur combat).
- L'issue #6 reste OPEN : phases 2 (juice) et 3 (respawn + prompt + outline) à enchaîner. La case `#6` du Sprint 1 sera cochée à la clôture complète.

---

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

*Dernière mise à jour : 2026-06-21 (combat polish — synchro anim/dégât : **impact piloté par l'animation** [Animation Event `AnimImpact` par clip, relayé par `PlayerAttackAnimationRelay`] + **verrou/cadence par durée** `_swingDurationSeconds` [robuste au martelage, ne dépend pas d'un event de fin] ; `EnemyController.Die()` coupe les colliders [plus de coup sur cadavre] ; verrou `IsSwinging` = socle B6 ; réintègre les bulles perdues au merge stacké de #93 ; ⚠️ câblage éditeur : relais sur l'avatar + `AnimImpact` sur Chop/Mine)*
