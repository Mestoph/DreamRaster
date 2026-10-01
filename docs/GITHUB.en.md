# GitHub setup — English

Planned account: `Mestoph`  
Planned repository: `DreamRaster`

## Creation

If GitHub CLI (`gh`) is installed and authenticated:

```bat
scripts\windows\create-github-repo.bat
```

The script creates a public repository and pushes the current branch.

## Important: forks

A public GitHub repository can be forked. GitHub does not provide a “public but non-forkable” repository switch. To technically prevent forks, use a private repository under the applicable GitHub policy.

## Release

After pushing the source:

```bash
git tag v33.0.0
git push origin v33.0.0
```

The release workflow builds the application and publishes `DreamRaster-win-x64.zip`.


## Licence / License

Copyright © 2026 Mestoph.  
License: **GNU AGPL-3.0-or-later**.  
Full text: `LICENSE`.


CLA: `CLA.md`  
Notice: `NOTICE`
