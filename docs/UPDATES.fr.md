# Mises à jour GitHub — Français

Le système de mise à jour utilise les **GitHub Releases**.

La release doit contenir un asset nommé :

```text
DreamRaster-win-x64.zip
```

Le workflow `.github/workflows/release.yml` crée automatiquement cet asset quand un tag `v*` est poussé.

L’application vérifie l’API :

```text
https://api.github.com/repos/<owner>/<repo>/releases/latest
```

Le dépôt est configurable dans l’onglet Configuration. La mise à jour ne remplace que l’exécutable ; les dossiers `config`, `models`, `runtime`, `images` et `workspace` restent en place.


CLA: `CLA.md`  
Notice: `NOTICE`
