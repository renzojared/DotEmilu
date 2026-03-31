# Coverage Configuration Reference

Personal reference for coverage tooling decisions in DotEmilu.

## Stack

| Tool | Package | Role |
|---|---|---|
| coverlet.MTP | `coverlet.MTP 8.0.1` | Code coverage collection (MTP-native) |
| Codecov | `codecov/codecov-action@v5` | Upload, threshold enforcement, PR comments |
| codecov.yml | repo root | Codecov server-side config |

## coverlet.MTP CLI Options

### Used in CI

| Option | Purpose |
|---|---|
| `--coverlet` | Enable coverage collection |
| `--coverlet-output-format cobertura` | Cobertura XML (Codecov compatible) |
| `--coverlet-include "[DotEmilu*]*"` | Only measure `src/` assemblies |
| `--coverlet-exclude "[DotEmilu.*Tests*]*"` | Exclude test assemblies |
| `--coverlet-exclude "[DotEmilu.Samples*]*"` | Exclude sample assemblies |
| `--coverlet-exclude-by-attribute "GeneratedCodeAttribute"` | Exclude generated code |
| `--coverlet-skip-auto-props` | Skip auto-implemented properties |

### Available but not used

| Option | Why skipped |
|---|---|
| `--coverlet-single-hit` | Less precise, marginal perf gain |
| `--coverlet-include-test-assembly` | Never want to measure test code |
| `--coverlet-include-directory` | Not needed |
| `--coverlet-exclude-by-file` | No migration files in tests |
| Threshold (`/p:Threshold`) | **Not supported** in coverlet.MTP 8.0.1 — use Codecov instead |

### Filter expression syntax

```
[Assembly-Filter]Type-Filter
```

- `*` = zero or more chars
- `?` = makes prefixed char optional
- `--coverlet-exclude` takes precedence over `--coverlet-include`

## codecov.yml Options

### Used

| Option | Value | Purpose |
|---|---|---|
| `codecov.require_ci_to_pass` | `true` | Wait for CI before sending Codecov status |
| `codecov.notify.after_n_builds` | `2` | Wait for both uploads (unit + integration) before sending status — prevents partial coverage reports |
| `coverage.status.project.default.target` | `auto` | Don't decrease from base commit |
| `coverage.status.project.default.threshold` | `2%` | Allow up to 2% decrease tolerance |
| `coverage.status.patch.default.target` | `80%` | New/modified code must have ≥80% coverage |
| `ignore` | `samples/**`, `tests/**` | Exclude from coverage metrics |
| `flags` (unittests, integrationtests) | `paths: src/`, `carryforward: true` | Separate coverage by test type; carryforward keeps last known coverage if a flag isn't uploaded |
| `comment.layout` | `reach, diff, flags, files` | PR comment with full breakdown |

### Available but not used

| Option | Why skipped |
|---|---|
| `coverage.status.changes` | Redundant with patch |
| `coverage.notify` | Default notifications are fine |
| `parsers` | Auto-detection works for Cobertura |
| `fixes` | No path mapping needed |

## Codecov Action (CI Workflow)

### Used

| Input | Value | Purpose |
|---|---|---|
| `directory` | Per test project | Find coverage files recursively |
| `flags` | `unittests` / `integrationtests` | Tag each upload |
| `fail_ci_if_error` | `false` | Codecov outage doesn't block CI |

### Not used

| Input | Why |
|---|---|
| `verbose` | Debug only, noisy in logs |
| `files` | `directory` is more resilient to path changes |
| `dry_run` | Only for testing config |
| `name` | Not needed with flags |

## Why coverlet.msbuild didn't work

`global.json` forces `"runner": "Microsoft.Testing.Platform"`. `coverlet.msbuild` hooks into
the VSTest MSBuild target, which MTP bypasses entirely. Result: `/p:CollectCoverage=true` is
silently ignored → 0 coverage files generated.

`coverlet.MTP` is a native MTP extension that instruments assemblies via
`ITestHostProcessLifetimeHandler`, independent of VSTest.

## Why xunit.v3.mtp-v2 instead of xunit.v3

`xunit.v3` resolves `xunit.v3.mtp-v1` → `Microsoft.Testing.Platform 1.9.1`.
`xunit.v3.mtp-v2` resolves → `Microsoft.Testing.Platform 2.0.2`, aligned with .NET 10.
Both work, but v2 is the correct target for MTP v2.
