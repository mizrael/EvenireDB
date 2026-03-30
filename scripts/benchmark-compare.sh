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

# ── Clean previous results ───────────────────
if [ -d "$ARTIFACTS_DIR" ]; then
    rm -rf "$ARTIFACTS_DIR"
fi

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

RESULT_FILES=$(find "$ARTIFACTS_DIR" -name "*-report-brief.json" 2>/dev/null || true)
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
BASELINE_JSON_FILES=$(find "$BASELINES_DIR" -name "*-report-brief.json" 2>/dev/null || true)
if [ -z "$BASELINE_JSON_FILES" ]; then
    echo "❌ No baseline files found in $BASELINES_DIR"
    echo "   Run with --update-baseline first to capture a reference."
    exit 1
fi

echo ""
echo "📊 Comparing against baseline (threshold: ${THRESHOLD}%)..."
echo ""

HAS_REGRESSION=false
UPDATED_BASELINES=false
TABLE_ROWS=""

# Track which files need baseline updates: file -> list of FullNames that improved
declare -A IMPROVED_BENCHMARKS

for current_file in $RESULT_FILES; do
    filename=$(basename "$current_file")
    baseline_file="$BASELINES_DIR/$filename"

    if [ ! -f "$baseline_file" ]; then
        echo "⚠️  No baseline for $filename — skipping"
        continue
    fi

    # Extract benchmark name from filename
    suite_name=$(echo "$filename" | sed 's/-report-brief\.json//' | sed 's/EvenireDB\.Benchmark\.//')

    # Process each benchmark in the current results
    current_count=$(jq '.Benchmarks | length' "$current_file")

    for (( i=0; i<current_count; i++ )); do
        full_name=$(jq -r ".Benchmarks[$i].FullName" "$current_file")
        method=$(jq -r ".Benchmarks[$i].Method" "$current_file")
        params=$(jq -r ".Benchmarks[$i].Parameters" "$current_file")
        current_mean=$(jq -r ".Benchmarks[$i].Statistics.Mean" "$current_file")

        # Find matching baseline entry by FullName
        baseline_mean=$(jq -r --arg fn "$full_name" \
            '.Benchmarks[] | select(.FullName == $fn) | .Statistics.Mean' \
            "$baseline_file" 2>/dev/null || echo "")

        if [ -z "$baseline_mean" ] || [ "$baseline_mean" = "null" ]; then
            status="🆕 NEW"
            delta_str="—"
            baseline_ms="—"
        else
            # Format to ms
            baseline_ms=$(awk "BEGIN { printf \"%.2f\", $baseline_mean / 1000000 }")

            # Calculate delta percentage
            delta_str=$(awk "BEGIN {
                if ($baseline_mean == 0) { print \"—\" }
                else { printf \"%.1f\", (($current_mean - $baseline_mean) / $baseline_mean) * 100 }
            }")

            # Determine status
            status=$(awk "BEGIN {
                delta = (($current_mean - $baseline_mean) / $baseline_mean) * 100
                if (delta > $THRESHOLD) print \"REGRESSION\"
                else if (delta < -$THRESHOLD) print \"FASTER\"
                else print \"OK\"
            }")

            if [ "$status" = "REGRESSION" ]; then
                status="🔴 REGRESSION"
                HAS_REGRESSION=true
            elif [ "$status" = "FASTER" ]; then
                status="🟢 FASTER"
                IMPROVED_BENCHMARKS["$filename"]+="$i "
                UPDATED_BASELINES=true
            else
                status="✅ OK"
            fi
        fi

        current_ms=$(awk "BEGIN { printf \"%.2f\", $current_mean / 1000000 }")

        # Truncate params for display
        short_params=$(echo "$params" | cut -c1-35)
        label="${method}(${short_params})"

        TABLE_ROWS+=$(printf "%-18s %-48s %12s %12s %8s%% %s" \
            "$suite_name" "$label" "$baseline_ms" "$current_ms" "$delta_str" "$status")
        TABLE_ROWS+=$'\n'
    done
done

# ── Print table ──────────────────────────────
printf "%-18s %-48s %12s %12s %9s %s\n" \
    "Suite" "Benchmark" "Baseline(ms)" "Current(ms)" "Δ%" "Status"
printf '%.0s─' {1..115}
echo ""
echo -n "$TABLE_ROWS"

# ── Exit code ────────────────────────────────
echo ""
if [ "$UPDATED_BASELINES" = true ]; then
    echo ""
    echo "📈 Updating baselines for improved benchmarks..."

    for filename in "${!IMPROVED_BENCHMARKS[@]}"; do
        current_file="$ARTIFACTS_DIR/$filename"
        baseline_file="$BASELINES_DIR/$filename"
        indices="${IMPROVED_BENCHMARKS[$filename]}"

        for idx in $indices; do
            full_name=$(jq -r ".Benchmarks[$idx].FullName" "$current_file")
            current_benchmark=$(jq ".Benchmarks[$idx]" "$current_file")

            # Replace the matching benchmark entry in the baseline
            jq --arg fn "$full_name" --argjson new "$current_benchmark" \
                '.Benchmarks = [.Benchmarks[] | if .FullName == $fn then $new else . end]' \
                "$baseline_file" > "${baseline_file}.tmp" && mv "${baseline_file}.tmp" "$baseline_file"
        done

        echo "   ✅ Updated $filename"
    done

    echo ""
    echo "   Commit the updated baselines to lock in the improvements."
fi

if [ "$HAS_REGRESSION" = true ]; then
    echo "❌ Performance regression detected (>${THRESHOLD}% slower)"
    exit 1
else
    echo "✅ No performance regressions detected"
    exit 0
fi
