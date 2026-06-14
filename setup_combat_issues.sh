#!/usr/bin/env bash
#
# setup_combat_issues.sh
# Convertit l'issue #16 en ÉPIC "combat à l'énergie" et crée les sous-issues
# du plan agile (Phases A / B / C). Idempotent sur les labels uniquement —
# relancer ce script recréerait des sous-issues en double, à lancer UNE fois.
#
# Prérequis : gh authentifié (`gh auth status`) avec accès en écriture au repo.
# Usage : bash setup_combat_issues.sh
#
set -euo pipefail

REPO="Alessandro-Bonnafous/Survain-POC"
EPIC=16
# Optionnel : décommente et adapte si tu veux rattacher à un milestone existant.
# MILESTONE="Sprint 5"

# --- Labels (idempotent) ----------------------------------------------------
gh label create epic   --repo "$REPO" --color 5319e7 --description "Issue parapluie regroupant des sous-issues" 2>/dev/null || true
gh label create combat --repo "$REPO" --color b60205 --description "Système de combat (#16)"                    2>/dev/null || true

# --- Helper : crée une issue et renvoie son numéro --------------------------
create_issue() {
  # $1 = titre, $2 = labels (csv), $3 = body
  gh issue create --repo "$REPO" --title "$1" --label "$2" --body "$3" \
    | sed -E 's#.*/([0-9]+)$#\1#'
}

echo "Création des sous-issues…"

# ============================================================================
# PHASE A — Socle énergie & ressenti (démarrable tout de suite)
# ============================================================================

