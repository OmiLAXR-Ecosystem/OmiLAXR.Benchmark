# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Working rules (versioning, CHANGELOG, `.meta` files, licenses, compatibility) are in @AGENTS.md. Follow them.

## What this is

`com.rwth.unity.omilaxr.benchmark`: Module for reproducible performance benchmarks of OmiLAXR tracking: warmup and measurement runs, frame-time/FPS/memory/GC sampling, CSV and log output, and a deterministic stress scenario.

## Commands

There is no command-line build or linter: the package compiles inside a Unity project. Paths below are relative to the workspace root (`..`).

- **Develop:** open `OmiLAXR_Dev/` in Unity 6000.3.13f1. Its `Packages/manifest.json` links `com.rwth.unity.omilaxr.benchmark` from disk (`file:`), so edits recompile live.
- **Tests** (`Tests/`, assembly `OmiLAXR.Benchmark.Tests.Runtime`): the test asmdef is a runtime assembly, so the tests are PlayMode-only. Run them in the Unity Test Runner (PlayMode tab).
- **CLI test run.** Batch mode fails while the editor has the project open. In that case, run against an APFS copy (`cp -c -R OmiLAXR_Dev <scratch>/Dev`).

  ```bash
  /Applications/Unity/Hub/Editor/6000.3.13f1/Unity.app/Contents/MacOS/Unity -batchmode \
    -projectPath OmiLAXR_Dev -runTests -testPlatform PlayMode \
    -assemblyNames OmiLAXR.Benchmark.Tests.Runtime -testResults results.xml
  # single test: replace -assemblyNames … with -testFilter OmiLAXR.Benchmark.Tests.<Class>.<Method>
  ```

## Architecture

- **`Benchmark`** (`Runtime/Benchmark.cs`) runs warmup and then measurement, samples every frame, and writes `benchmark_<timestamp>_<reason>.log/.csv` under `Application.persistentDataPath/benchmarks`. The CSV is binned (`csvIntervalSeconds`), or per-frame when that is ≤ 0. The static `Benchmark.StartRecord/StopRecord("name")` adds custom metrics.
- **`StressScenario`** spawns objects with deterministic motion and optional synthetic events (`syntheticEventsPerSecond`, `allocateBytesPerEvent`, `onSyntheticEvent`).
- **`CountingEndpoint`** is a core `Endpoint` that only counts statements, so OmiLAXR's statement flow is measured without network or disk cost.
- **Editor**: `BenchmarkWindow`. **Scenes**: `Resources/Scenes/BenchmarkScene_WithOmiLAXR.unity` and `BenchmarkScene_WithoutOmiLAXR.unity` (baseline). **Samples~**: `plot.py` and `do_plot.sh` (Python/matplotlib), registered as package samples in `package.json`.
