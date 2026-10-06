# GitHub setup — English

Account: `Mestoph`
Repository: `DreamRaster`

## GitHub automation

The repository uses these automated checks:

- `Build` validates the source tree with `scripts/verify-local.py`, builds on Windows and also cross-builds the Windows target from Linux;
- when `tests/DreamRaster.Tests/DreamRaster.Tests.csproj` exists, `Build` and `Release` also run MSTest;
- `CodeQL` analyzes C# on pushes/PRs to `main`, weekly and on demand;
- `CLA Check` requires CLA acknowledgement in pull requests;
- `Dependabot` checks GitHub Actions and NuGet dependencies weekly;
- `Linux Cross Publish` validates Windows publishing from Linux.

After the new workflows have completed successfully at least once, protect `main` and require at least `Verify repository`, `Windows build and tests`, `Linux cross-build to Windows`, `Analyze C#`, and `CLA acknowledgement` before merging.

## Local verification

Install the YAML dependency once:

```bash
python -m pip install -r scripts/requirements-verify.txt
```

Then verify the repository:

```bash
python scripts/verify-local.py --root .
```

`--check-network` additionally checks documented URLs over HTTP without downloading complete model payloads.

## Release

Create a tag that exactly matches the prepared application version, for example:

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

The release workflow:

1. verifies the repository;
2. restores and builds the application;
3. runs tests when they are present;
4. publishes the portable EXE;
5. creates `DreamRaster-win-x64.zip`;
6. generates `DreamRaster-win-x64.zip.sha256.txt`;
7. uploads both files as an Actions artifact;
8. creates the GitHub Release only for an existing pushed `v*` tag.

A manual `workflow_dispatch` run therefore validates the pipeline and produces the artifact without accidentally creating a release.

## Important: forks

A public GitHub repository can be forked. GitHub does not provide a “public but non-forkable” repository switch. To technically prevent forks, use a private repository under the applicable GitHub policy.

## License

Copyright © 2026 Mestoph.
License: **GNU AGPL-3.0-or-later**.
Full text: `LICENSE`.

CLA: `CLA.md`
Notice: `NOTICE`