A1=$(create_issue \
"feat(combat): réserve d'énergie joueur (PlayerEnergy) + barre HUD" \
"type:gamedesign,combat" \
"## Objectif
Introduire l'**énergie** comme ressource unique du combat : réserve de 100 pts, régénération paramétrable, barre HUD.

## Périmètre
- \`PlayerEnergy\` (satellite sur \`_Player\`, distinct de \`PlayerController\`) : valeur courante, \`TryConsume\`/\`Restore\`, régén après délai, events \`EnergyChanged\`, \`Instance\` statique.
- Config SO \`PlayerEnergyConfig\` (\`Survain.Data\`).
- Barre HUD \`PlayerEnergyBar\` auto-construite (pattern singleton type \`PlayerHealthBar\`).

## Réutilise
Calqué trait pour trait sur \`PlayerHealth\` (#19) + pattern HUD singleton lazy.

## ⏳ Gate Pascal (structurelle)
**Q1** — Énergie = réserve unique partagée (course + esquive + compétences puisent dans les mêmes 100 pts) ?

## Placeholders SO (équilibrage différé)
Vitesse de régén, délai avant régén.

## Definition of Done
Build où la barre se vide/régénère via une conso de test, toutes valeurs ajustables en Inspector.

## Dépend de
—  (sous-issue de #${EPIC})")
echo "  A1 → #$A1"

A2=$(create_issue \
"feat(combat): auto-attack pilotée par l'énergie" \
"type:gamedesign,combat" \
"## Objectif
L'auto-attack consomme de l'énergie (5% spec) et devient la vraie source de dégâts joueur.

## Périmètre
- Remplace/enrichit le placeholder \`PlayerEnemyStrike\` : l'attaque ne part que si l'énergie suffit, sinon échec (feedback).
- Conso, vitesse d'attaque, cooldown = champs SO.

## Réutilise
Event \`Swung\` + pipeline outil-en-main/anim (journal 2026-05-31), \`PlayerEquipment.CurrentTool\`, \`PlayerHealth.Instance\` côté cible.

## ⏳ Gate Pascal
Aucune (conso 5% = valeur de spec, en placeholder).

## Placeholders SO
Vitesse d'attaque, cooldown, conso énergie.

## Definition of Done
Attaquer un ennemi PVE consomme l'énergie ; impossible d'attaquer à sec.

## Dépend de
#${A1}  (sous-issue de #${EPIC})")
echo "  A2 → #$A2"

A3=$(create_issue \
"feat(combat): esquive (i-frames) + course consommant l'énergie" \
"type:gamedesign,combat" \
"## Objectif
Livrer l'**arbitrage tactique mobilité vs attaque** — cœur du pilier anti-zerg.

## Périmètre
- Esquive : consomme 40% d'énergie + fenêtre d'invulnérabilité (i-frames).
- Course : draine l'énergie en continu.
- Conséquence visée (spec) : après 2 esquives, plus assez d'énergie pour enchaîner les compétences lourdes.

## Réutilise
Fenêtre d'invuln de \`PlayerHealth\` pour les i-frames ; Input System (action \`Attack\` déjà en place).

## ⏳ Gate Pascal (structurelle)
**Q1 (corollaire)** — Confirme l'arbitrage : courir/esquiver et attaquer puisent dans la même jauge.

## Placeholders SO
Coût esquive, durée i-frames, drain course.

## Definition of Done
**Build jouable** : ressenti du combat à l'énergie contre les ennemis PVE existants. Candidat tag \`v0.6.0\`.

## Dépend de
#${A1}, #${A2}  (sous-issue de #${EPIC})")
echo "  A3 → #$A3"

# ============================================================================
# PHASE B — Profondeur systémique (gates + couplage craft #8)
# ============================================================================

B4=$(create_issue \
"feat(combat): modèle de dégâts typés (biome 80% / physique 20%)" \
"type:gamedesign,combat" \
"## Objectif
Poser le modèle de dégâts typés : chaque arme = 80% dégâts de biome + 20% physiques.

## Périmètre
- Enum type de dégât + champs sur la donnée d'arme.
- Stub sur hache/pioche (pas encore de vraies armes craftables).

## ⏳ Gate Pascal (structurelle)
**Q2** — Split 80/20 (biome/physique) confirmé sur chaque arme ?

## Placeholders SO
Répartition exacte, valeurs par arme.

## Definition of Done
Les dégâts d'arme se décomposent biome/physique (visible en debug).

## Couplage
Les **vraies armes** dépendent du craft (#8, bloqué arbitrage Pascal).

## Dépend de
#${A2}  (sous-issue de #${EPIC})")
echo "  B4 → #$B4"

B5=$(create_issue \
"feat(combat): modèle d'armure 5 pièces + résistances typées (75/25) + mix" \
"type:gamedesign,combat" \
"## Objectif
Modèle d'armure permettant les builds spécialisés ou polyvalents.

## Périmètre
- 5 slots (Casque / Torse / Gants / Jambières / Bottes).
- Champs de résistance : set complet 100% = 75% dégâts de biome / 25% physiques.
- Support du **mix** (pièces de biomes différents → résistances cumulées).

## ⏳ Gate Pascal (structurelle)
**Q3** — Modèle d'armure + intention du mix (builds variés) confirmés ?

## Placeholders SO
Répartition de la résistance entre les 5 pièces.

## Definition of Done
Le modèle existe et module les dégâts reçus. ⚠️ Le **ressenti complet** (builds) est **différé** au déblocage du craft.

## Dépend de
#${B4}, **craft #8 (bloqué arbitrage Pascal)**  (sous-issue de #${EPIC})")
echo "  B5 → #$B5"

B6=$(create_issue \
"feat(combat): kit de compétences (auto + compétences + ultimate)" \
"type:gamedesign,combat" \
"## Objectif
Modèle d'ability + montée progressive du kit (auto → 1 compétence → 4 + ultimate).

## Périmètre
- Données d'ability : coût énergie, cooldown, anim/VFX, durée de contrôle.
- POC : auto + **1 compétence** suffisent à valider l'économie d'énergie.
- Palier ultérieur : 4 compétences + ultimate (asset-lourd : anims/VFX).
- Conso spec : Comp. 1 = 10% / Comp. 2 = 15% / Comp. 3 = 20% / Comp. 4 = 25% / Ultimate = 50%.

## ⏳ Gate Pascal (structurelle)
**Q4** — Le kit (4 compétences + ultimate) est-il **lié à l'arme** (changer d'arme change le kit) ou **lié au personnage** ?

## Placeholders SO
Coûts, cooldowns, durées de contrôle.

## Definition of Done (POC)
Auto + 1 compétence + esquive jouables. Kit complet = palier suivant.

## Dépend de
#${A3}, #${B4}  (sous-issue de #${EPIC})")
echo "  B6 → #$B6"

B7=$(create_issue \
"feat(combat): finition — switch 2 armes, critiques, durée de contrôle" \
"type:gamedesign,combat" \
"## Objectif
Compléter le méta de combat : 2 armes équipées + switch, critiques, durées de contrôle.

## Périmètre
- 2 armes équipables simultanément + switch en combat.
- Critiques (taux + multiplicateur).
- Durées de contrôle (étourdissements, etc.).

## ⏳ Gate Pascal (mineure)
Switch d'arme = instantané, ou coût/cooldown ?

## Placeholders SO
Crit rate/mult, durées de contrôle, coût éventuel du switch.

## Definition of Done
2 armes équipables + switch ; critiques appliqués ; placeholders ajustables.

## Dépend de
#${B6}  (sous-issue de #${EPIC})")
echo "  B7 → #$B7"

# ============================================================================
# PHASE C — Équilibrage (non bloquant, pass dédié Pascal)
# ============================================================================

C1=$(create_issue \
"chore(combat): pass d'équilibrage (valeurs « à définir »)" \
"type:gamedesign,combat" \
"## Objectif
Regrouper tous les chiffres « à définir » de la spec en un seul pass d'arbitrage Pascal.

## Valeurs à figer
Régén d'énergie · vitesse d'attaque · cooldowns · PV total joueur · crit (taux + mult) · durées de contrôle · répartition des résistances entre pièces d'armure · coûts esquive/course.

## ⏳ Gate Pascal
**Q5** — Tous ces chiffres restent en placeholders ajustables jusqu'à une session d'équilibrage dédiée ?

## Definition of Done
Valeurs validées avec Pascal et figées dans les SO.

## Dépend de
#${A3} (au minimum) ; idéalement après #${B7}  (sous-issue de #${EPIC})")
echo "  C1 → #$C1"

# ============================================================================
# ÉPIC — réécriture de #16
# ============================================================================
echo "Conversion de #${EPIC} en épic…"

read -r -d '' EPIC_BODY <<EOF || true
# ÉPIC — Système de combat à l'énergie (anti-zerg, effectifs fixes)

> Pilier non négociable : **combat anti-zerg à effectifs fixes**, bâti sur une **économie d'énergie**.
> Spec PO : voir \`docs/Spec_combat.md\`.

## Démarche
Découpage en increments testables, chacun avec sa **gate Pascal structurelle** (le *modèle*, pas les chiffres). Les valeurs « à définir » sont des **placeholders SO** réglés à un pass d'équilibrage (#${C1}, non bloquant).

## Phase A — Socle énergie & ressenti _(démarrable maintenant)_
- [ ] #${A1} — Réserve d'énergie (\`PlayerEnergy\`) + barre HUD
- [ ] #${A2} — Auto-attack pilotée par l'énergie
- [ ] #${A3} — Esquive (i-frames) + course → **build jouable, candidat \`v0.6.0\`**

## Phase B — Profondeur systémique _(gates + couplage craft #8)_
- [ ] #${B4} — Modèle de dégâts typés (biome 80% / physique 20%)
- [ ] #${B5} — Modèle d'armure 5 pièces + résistances (75/25) + mix _(dépend du craft #8)_
- [ ] #${B6} — Kit de compétences (auto + compétences + ultimate)
- [ ] #${B7} — Finition : switch 2 armes, critiques, durée de contrôle

## Phase C — Équilibrage _(non bloquant)_
- [ ] #${C1} — Pass d'équilibrage des valeurs « à définir »

## ⚠️ Dépendances bloquantes
- La Phase B (armures) a besoin du **craft #8**, lui-même bloqué arbitrage Pascal.
- Les gates structurelles **Q1→Q5** sont posées à Pascal (Discord/Notion) avant de coder le chunk correspondant.

## Existant réutilisé
\`PlayerHealth\`/i-frames (#19), event \`Swung\` + pipeline outil-en-main (placeholder combat, 2026-06-12), \`PlayerEquipment.CurrentTool\`, ennemis PVE + \`EnemyAttackState\` (#17), patterns HUD singleton & camera punch.
EOF

gh issue edit "$EPIC" --repo "$REPO" \
  --title "[EPIC] Système de combat à l'énergie (anti-zerg, effectifs fixes)" \
  --add-label epic \
  --add-label combat \
  --body "$EPIC_BODY"

echo ""
echo "✅ Terminé."
echo "Épic #${EPIC} mis à jour. Sous-issues : A=$A1,$A2,$A3  B=$B4,$B5,$B6,$B7  C=$C1"
echo "Pense à rattacher au milestone voulu (Sprint 5 ?) dans l'UI, ou via --milestone."
