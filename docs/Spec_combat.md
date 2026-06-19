# ⚔️ Spec — Système de combat (SURVAIN)

> Source : spécification PO (Pascal), mise à jour le 2026-06-14.
> Référencée par l'épic **#16** (`[EPIC] Système de combat à l'énergie`).
>
> **Statut chiffres** : toutes les valeurs marquées « à définir » sont des **placeholders SO**
> réglés au pass d'équilibrage (#88, Phase C), non bloquant. Les **gates structurelles**
> (Q1→Q5) portent sur le *modèle*, pas sur les chiffres.

---

## 🗡️ Armes

| Aspect | Spécification |
|---|---|
| **Équipement** | 2 armes équipées simultanément / switch d'arme en combat. |
| **Compétences** | Auto-attack + 4 compétences + 1 ultimate. |
| **Dégâts** | Chaque arme inflige **80 % de dégâts du biome** + **20 % de dégâts physiques**. |
| **Critiques** | Taux et multiplicateur _à définir_. |
| **Vitesse d'attaque / cooldown / durée de contrôle** | _À définir._ |
| **Qualité** | La qualité de l'arme modifie **uniquement les dégâts** de l'arme. |

---

## 🛡️ Armures

| Aspect | Spécification |
|---|---|
| **Composition** | 5 pièces : Casque / Torse / Gants / Jambières / Bottes. Répartition de la résistance entre pièces _à définir_. |
| **Résistances** | Un set complet de qualité 100 % protège : **75 % des dégâts de son biome** + **25 % des dégâts physiques**. |
| **Mix d'armures** | Combiner des pièces de biomes différents → résistances cumulées. _Exemple :_ 2 pièces Côte maritime + 2 pièces Montagne + 1 pièce Forêt → Résistance Vent + Résistance Froid + Résistance Physique. |
| **Objectif** | Permettre des **builds spécialisés ou polyvalents**. |

---

## ⚡ Énergie

| Aspect | Spécification |
|---|---|
| **Réserve** | **100 points.** Vitesse de régénération _à définir_. |
| **Consommation** | Auto-attack : **5 %** · Compétence 1 : **10 %** · Compétence 2 : **15 %** · Compétence 3 : **20 %** · Compétence 4 : **25 %** · Ultimate : **50 %**. |

---

## 🏃 Déplacements

| Aspect | Spécification |
|---|---|
| **Course** | Consomme de l'énergie (drain continu). |
| **Esquive** | Consomme **40 % d'énergie**. Après **2 esquives**, énergie insuffisante pour enchaîner plusieurs compétences lourdes. |
| **Objectif** | **Choix tactique entre mobilité et attaque.** |

---

## 🥊 Combat — objectifs de ressenti

| Aspect | Spécification |
|---|---|
| **Durée moyenne d'un duel** | ~30 secondes. |
| **Combat de groupe** | 1 à 3 minutes. |
| **Total de points de vie joueur** | _À définir._ |

**Le joueur doit :**
- gérer son énergie ;
- choisir ses esquives ;
- utiliser le bon type de dégâts ;
- adapter son équipement au biome rencontré.

---

## ❓ Gates structurelles posées à Pascal (Q1→Q5)

Ces questions doivent être tranchées **avant de coder le chunk correspondant** (le *modèle*, pas les chiffres) :

1. **Q1 — Énergie partagée.** La réserve de 100 pts est-elle **unique et partagée** (course + esquive + compétences puisent dans la même jauge) ? → consommée en **A2/A3** ; **A1 est neutre** vis-à-vis de Q1.
2. **Q2 — Split des dégâts.** Confirme-t-on **80 % biome / 20 % physique** sur chaque arme ? (et **75 % / 25 %** côté résistances d'armure)
3. **Q3 — Mix d'armures = builds.** Le modèle d'armure 5 pièces + l'intention du mix (résistances cumulées → builds variés) sont-ils confirmés ?
4. **Q4 — Kit de compétences.** Le kit (4 compétences + ultimate) est-il **lié à l'arme** (changer d'arme change le kit) ou **lié au personnage** ?
5. **Q5 — Ordre « ressenti d'abord ».** Tous les chiffres restent-ils en **placeholders SO ajustables** jusqu'à une session d'équilibrage dédiée (#88) ?

> ⚠️ La **Phase B** (armures, #85) dépend du **craft #8**, lui-même bloqué arbitrage Pascal.
