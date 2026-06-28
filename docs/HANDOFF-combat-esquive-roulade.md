# 🤝 Handoff — Combat / Esquive = roulade (reprise sur autre PC)

> But : reprendre le travail en cours sur une **nouvelle session Claude Code**, sur un autre poste.
> **Première action de la nouvelle session** : lire `CLAUDE.md` (racine) en entier, puis ce fichier.

---

## 📍 État git

- **Branche de travail** : `feat/combat-esquive-roulade` (PR **#95**, ouverte, non mergée).
- Ce handoff committe aussi le **travail éditeur Unity en cours** (sinon l'autre PC ne l'aurait pas) :
  - `Assets/Animation/Mixamo/Dodge.fbx` (+ `.meta`) — clip de roulade Mixamo (LFS).
  - `Assets/Animation/PlayerAvatar.controller` — état `Dodge` + transitions.
  - `Assets/ScriptableObjects/Player/PlayerMovementConfig.asset` — tuning esquive.
- ⚠️ Sur le poste de reprise : `git checkout feat/combat-esquive-roulade && git pull`, puis **ouvrir Unity** (réimport des assets/LFS). Ne **jamais** `Ctrl+S` la scène en mode Play.

### Déjà mergé dans `main` (contexte, ne pas refaire)
- #92 — B4 modèle de dégâts typés (`DamageType` + `DamageInfo`, roster combat PO : `Foret`/`Plaines`/`Montagnes`/`CoteMaritime` + `Physical`).
- #94 — synchro anim/dégât (pilotée par l'animation) **+** réintégration des bulles de dégâts (#93 avait été perdu dans un merge stacké) **+** `DamageNumberOverlay`.

---

## 🎯 Tâche en cours : esquive = vraie roulade

Objectif : remplacer le « dash glissé maladroit » (A3) par une **anim de roulade**.

### Côté CODE — ✅ FAIT et committé (commit `feat(combat): esquive — anim de roulade via event Dodged`)
- `PlayerController` expose l'event **`Dodged`** (calqué sur `Jumped`), émis au frame où une esquive démarre réellement (énergie OK, hors dash).
- `PlayerVisualAnimator` s'y abonne → déclenche le trigger Animator **`isDodging`**.
- Gameplay du dash **inchangé** (vitesse/i-frames/coût). Le code ne touche pas `Main.unity` (refs auto-résolues).

### Côté ÉDITEUR Unity — 🚧 EN COURS (committé en WIP par ce handoff)
Fait : `Dodge.fbx` importé (Humanoid), état `Dodge` dans `PlayerAvatar.controller`, transition `Any State → Dodge` (condition `isDodging`, **Has Exit Time OFF**, **Can Transition To Self OFF**), sortie `Dodge → Blend Tree` (Has Exit Time ~0.85), tuning sur `PlayerMovementConfig`.

---

## 🐞 BUG OUVERT — à finir : la roulade déconne en jeu

### Symptômes (dans l'ordre où ils sont apparus, après corrections successives)
1. ~~Le perso jouait la locomotion (Run) pendant le dash~~ → réglé en branchant `isDodging`.
2. ~~« Bloqué en plein milieu / répétitif »~~ → réglé : la transition `Any State → Dodge` avait **`Can Transition To Self` = ON** (re-déclenchement). **Mise à OFF** → la roulade va maintenant **au bout**.
3. **ACTUEL** : une fois déclenchée, la roulade **se répète à l'infini**, ET **en plein milieu le perso fait un tour sur lui-même (spin 360°)**. Dans `Dodge.fbx`, **« Root Transform Position (XZ) » a une pastille rouge « loop match »**.

### Ce qui est vérifié / écarté
- ✅ Câblage Animator correct : `Any State → Dodge` immédiat (Has Exit Time off, Can Transition To Self **off**), pointe bien sur l'état `Dodge` (vitesse 1, motion = `Dodge.fbx`), sortie `Dodge → Blend Tree` (Has Exit Time ~0.85, sans condition).
- ✅ `Loop Time` du clip = OFF (confirmé, re-testé ON/Apply/OFF/Apply).
- ✅ Write Defaults cohérents (tous à 1), pas de doublon de transition.
- ✅ Le clip joué **seul** (bouton play du `Dodge.fbx`) est **nickel** (pas de bug, pas de pose debout traînante).
- ✅ Import : `animationType: 3` (Humanoid), comme les clips qui marchent.
- ℹ️ États orphelins « Take 001 » / « mixamo_com » dans le controller = restes de `Axe Swing.fbx`, **inoffensifs** (pas sur le chemin actif).

### 🔬 Hypothèse courante (forte) : root motion non baké
`Dodge.fbx.meta` a **`clipAnimations: []` (VIDE)** → le clip n'a **jamais reçu la config root-transform** que les autres clips ont. Une roulade a un **vrai root motion** (la racine se déplace **et tourne**) → non baké, ça fait **tourner/dériver** le perso (spin 360° + pastille rouge), et ça ressemble à une boucle.

### ➡️ PROCHAINES ÉTAPES À FAIRE (dans Unity)
1. **`Dodge.fbx` → onglet Animation → sélectionner le clip → cocher Bake Into Pose partout** :
   | Section | Bake Into Pose | Based Upon |
   |---|---|---|
   | Root Transform Rotation | ✔ ON | Body Orientation (ou Original) |
   | Root Transform Position (Y) | ✔ ON | Original |
   | Root Transform Position (XZ) | ✔ ON | Center of Mass (ou Original) |
   - `Loop Time` = OFF · puis **Apply**. (Après un vrai Apply, `clipAnimations` ne doit **plus** être vide dans le `.meta`.)
   - → le perso roule **sur place** (plus de spin/dérive), c'est le **dash CharacterController** qui déplace.
2. **Avatar (GameObject qui a l'Animator) → Inspector → décocher `Apply Root Motion`** (convention projet). Si c'était coché, ça expliquait à 100 % le spin 360°.
3. Re-tester : roulade jouée **une fois**, sur place, puis retour locomotion.

### 🪤 Filet de sécurité si ça boucle ENCORE après (1)+(2)
Alors c'est un re-déclenchement runtime → **ajouter un log de diagnostic** dans `PlayerVisualAnimator.OnDodged` : après `SetTrigger`, lancer une coroutine qui logge chaque frame (~1,5 s) `GetCurrentAnimatorStateInfo(0)` : `normalizedTime`, `IsName("Dodge")`, `IsInTransition(0)`. On verra si `normalizedTime` progresse, stalle, ou se **réinitialise** (= re-entrée). Vérifier alors que `Dodged` n'est émis qu'une fois (déjà gardé par `_dodgeTimeRemaining <= 0`).

---

## 🎚️ Tuning esquive (sur `PlayerMovementConfig.asset`, PAS sur le composant)
Accès : `_Player` → `PlayerController` → champ **Config** (double-clic) → ou `Assets/ScriptableObjects/Player/PlayerMovementConfig.asset`.
- **Dodge Duration Seconds** (`_dodgeDurationSeconds`) : durée du **dash** (déplacement). À caler ≈ durée du clip de roulade rogné.
- **Dodge Speed** (`_dodgeSpeed`) : distance d'esquive = vitesse × durée. (12 × 1 s = 12 m = beaucoup ; viser ~3-4 m.)
- **Dodge I Frame Seconds**, **Dodge Energy Cost** : i-frames et coût (placeholders #88).

> Rappel connexe : `_swingDurationSeconds` (cadence d'attaque) est sur le **composant `PlayerEnemyStrike`** (pas un SO) ; il avait été monté à **2 s** pour test → à rebaisser (~0.8) pour un combat nerveux.

---

## 🧠 Comment marche le combat actuel (pour contexte rapide)

- **Attaque** (`PlayerEnemyStrike`) : clic → **swing verrouillé** (`IsSwinging`) pour `_swingDurationSeconds` → 1 swing = 1 coup. Le **dégât** part à la **frame de contact** via un **Animation Event `AnimImpact`** posé sur les clips Chop/Mine, relayé par **`PlayerAttackAnimationRelay`** (composant sur l'avatar, car l'Animator est sur l'enfant). Filet de sécurité temporisé si l'event manque. `AnimSwingEnd` = no-op déprécié.
- **Dégâts typés** : `DamageInfo.Split(total, biomeFraction, biomeType)`. Biome dérivé de l'outil au POC (`ResolveBiomeType` : hache→Forêt, pioche→Montagnes). Migrera sur `WeaponData.BuildHit()` au craft #8.
- **Bulles** (`DamageNumberOverlay`, `Survain.UI`) : 2 nombres colorés (biome + physique) au-dessus de l'ennemi, fire-and-forget. Couleurs : Forêt vert, Plaines doré, Montagnes bleu, Côte maritime rouge, physique gris.
- **`IsSwinging`** = socle réutilisable pour le **coordinateur de compétences B6** (non construit, dépend de Q4).

---

## 🗺️ Suite du combat (après l'esquive)

| Piste | État | Blocage |
|---|---|---|
| Esquive (roulade) | 🚧 en cours (ce handoff) | finir le bug root motion ci-dessus |
| Ennemis : capsules → vrais modèles | à faire | **assets/budget Pascal** (comme #46) |
| **B5** armures | bloqué | **craft #8** (arbitrage Pascal) |
| **B6** kit de compétences | bloqué | **gate Q4** (kit lié arme vs perso) |
| **B7** finition / **#88** équilibrage | plus tard | — |

### Gates Pascal en attente
- **Q2** : split 80 % biome / 20 % physique (et 75/25 armures) à confirmer. Codé en placeholder, rien figé.
- **Q4** : kit de compétences lié à l'**arme** ou au **personnage** ? (à trancher avant B6.)
- **Craft #8** : bloque B5 (armures) et la migration des dégâts sur `WeaponData`.

---

## ✅ Conventions à respecter (rappel)
- Lire/mettre à jour `CLAUDE.md` (journal append-only, plus récent en haut ; ne modifier qu'avec accord, commit `docs(claude): …` dédié).
- Logs via `SurvainLog` (jamais `Debug.Log`).
- Commits Conventional Commits en français ; **ouvrir une PR, ne pas merger** soi-même.
- **Ne jamais sauvegarder `Main.unity` en mode Play** (incidents passés documentés).
- Quand on touche un clip Mixamo : checklist `In Place` + Humanoid `Create From This Model` + Loop (sauf one-shot) + **root transform Bake Into Pose** (la leçon de ce bug).

---

*Handoff rédigé le 2026-06-28 — reprise sur la branche `feat/combat-esquive-roulade` / PR #95.*
