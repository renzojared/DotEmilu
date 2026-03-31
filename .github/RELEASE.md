# CI/CD & Release Workflow

Complete guide on how the DotEmilu CI/CD pipeline works, how MinVer
calculates versions, and what you as a developer need to do for each type of
release.

---

## Table of contents

- [Workflow architecture](#workflow-architecture)
- [How MinVer works (your versioning system)](#how-minver-works-your-versioning-system)
- [Distribution strategy](#distribution-strategy)
- [What you need to do: step-by-step scenarios](#what-you-need-to-do-step-by-step-scenarios)
- [How overwriting already-published versions is prevented](#how-overwriting-already-published-versions-is-prevented)
- [Community files (.github)](#community-files-github)
- [Dependabot](#dependabot)
- [Required setup](#required-setup)
- [FAQ](#faq)

---

## Workflow architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│  YOU (developer)                                                    │
│                                                                     │
│  1. Work on a feature/* branch                                      │
│  2. Open PR → main                           ──► CI (build + test)  │
│  3. Merge to main                             ──► Auto pre-release  │
│  4. git tag v10.0.0 && git push --tags         ──► Stable release   │
│  5. git tag v10.1.0-beta.1 && git push --tags  ──► Pre-release      │
└─────────────────────────────────────────────────────────────────────┘
```

### Three workflows

| Workflow | Trigger | What it does | Destination |
|---|---|---|---|
| **CI** | PR to `main` (with relevant changes) | Dependency Review + Build + Test + Coverage (Codecov) + Pack validation | None (validation only) |
| **Publish Pre-release** | Push/merge to `main` (with relevant changes) | Build + Test + Pack + Push | GitHub Packages |
| **Publish Release** | Push of tag `v*` | Build + Test + Pack + Attest provenance + Push + GitHub Release | NuGet.org (stable) or GitHub Packages (pre-release) |

---

## How MinVer works (your versioning system)

### Fundamental rule

MinVer calculates the version by **reading Git tags**, not configuration files.
You do not need to write the version in any `.csproj` or any YAML.

### Calculation algorithm

MinVer walks the commit history backwards from HEAD looking for the nearest tag
that is a valid semver version (with your `v` prefix):

| Situation | Version MinVer calculates |
|---|---|
| HEAD **has** tag `v10.0.0` | `10.0.0` (exact, stable) |
| HEAD **has** tag `v10.0.0-beta.1` | `10.0.0-beta.1` (exact, pre-release) |
| Last tag is `v10.0.0` and there are 5 commits after | `10.0.1-alpha.0.5` |
| Last tag is `v10.0.0-beta.1` and there are 3 commits after | `10.0.0-beta.1.3` |
| No tags in the repo | `0.0.0-alpha.0.N` |

### What is the "height"?

The number at the end (`.5`, `.3`, `.N`) is the **height** — the number of
commits between HEAD and the last tag. This is what makes every push to `main`
generate a unique version automatically.

### Example: version lifecycle

```
v10.0.0                             ← stable tag on a specific commit
  │
  ├── commit 1                      → 10.0.1-alpha.0.1  (auto pre-release)
  ├── commit 2                      → 10.0.1-alpha.0.2
  ├── ...
  ├── commit N                      → 10.0.1-alpha.0.N
  │
v10.0.1                             ← next stable tag (height resets)
  │
  ├── commit 1                      → 10.0.2-alpha.0.1
  └── ...
```

After a stable tag, MinVer increments the patch and appends `alpha.0.N`. Every
merge to `main` increments N automatically. When you create the next stable tag,
the height resets.

### When is the version "stable"?

Only when HEAD **has a tag** that is a stable version (without `-`). Example:

```shell
git tag v10.0.0      # HEAD now has a stable tag
git push --tags       # MinVer calculates 10.0.0 → published to NuGet.org
```

---

## Distribution strategy

```
                    ┌──────────────────┐
                    │   Your code      │
                    └────────┬─────────┘
                             │
                    ┌────────▼─────────┐
                    │  MinVer calculates│
                    │  the version      │
                    └────────┬─────────┘
                             │
                ┌────────────┴────────────┐
                │                         │
        version with "-"          version without "-"
        (pre-release)                 (stable)
                │                         │
    ┌───────────▼──────────┐  ┌───────────▼──────────┐
    │  GitHub Packages     │  │  NuGet.org            │
    │  (private feed)      │  │  (public feed)        │
    └──────────────────────┘  └──────────────────────┘
```

### Packages that are published

All projects under `src/` are packaged together with the same version:

- `DotEmilu.Abstractions`
- `DotEmilu`
- `DotEmilu.AspNetCore`
- `DotEmilu.EntityFrameworkCore`

Projects under `tests/` and `samples/` have `<IsPackable>false</IsPackable>`
and are never packaged.

---

## What you need to do: step-by-step scenarios

### Scenario 1: Normal development (day to day)

```shell
# 1. Work on your branch
git checkout -b feature/add-new-handler

# 2. Make commits
git add . && git commit -m "feat: add batch handler support"

# 3. Open a PR to main
git push -u origin feature/add-new-handler
# → On GitHub, open the PR to main

# 4. CI runs automatically (build + test)
#    If it passes, you can merge

# 5. On merge/push to main → pre-release is published automatically
#    Version: 10.0.0-alpha.0.81 (example)
#    Destination: GitHub Packages
```

**You do not need to do anything else.** The pre-release publishes itself.

### Scenario 2: Publish a stable release

```shell
# 1. Make sure you are on main, up to date
git checkout main && git pull

# 2. Create the tag with the 'v' prefix
git tag v10.0.0

# 3. Push the tag
git push --tags

# → Publish Release triggers automatically:
#   - Build + Test + Pack
#   - Validates that MinVer calculates 10.0.0 (must match the tag)
#   - Publishes .nupkg and .snupkg to NuGet.org
#   - Creates GitHub Release with notes and artifacts
```

### Scenario 3: Publish a beta/RC version by tag

Sometimes you want a "named" pre-release (beta, rc) instead of the auto-generated
`alpha.0.N`:

```shell
# 1. Create a pre-release tag
git tag v10.0.0-beta.1

# 2. Push the tag
git push --tags

# → Publish Release triggers:
#   - MinVer calculates 10.0.0-beta.1
#   - Publishes to GitHub Packages (not NuGet.org)
#   - Creates GitHub Release marked as pre-release
```

### Scenario 4: Promote a beta to stable

If `v10.0.0-beta.1` works well and you want to make a stable release **from the
same commit**:

```shell
# 1. Find the commit for the beta tag
git log --oneline v10.0.0-beta.1

# 2. Create the stable tag on the SAME commit
git tag v10.0.0 v10.0.0-beta.1

# 3. Push
git push --tags

# → MinVer now sees v10.0.0 on that commit → publishes to NuGet.org
```

This is one of MinVer's great advantages over other systems: you can tag the
same commit as RC and then as RTM without needing a new commit.

### Scenario 5: Hotfix on an already-published version

```shell
# 1. Create a branch from the affected version's tag
git checkout -b hotfix/fix-null-ref v10.0.0

# 2. Apply the fix
git add . && git commit -m "fix: null reference in handler pipeline"

# 3. Patch tag
git tag v10.0.1

# 4. Push branch and tag
git push origin hotfix/fix-null-ref --tags

# → Publish Release triggers with 10.0.1 → NuGet.org

# 5. Then merge the hotfix back to main
git checkout main && git merge hotfix/fix-null-ref
git push
```

---

## How overwriting already-published versions is prevented

Four layers of protection:

### 1. MinVer + height = automatically unique versions

Each additional commit after a tag increments the height. It is impossible for
two different commits to produce the same version, unless they share the same tag.

### 2. Tag must point to a commit on main

`publish-release.yml` verifies before anything else that the tagged commit
belongs to the `main` branch. If you accidentally push a tag pointing to a
commit outside of `main`, the workflow fails immediately with a clear error.

### 3. `--skip-duplicate` in `dotnet nuget push`

If for any reason you try to upload a package with a version that already exists
in the feed, the push **does not fail** — it simply skips it. NuGet.org rejects
duplicate uploads, and `--skip-duplicate` prevents the workflow from failing
because of that.

### 4. NuGet.org is immutable for stable versions

NuGet.org does not allow overwriting a stable package once published. If you try
to upload `DotEmilu 10.0.0` twice, the second attempt is ignored.

GitHub Packages does allow re-uploading the same version, but `--skip-duplicate`
prevents duplicate attempts.

### 5. Tag vs. computed version validation

In `publish-release.yml`, before packing, it is verified that the version MinVer
computes matches exactly the version in the tag across all projects under `src/`.
If they do not match, the workflow fails with a clear error. This prevents a tag
placed on the wrong commit from publishing an unexpected version.

---

## Required setup

### NuGet.org — Trusted Publishing (no stored secret needed)

The release workflow authenticates to NuGet.org via **OIDC Trusted Publishing**.
GitHub issues a short-lived token unique to each workflow run; NuGet.org verifies
it against a trusted publisher configuration you define once. No `NUGET_API_KEY`
secret needs to be stored in the repository.

**One-time setup on NuGet.org:**

1. Go to https://www.nuget.org/account/apikeys
2. Click **Add new key** → Type: **Trusted Publisher** → **GitHub Actions**
3. Fill in:
   - Owner: `renzojared`
   - Repository: `DotEmilu`
   - Workflow: `publish-release.yml`
   - Environment: `nuget-org`
4. Save the key (it is not stored anywhere — NuGet uses the OIDC config, not this
   value)

**One-time setup on GitHub:**

1. Go to your repo → **Settings** → **Environments**
2. Create an environment named exactly **`nuget-org`**
3. Optionally add protection rules (e.g., require a reviewer before publishing)

### GitHub Packages

| Secret / Variable | Where it is configured | Purpose |
|---|---|---|
| `GITHUB_TOKEN` | Automatic (provided by GitHub) | Publish to GitHub Packages |

No additional setup required for GitHub Packages.

---

### Codecov — code coverage reporting

CI uploads coverage reports to [Codecov](https://codecov.io) after every PR.
If `CODECOV_TOKEN` is not configured, the upload is skipped silently
(`fail_ci_if_error: false`) — CI will still pass.

**One-time setup:**

1. Go to https://codecov.io and log in with GitHub
2. Add the `DotEmilu` repository
3. Copy the upload token
4. In your repo: **Settings → Secrets and variables → Actions → New secret**
   - Name: `CODECOV_TOKEN`, Value: the token

### Coverage threshold

CI enforces a minimum line coverage percentage via a **repository variable**
(not a secret). If the variable is not set, the threshold defaults to `0`
(no gate).

**To set a threshold:**

1. Go to **Settings → Secrets and variables → Actions → Variables**
2. Create variable `COVERAGE_THRESHOLD` with a numeric value, e.g. `80`

---

## Community files (.github)

GitHub recognizes certain files as "community health files" and displays them in
the repo UI (Insights → Community tab). Projects adopted by the community
(Serilog, MediatR, Polly, ASP.NET Core, etc.) include them as standard.

| File | Purpose |
|---|---|
| `.github/ISSUE_TEMPLATE/bug_report.yml` | Structured form for reporting bugs |
| `.github/ISSUE_TEMPLATE/feature_request.yml` | Form for suggesting features |
| `.github/ISSUE_TEMPLATE/config.yml` | Disables blank issues, adds link to Discussions |
| `.github/PULL_REQUEST_TEMPLATE.md` | Checklist that appears when opening a PR |
| `.github/SECURITY.md` | Vulnerability reporting policy |
| `.github/FUNDING.yml` | "Sponsor" button on GitHub |
| `.github/dependabot.yml` | Automatic dependency updates |
| `.github/RELEASE.md` | This guide |
| `CONTRIBUTING.md` | Contributor guide (build, test, style, conventions) |
| `CODE_OF_CONDUCT.md` | Code of conduct (Contributor Covenant 2.1) |

### About FUNDING.yml

`FUNDING.yml` activates the **"Sponsor"** button on your GitHub repo. Its current
content:

```yaml
github: [renzojared]
```

This enables GitHub Sponsors for your profile. If you have not configured GitHub
Sponsors yet, the button will not do anything visible. To activate it:

1. Go to https://github.com/sponsors/renzojared
2. Follow the onboarding process

If you want to add other donation platforms, you can add more keys:

```yaml
github: [renzojared]
# ko_fi: your_username
# buy_me_a_coffee: your_username
# custom: ["https://your-site.com/donate"]
```

It is not mandatory. Many projects only have `github:`.

---

## Dependabot

### What is it?

Dependabot is a bot built into GitHub that scans your dependencies and opens
automatic PRs when updates are available. It does not touch your code — it only
updates versions in configuration files.

### What does it do in your repo?

Your `dependabot.yml` has two ecosystems configured:

#### 1. `github-actions`

Every Monday it checks whether the actions used in your workflows have new
versions:

```yaml
# If actions/checkout@v6.0.2 has a v6.0.3, it opens a PR
- package-ecosystem: "github-actions"
  schedule:
    interval: "weekly"
    day: "monday"
```

This is important because GitHub Actions are pinned to specific versions for
security. Without Dependabot, you would stay on old versions indefinitely.

#### 2. `nuget`

Every Monday it checks your `PackageVersion` entries in `Directory.Packages.props`:

```yaml
- package-ecosystem: "nuget"
  groups:
    ef-core:       # Grouped PRs for Microsoft.EntityFrameworkCore*
    aspnetcore:    # Grouped PRs for Microsoft.AspNetCore*
    testing:       # Grouped PRs for xunit*, coverlet*, NSubstitute*, Microsoft.NET.Test.Sdk
```

The **groups** reduce noise: instead of 5 separate PRs for each EF Core package,
it opens 1 single PR that updates them all together.

### What do you do?

1. Dependabot opens a PR
2. CI runs automatically (build + test)
3. If it passes, review and merge
4. If it fails, check whether it is a breaking change and decide

---

## FAQ

### Does a pre-release tag trigger BOTH workflows (release and pre-release)?

**No.** The triggers are mutually exclusive:

| Event | `Publish Pre-release` | `Publish Release` |
|---|---|---|
| Push/merge to `main` | **YES** (if relevant paths change) | NO |
| Push of tag `v10.0.0` | NO | **YES** |
| Push of tag `v10.0.0-beta.1` | NO | **YES** |
| PR to `main` | NO (that is `CI`) | NO |

A pre-release tag like `v10.0.0-beta.1` only triggers `Publish Release`. That
workflow detects the `-` in the version and sends to GitHub Packages (not
NuGet.org).

`Publish Pre-release` only triggers on push/merge to the **branch** `main`,
never on tags.

### Are pre-release versions always "alpha"?

**Not necessarily.** It depends on how you arrive at the version:

| How it is generated | Example version | Pre-release identifier |
|---|---|---|
| Push to `main` without tag (automatic) | `10.0.0-alpha.0.5` | `alpha.0.N` (MinVer default) |
| Explicit tag `v10.0.0-beta.1` | `10.0.0-beta.1` | `beta.1` (you decide) |
| Explicit tag `v10.0.0-rc.1` | `10.0.0-rc.1` | `rc.1` (you decide) |
| Explicit tag `v10.0.0-charlie.42` | `10.0.0-charlie.42` | `charlie.42` (whatever you want) |

When you create an explicit tag, **MinVer respects exactly what you put**. It
does not add or remove anything. The version is literally what is in the tag
(without the `v` prefix).

### What about automatic height with explicit tags?

If you create tag `v10.0.0-beta.1` and then make 3 more commits without a new
tag:

```
v10.0.0-beta.1          ← tag on this commit
  ├── commit 1          → 10.0.0-beta.1.1
  ├── commit 2          → 10.0.0-beta.1.2
  └── commit 3          → 10.0.0-beta.1.3
```

MinVer **appends** the height (`.1`, `.2`, `.3`) at the end of the pre-release,
preserving your identifier (`beta.1`). The convention is therefore:

- `v10.0.0-beta.1` → exact commit: `10.0.0-beta.1`
- subsequent commits: `10.0.0-beta.1.N`
- `v10.0.0-beta.2` → next beta tag when you want to "reset" the height

### Can I publish a pre-release manually?

Yes, with a pre-release tag:
```shell
git tag v10.0.0-rc.1 && git push --tags
```

### What happens if I push a tag that does not start with `v`?

Nothing. The workflow only activates with tags matching `v[0-9]+.[0-9]+.[0-9]+*`.

### Can I use `workflow_dispatch` on the publish workflows?

Only `CI` has `workflow_dispatch`. The publish workflows trigger exclusively from
their defined triggers (push to main / push of tag). This is intentional to
prevent accidental publications.

### What happens if a merge to main does not change files in `src/`?

`Publish Pre-release` does not trigger. Only changes in `src/**`, `tests/**`,
`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `nuget.config`,
or the workflow file itself activate publishing.

### Can I have different versions per project?

Not with your current configuration. All projects under `src/` share the same
`MinVerTagPrefix` (`v`) and have no `MinVerIgnoreHeight`. MinVer assigns them all
the same version based on the same tags.

If you ever need independent versioning, MinVer supports per-project tag prefixes
(e.g., `abstractions-v1.0.0`, `efcore-v2.0.0`), but that would require separate
workflows per project.

### How do I know what version MinVer will calculate before pushing?

Run the MinVer target directly (no full build needed):

```shell
dotnet msbuild src/DotEmilu/DotEmilu.csproj -target:MinVer -getProperty:PackageVersion -p:Configuration=Release -nologo
```
