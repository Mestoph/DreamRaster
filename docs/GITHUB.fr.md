# Préparer GitHub — Français

Compte prévu : `Mestoph`  
Nom de dépôt prévu : `DreamRaster`

## Création

Si GitHub CLI (`gh`) est installé et authentifié :

```bat
scripts\windows\create-github-repo.bat
```

Le script crée un dépôt public puis pousse la branche courante.

## Important : forks

Un dépôt GitHub public est forkable. GitHub ne propose pas de case « public mais non forkable ». Pour empêcher techniquement les forks, il faut utiliser un dépôt privé répondant aux règles GitHub applicables.

## Release

Après avoir poussé le code :

```bash
git tag v33.0.0
git push origin v33.0.0
```

Le workflow de release compile l’application et publie `DreamRaster-win-x64.zip`.


## Licence / License

Copyright © 2026 Mestoph.  
License: **GNU AGPL-3.0-or-later**.  
Full text: `LICENSE`.


CLA: `CLA.md`  
Notice: `NOTICE`
