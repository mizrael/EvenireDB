# Benchmark Comparison Script Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a bash script that runs BenchmarkDotNet benchmarks, stores baseline results as JSON in the repo, and compares current performance against the baseline with a configurable regression threshold.

**Architecture:** A single bash script (`scripts/benchmark-compare.sh`) that wraps the existing BenchmarkDotNet tool project. It runs benchmarks via `dotnet run`, parses the JSON Brief export with `jq`, and produces a console diff table. Baseline JSON files live in `benchmarks/baselines/` and are committed to the repo. The script has two modes: `--update-baseline` (capture new baseline) and default (compare against baseline).

**Tech Stack:** Bash, jq, BenchmarkDotNet (existing), dotnet CLI

---

### Task 1: Create the baselines directory and .gitkeep

**Files:**
- Create: `benchmarks/baselines/.gitkeep`

**Step 1: Create directory structure**

```bash
mkdir -p benchmarks/baselines
touch benchmarks/baselines/.gitkeep
```

**Step 2: Add to .gitignore check**

Verify `BenchmarkDotNet.Artifacts/` is already in `.gitignore` (it should be — that's the transient output dir). The `benchmarks/baselines/` directory should NOT be in `.gitignore` since we want baselines committed.

**Step 3: Commit**

```bash
git add benchmarks/baselines/.gitkeep
git commit -m "chore: add benchmarks/baselines directory for perf baselines"
```

---

### Task 2: Create the benchmark comparison script

**Files:**
- Create: `scripts/benchmark-compare.sh`

The script must:
1. Accept `--update-baseline`, `--threshold N` (default 10), and `--filter "pattern"` args
2. Build the solution in Release mode
3. Run BenchmarkDotNet with JSON export
4. In update-baseline mode: copy the brief JSON files to `benchmarks/baselines/`
5. In compare mode: load baseline JSONs, load current run JSONs, match benchmarks by `FullName`, compute `Δ%` on `Mean`, print a table, exit non-zero if any benchmark exceeds the threshold

**Step 1: Write the script**

```bash
#!/usr/bin/env bash
set -euo pipefail

# ──────────────────────────────────────────────
# benchmark-compare.sh
# Runs BenchmarkDotNet benchmarks and compares
# against committed baselines.
#
# Usage:
#   ./scripts/benchmark-compare.sh                    # compare against baseline
#   ./scripts/benchmark-compare.sh --update-baseline  # capture new baseline
#   ./scripts/benchmark-compare.sh --threshold 15     # custom regression threshold
#   ./scripts/benchmark-compare.sh --filter "Read*"   # run subset of benchmarks
# ──────────────────────────────────────────────

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BASELINES_DIR="$ROOT_DIR/benchmarks/baselines"
ARTIFACTS_DIR="$ROOT_DIR/BenchmarkDotNet.Artifacts/results"
BENCHMARK_PROJECT="$ROOT_DIR/tools/EvenireDB.Tools.Benchmark/EvenireDB.Tools.Benchmark.csproj"

UPDATE_BASELINE=false
THRESHOLD=10
FILTER="EvenireDB.Benchmark*"

# ── Parse arguments ──────────────────────────
while [[ $# -gt 0 ]]; do
    case "$1" in
        --update-baseline)
            UPDATE_BASELINE=true
            shift
            ;;
        --threshold)
            THRESHOLD="$2"
            shift 2
            ;;
        --filter)
            FILTER="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: benchmark-compare.sh [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --update-baseline   Run benchmarks and save as new baseline"
            echo "  --threshold N       Regression threshold percentage (default: 10)"
            echo "  --filter PATTERN    BenchmarkDotNet filter pattern (default: EvenireDB.Benchmark*)"
            echo "  -h, --help          Show this help"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# ── Check dependencies ──────────────────────
if ! command -v jq &>/dev/null; then
    echo "❌ jq is required but not found. Install it: https://jqlang.github.io/jq/download/"
    exit 1
fi

if ! command -v dotnet &>/dev/null; then
    echo "❌ dotnet CLI is required but not found."
    exit 1
fi

# ── Build ────────────────────────────────────
echo "🔨 Building solution in Release mode..."
cd "$ROOT_DIR/src"
dotnet build --nologo -v q -c Release
cd "$ROOT_DIR"

# ── Run benchmarks ───────────────────────────
echo ""
echo "🏃 Running benchmarks (filter: $FILTER)..."
echo "   This may take a while..."
echo ""

dotnet run --no-build -c Release \
    --project "$BENCHMARK_PROJECT" \
    -- --exporters JSON --filter "$FILTER"

# ── Locate result files ──────────────────────
if [ ! -d "$ARTIFACTS_DIR" ]; then
    echo "❌ No benchmark results found in $ARTIFACTS_DIR"
    exit 1
fi

RESULT_FILES=$(find "$ARTIFACTS_DIR" -name "*-report-brief.json" 2>/dev/null)
if [ -z "$RESULT_FILES" ]; then
    echo "❌ No brief JSON reports found"
    exit 1
fi

# ── Update baseline mode ────────────────────
if [ "$UPDATE_BASELINE" = true ]; then
    echo ""
    echo "📊 Saving baseline results..."
    mkdir -p "$BASELINES_DIR"

    for f in $RESULT_FILES; do
        filename=$(basename "$f")
        cp "$f" "$BASELINES_DIR/$filename"
        echo "   ✅ $filename"
    done

    echo ""
    echo "✅ Baseline updated in benchmarks/baselines/"
    echo "   Commit these files to make them the reference point."
    exit 0
fi

# ── Compare mode ─────────────────────────────
if [ ! -d "$BASELINES_DIR" ] || [ -z "$(ls -A "$BASELINES_DIR"/*.json 2>/dev/null)" ]; then
    echo "❌ No baseline files found in $BASELINES_DIR"
    echo "   Run with --update-baseline first to capture a reference."
    exit 1
fi

echo ""
echo "📊 Comparing against baseline (threshold: ${THRESHOLD}%)..."
echo ""

HAS_REGRESSION=false

# Build a combined comparison from all result files
COMPARISON_OUTPUT=""

for current_file in $RESULT_FILES; do
    filename=$(basename "$current_file")
    baseline_file="$BASELINES_DIR/$filename"

    if [ ! -f "$baseline_file" ]; then
        echo "⚠️  No baseline for $filename — skipping"
        continue
    fi

    # Extract benchmark name from filename (remove -report-brief.json)
    suite_name=$(echo "$filename" | sed 's/-report-brief\.json//' | sed 's/EvenireDB\.Benchmark\.//')

    # Extract benchmark entries and match by FullName
    baseline_benchmarks=$(jq -c '.Benchmarks[]' "$baseline_file")
    current_benchmarks=$(jq -c '.Benchmarks[]' "$current_file")

    while IFS= read -r current_entry; do
        full_name=$(echo "$current_entry" | jq -r '.FullName')
        method=$(echo "$current_entry" | jq -r '.Method')
        params=$(echo "$current_entry" | jq -r '.Parameters')
        current_mean=$(echo "$current_entry" | jq -r '.Statistics.Mean')

        # Find matching baseline entry
        baseline_mean=$(jq -r --arg fn "$full_name" \
            '.Benchmarks[] | select(.FullName == $fn) | .Statistics.Mean' \
            "$baseline_file" 2>/dev/null)

        if [ -z "$baseline_mean" ] || [ "$baseline_mean" = "null" ]; then
            status="🆕 NEW"
            delta="—"
        else
            # Calculate delta percentage
            delta=$(echo "$baseline_mean $current_mean" | awk '{
                if ($1 == 0) { printf "—" }
                else { printf "%.1f", (($2 - $1) / $1) * 100 }
            }')

            # Determine status
            delta_abs=$(echo "$delta" | awk '{v=$1; if(v<0) v=-v; print v}')
            is_regression=$(echo "$delta $THRESHOLD" | awk '{print ($1 > $2) ? "yes" : "no"}')

            if [ "$is_regression" = "yes" ]; then
                status="🔴 REGRESSION"
                HAS_REGRESSION=true
            elif echo "$delta" | awk '{exit ($1 < -'"$THRESHOLD"') ? 0 : 1}'; then
                status="🟢 FASTER"
            else
                status="✅ OK"
            fi
        fi

        # Format times to ms
        baseline_ms=$(echo "$baseline_mean" | awk '{printf "%.2f", $1/1000000}')
        current_ms=$(echo "$current_mean" | awk '{printf "%.2f", $1/1000000}')

        COMPARISON_OUTPUT+=$(printf "%-20s %-30s %12s %12s %8s%% %s\n" \
            "$suite_name" "$method($params)" "$baseline_ms" "$current_ms" "$delta" "$status")
        COMPARISON_OUTPUT+=$'\n'
    done <<< "$current_benchmarks"
done

# ── Print table ──────────────────────────────
printf "%-20s %-30s %12s %12s %9s %s\n" \
    "Suite" "Benchmark" "Baseline(ms)" "Current(ms)" "Δ%" "Status"
printf '%.0s─' {1..110}
echo ""
echo "$COMPARISON_OUTPUT"

# ── Exit code ────────────────────────────────
echo ""
if [ "$HAS_REGRESSION" = true ]; then
    echo "❌ Performance regression detected (>${THRESHOLD}% slower)"
    exit 1
else
    echo "✅ No performance regressions detected"
    exit 0
fi
```

**Step 2: Make executable**

```bash
chmod +x scripts/benchmark-compare.sh
```

**Step 3: Commit**

```bash
git add scripts/benchmark-compare.sh
git commit -m "feat: add benchmark comparison script with baseline support"
```

---

### Task 3: Capture initial baseline from current code

**Step 1: Run the script in update-baseline mode**

This will take a while (potentially 30+ minutes for all 3 benchmark suites):

```bash
./scripts/benchmark-compare.sh --update-baseline
```

For a quick test of the script mechanics, you can run just the fastest benchmark:

```bash
./scripts/benchmark-compare.sh --update-baseline --filter "*EventsProviderRead*"
```

**Step 2: Verify baseline files were created**

```bash
ls -la benchmarks/baselines/*.json
```

Expected: 3 files (EventsProviderReadBenchmark, GrpcApiBenchmark, RestApiBenchmark) — or 1 file if using the filtered run.

**Step 3: Commit the baseline**

```bash
git add benchmarks/baselines/*.json
git commit -m "perf: capture initial benchmark baselines"
```

---

### Task 4: Test the comparison flow

**Step 1: Run in compare mode (should show ✅ OK since baseline = current)**

```bash
./scripts/benchmark-compare.sh --filter "*EventsProviderRead*"
```

Expected: All benchmarks show `✅ OK` with ~0% delta (small variance expected).

**Step 2: Verify exit code**

```bash
echo $?  # Should be 0
```

---

### Task 5: Update README with benchmark instructions

**Files:**
- Modify: `.github/copilot-instructions.md` — add a brief benchmarking section

Add to copilot-instructions.md under Docker section:

```markdown
### Benchmarking

Run benchmarks and compare against baseline:
```shell
# Compare current branch against baseline
./scripts/benchmark-compare.sh

# Capture a new baseline (run on main before making changes)
./scripts/benchmark-compare.sh --update-baseline

# Run only a specific benchmark
./scripts/benchmark-compare.sh --filter "*EventsProviderRead*"

# Custom regression threshold (default: 10%)
./scripts/benchmark-compare.sh --threshold 15
```

Baseline JSON files are committed in `benchmarks/baselines/`. Update them when merging perf improvements to main.
```

**Step 2: Commit**

```bash
git add .github/copilot-instructions.md
git commit -m "docs: add benchmark comparison instructions"
```

---

## Execution Order

Tasks 1-5 are sequential. Task 3 is the slowest (benchmark run).

## Quick Verification Path

To test the script mechanics without waiting 30+ minutes for all benchmarks, use:
```bash
./scripts/benchmark-compare.sh --update-baseline --filter "*EventsProviderRead*"
./scripts/benchmark-compare.sh --filter "*EventsProviderRead*"
```

This runs only the disk read benchmark (~5 min), which is the most relevant to the current perf work.
