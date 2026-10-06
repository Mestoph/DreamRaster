# Préparer GitHub — Français

Compte : `Mestoph`
Dépôt : `DreamRaster`

## Automatisation GitHub

Le dépôt utilise les contrôles suivants :

- `Build` valide les sources avec `scripts/verify-local.py`, compile sous Windows et effectue aussi un cross-build Windows depuis Linux ;
- si `tests/DreamRaster.Tests/DreamRaster.Tests.csproj` est présent, `Build` et `Release` exécutent aussi MSTest ;
- `CodeQL` analyse le code C# lors des pushes/PR vers `main`, chaque semaine et à la demande ;
- `CLA Check` exige la confirmation du CLA dans les pull requests ;
- `Dependabot` vérifie chaque semaine les GitHub Actions et les dépendances NuGet ;
- `Linux Cross Publish` vérifie la publication Windows depuis Linux.

Une fois les nouveaux workflows exécutés au moins une fois avec succès, protéger `main` et exiger au minimum les checks `Verify repository`, `Windows build and tests`, `Linux cross-build to Windows`, `Analyze C#` et `CLA acknowledgement` avant fusion.

## Vérification locale

Installer une fois la dépendance YAML :

```bash
python -m pip install -r scripts/requirements-verify.txt
```

Puis vérifier le dépôt :

```bash
python scripts/verify-local.py --root .
```

Le mode `--check-network` ajoute une vérification HTTP des URLs documentées sans télécharger les modèles complets.

## Release

Créer un tag correspondant exactement à la version préparée, par exemple :

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

Le workflow de release :

1. vérifie le dépôt ;
2. restaure et compile l'application ;
3. exécute les tests lorsqu'ils sont présents ;
4. publie l'EXE portable ;
5. crée `DreamRaster-win-x64.zip` ;
6. génère `DreamRaster-win-x64.zip.sha256.txt` ;
7. charge les deux fichiers comme artefact Actions ;
8. crée la GitHub Release uniquement lors d'un push de tag `v*` existant.

Un lancement manuel (`workflow_dispatch`) valide donc le pipeline et produit l'artefact sans créer de release par erreur.

## Important : forks

Un dépôt GitHub public est forkable. GitHub ne propose pas de case « public mais non forkable ». Pour empêcher techniquement les forks, il faut utiliser un dépôt privé répondant aux règles GitHub applicables.

## Licence

Copyright © 2026 Mestoph.
Licence : **GNU AGPL-3.0-or-later**.
Texte complet : `LICENSE`.

CLA : `CLA.md`
Notice : `NOTICE`
