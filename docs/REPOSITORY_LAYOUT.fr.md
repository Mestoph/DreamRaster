# Structure du dépôt — Français

La structure introduite en v33 et conservée en v37 sépare le code, les scripts et la documentation afin d'éviter une racine encombrée.

```text
src/DreamRaster/
```

contient uniquement le projet WinForms : `.cs`, `.resx`, `DreamRaster.csproj`, `Assets/` et `Properties/PublishProfiles/`.

```text
scripts/windows/
```

contient les scripts Windows de build, publication, diagnostic, création GitHub et release.

```text
scripts/linux/
```

contient les scripts de cross-build Linux vers Windows.

```text
scripts/common/
```

contient le moteur de diagnostic PowerShell commun.

Le cache NuGet est généré à la racine dans `.nuget/packages/`, même si le `.csproj` se trouve dans `src/DreamRaster/`. Il est régénérable par `dotnet restore` et ne doit pas être inclus dans une archive source.

Les sorties `bin/` et `obj/` restent sous `src/DreamRaster/` et sont ignorées par Git.
