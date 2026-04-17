# 🎮 Installer et lancer SURVAIN

> **Statut :** POC en développement actif. Les instructions ci-dessous décrivent le comportement cible. Tant qu'un build stable n'est pas disponible, se référer à la section [Lancer depuis Unity (dev)](#-lancer-depuis-unity-dev).

---

## 💻 Configuration requise

### Minimale

- **OS** : Windows 10 (64-bit) ou Windows 11
- **Processeur** : Intel Core i5 (génération 6+) ou AMD Ryzen 5 équivalent
- **Mémoire** : 8 Go de RAM
- **Carte graphique** : compatible DirectX 11, 2 Go de VRAM (ex: GTX 1050, RX 560)
- **Espace disque** : 5 Go disponibles (SSD recommandé)
- **DirectX** : Version 11

### Recommandée

- **OS** : Windows 11 (64-bit)
- **Processeur** : Intel Core i7 (génération 10+) ou AMD Ryzen 7 équivalent
- **Mémoire** : 16 Go de RAM
- **Carte graphique** : GTX 1660 / RX 5600 ou supérieure, 6 Go de VRAM
- **Espace disque** : 10 Go disponibles sur SSD

> **macOS et Linux** ne sont pas supportés pour le POC.

---

## 📥 Télécharger le jeu

_Aucune release publique n'est encore disponible. Cette section sera mise à jour dès le premier build du Sprint 0._

Les builds seront publiés sur la [page des Releases GitHub](https://github.com/Alessandro-Bonnafous/Survain-POC/releases).

Chaque release contient :
- `SURVAIN-vX.Y.Z-win64.zip` — archive du jeu
- Notes de version (changements, limitations connues, feedback attendu)

---

## 🔧 Installer

1. Télécharger l'archive `SURVAIN-vX.Y.Z-win64.zip` depuis la page Releases.
2. Extraire l'archive dans un dossier de son choix (ex: `C:\Jeux\SURVAIN\`).
3. Laisser tous les fichiers dans le même dossier — ne pas déplacer `SURVAIN_Data/`, `UnityPlayer.dll`, etc.

### Windows SmartScreen

Au premier lancement, Windows peut afficher un avertissement SmartScreen car l'exécutable n'est pas signé. C'est normal pour un POC.

- Cliquer sur **« Informations complémentaires »**, puis **« Exécuter quand même »**.
- Pour éviter cet avertissement aux lancements suivants : clic droit sur `SURVAIN.exe` → Propriétés → cocher **« Débloquer »** en bas → OK.

### Antivirus

Certains antivirus mettent en quarantaine les exécutables Unity non signés. Ajouter le dossier d'installation aux exclusions si nécessaire.

---

## ▶️ Lancer le jeu

Double-cliquer sur `SURVAIN.exe` dans le dossier d'installation.

Au premier lancement, une fenêtre de configuration Unity peut apparaître pour choisir la résolution et la qualité graphique. Ce n'est temporaire que pour le POC — un menu options intégré arrivera plus tard.

---

## 🎮 Contrôles

_En cours de définition au Sprint 0._ Les contrôles définitifs seront documentés ici dès le premier build.

Clavier/souris prévu par défaut :

| Action | Touche |
|---|---|
| Se déplacer | ZQSD (ou WASD) |
| Courir | Maj gauche |
| Sauter | Espace |
| Regarder | Souris |
| Interagir | E |
| Inventaire | I |
| Pause | Échap |

Support manette prévu mais non garanti pour le Sprint 0.

---

## 💾 Sauvegardes

Les sauvegardes seront stockées dans :

```
%APPDATA%\..\LocalLow\Survain\SURVAIN POC\saves\
```

(chemin typique Unity sous Windows).

> Le système de sauvegarde n'est pas prévu avant le Sprint 2 ou 3.

---

## 🗑️ Désinstaller

Supprimer simplement le dossier d'installation. Pour nettoyer les sauvegardes et préférences :

```
%APPDATA%\..\LocalLow\Survain\
```

Supprimer ce dossier si présent.

---

## 🐛 Signaler un problème

Ouvrir une issue sur le repo : https://github.com/Alessandro-Bonnafous/Survain-POC/issues

Joindre si possible :
- Version du jeu (visible dans le menu principal ou nom de l'archive)
- OS et version Windows
- Carte graphique et pilote
- Description du problème et étapes de reproduction
- Le log Unity, situé par défaut dans :
  ```
  %APPDATA%\..\LocalLow\Survain\SURVAIN POC\Player.log
  ```

---

## 🧪 Lancer depuis Unity (dev)

Tant qu'aucun build n'est publié, le seul moyen de lancer le jeu est d'ouvrir le projet dans l'éditeur Unity. Voir le [README principal](../README.md#-installation-dev) pour la marche à suivre.

---

*Dernière mise à jour : 2026-04-17*
