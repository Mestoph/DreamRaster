# GitHub updates — English

The updater uses **GitHub Releases**.

A release must contain this asset:

```text
DreamRaster-win-x64.zip
```

`.github/workflows/release.yml` creates that asset automatically when a `v*` tag is pushed.

The application checks:

```text
https://api.github.com/repos/<owner>/<repo>/releases/latest
```

The repository is configurable in Settings. Updates replace only the executable; `config`, `models`, `runtime`, `images`, and `workspace` remain untouched.


CLA: `CLA.md`  
Notice: `NOTICE`
