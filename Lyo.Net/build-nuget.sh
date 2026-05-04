#!/bin/bash

# Build and pack NuGet packages for Lyo libraries
# Usage:
#   ./build-nuget.sh                    # Build all packages (skips unchanged)
#   ./build-nuget.sh -v 2.0.0           # Build all packages with version 2.0.0
#   ./build-nuget.sh Lyo.Encryption     # Build specific package (skips unchanged)
#   ./build-nuget.sh -v 1.5.0 Lyo.Encryption  # Build specific package with version
#   ./build-nuget.sh -f Lyo.Encryption  # Force build even if no git changes
#   ./build-nuget.sh -v 1.0.27 Lyo.Sms.Twilio  # Build only SMS packages + deps at 1.0.27
#
# Change detection:
#   Each project's source directory is fingerprinted using git (committed state +
#   staged/unstaged diffs + untracked file contents). If the fingerprint matches the
#   last successful build, the project is skipped — no new .nupkg is emitted and the
#   version number in consuming nuspecs does not need to change.
#   Use -f / --force to bypass change detection and always rebuild everything.
#
# When building specific packages with -v, only that package and its Lyo deps are built.
# Packaging uses SDK-style `dotnet pack`; project/package dependencies are derived automatically
# from ProjectReference items in each csproj — no nuspec files are used.
#
# Environment variables:
#   NUGET_OUTPUT_DIR - Output directory for packages (default: ~/nuget-local)
#   BUILD_CONFIG     - Build configuration (default: Release)
#
# Version: the same VERSION is passed to dotnet build and dotnet pack so NuGet package version
# and embedded assembly metadata (AssemblyInformationalVersion, FileVersion, etc.) stay aligned.

# Don't use set -e here - we want to handle errors manually
# set -e  # Exit on error

# Parse command line arguments
VERSION="1.0.0"
FORCE=0
PATTERNS=()

while [[ $# -gt 0 ]]; do
    case $1 in
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        -f|--force)
            FORCE=1
            shift
            ;;
        -h|--help)
            echo "Build and pack NuGet packages for Lyo libraries"
            echo ""
            echo "Usage:"
            echo "  $0 [OPTIONS] [PATTERN...]"
            echo ""
            echo "Options:"
            echo "  -v, --version VERSION    Specify package version (default: 1.0.0)"
            echo "  -f, --force             Force build even if no git changes detected"
            echo "  -h, --help              Show this help message"
            echo ""
            echo "Examples:"
            echo "  $0                                    # Build all packages (skips unchanged)"
            echo "  $0 -v 2.0.0                          # Build all packages with version 2.0.0"
            echo "  $0 Lyo.Encryption                    # Build specific package (skips unchanged)"
            echo "  $0 -v 1.5.0 Lyo.Encryption           # Build specific package with version"
            echo "  $0 -f Lyo.Encryption                 # Force build ignoring change detection"
            echo "  $0 Lyo.Encryption.*                  # Build all packages matching pattern"
            echo "  $0 Lyo.Encryption Lyo.Compression    # Build multiple packages"
            echo ""
            echo "Change detection:"
            echo "  Projects are fingerprinted via git (commits + dirty files + untracked files)."
            echo "  If a project's fingerprint hasn't changed since the last successful build,"
            echo "  it is skipped — no new .nupkg is emitted. State is stored in:"
            echo "  \$NUGET_OUTPUT_DIR/.build-state"
            echo ""
            echo "Environment variables:"
            echo "  NUGET_OUTPUT_DIR - Output directory (default: ~/nuget-local)"
            echo "  BUILD_CONFIG     - Build configuration (default: Release)"
            exit 0
            ;;
        *)
            PATTERNS+=("$1")
            shift
            ;;
    esac
done

# MSBuild /p: props so NuGet package version and built assembly metadata stay aligned (SDK derives AssemblyVersion from Version).
DOTNET_VERSION_MSBUILD_PROPS=( "/p:Version=$VERSION" "/p:InformationalVersion=$VERSION" "/p:FileVersion=$VERSION" )

# Configuration
NUGET_OUTPUT_DIR="${NUGET_OUTPUT_DIR:-$HOME/nuget-local}"
BUILD_CONFIG="${BUILD_CONFIG:-Release}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$SCRIPT_DIR"

# Path to the persistent build-state file (one line per project: "Lyo.Foo=<hash>")
BUILD_STATE_FILE="$NUGET_OUTPUT_DIR/.build-state"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m' # No Color

# Function to print colored output
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_skip() {
    echo -e "${CYAN}[SKIP]${NC} $1"
}

print_pack_only() {
    echo -e "${MAGENTA}[PACK]${NC} $1"
}

# ---------------------------------------------------------------------------
# Git change detection
# ---------------------------------------------------------------------------

# Compute a single fingerprint for a project directory. The hash changes when:
#   - a new commit touches any file under that directory
#   - staged or unstaged edits exist in that directory
#   - new untracked files are present in that directory
compute_project_hash() {
    local project_dir="$1"
    {
        # Last commit touching this directory
        git -C "$ROOT_DIR" log -1 --format="%H" -- "$project_dir" 2>/dev/null || echo "no-commits"

        # Full diff (staged + unstaged) against HEAD for this directory
        git -C "$ROOT_DIR" diff HEAD -- "$project_dir" 2>/dev/null

        # Names and content of untracked files in this directory
        while IFS= read -r f; do
            echo "untracked:$f"
            cat "$ROOT_DIR/$f" 2>/dev/null
        done < <(git -C "$ROOT_DIR" ls-files --others --exclude-standard -- "$project_dir" 2>/dev/null)
    } | sha256sum | cut -d' ' -f1
}

# State file format per line: "Lyo.Foo=<hash>:<version>"
# Hash covers the source fingerprint; version is the NuGet version used for the last pack.

# Read the stored source hash for a project from the state file.
get_stored_hash() {
    local project_name="$1"
    if [[ -f "$BUILD_STATE_FILE" ]]; then
        local entry
        entry=$(grep "^${project_name}=" "$BUILD_STATE_FILE" | tail -1 | cut -d'=' -f2-)
        echo "${entry%%:*}"
    fi
}

# Read the stored version for a project from the state file.
get_stored_version() {
    local project_name="$1"
    if [[ -f "$BUILD_STATE_FILE" ]]; then
        local entry
        entry=$(grep "^${project_name}=" "$BUILD_STATE_FILE" | tail -1 | cut -d'=' -f2-)
        echo "${entry#*:}"
    fi
}

# Persist the fingerprint + version for a project after a successful pack.
save_project_state() {
    local project_name="$1"
    local hash="$2"
    local version="$3"
    # Remove any previous entry for this project, then append the new one.
    if [[ -f "$BUILD_STATE_FILE" ]]; then
        local tmp
        tmp=$(mktemp)
        grep -v "^${project_name}=" "$BUILD_STATE_FILE" > "$tmp" || true
        mv "$tmp" "$BUILD_STATE_FILE"
    fi
    echo "${project_name}=${hash}:${version}" >> "$BUILD_STATE_FILE"
}

# Returns 0 if the project source has changed (needs a build), 1 if unchanged.
project_source_changed() {
    local project_name="$1"
    local project_dir="$2"
    local current_hash stored_hash
    current_hash=$(compute_project_hash "$project_dir")
    stored_hash=$(get_stored_hash "$project_name")
    [[ "$current_hash" != "$stored_hash" ]]
}

# Returns 0 if the requested version differs from the last packed version.
project_version_changed() {
    local project_name="$1"
    local stored_version
    stored_version=$(get_stored_version "$project_name")
    [[ "$VERSION" != "$stored_version" ]]
}

# ---------------------------------------------------------------------------
# Project discovery helpers
# ---------------------------------------------------------------------------

# Function to get a project name from a project file
get_project_name() {
    local project_file="$1"
    local base_name
    base_name=$(basename "$project_file")
    echo "${base_name%.*proj}"
}

# Function to find all project files matching the pattern
find_projects() {
    local pattern="$1"
    local projects=()
    
    # Convert pattern to regex (handle wildcards)
    local regex_pattern="${pattern//\*/.*}"
    
    # Find all .csproj and .fsproj files (excluding test, benchmark, and tool projects)
    while IFS= read -r -d '' file; do
        local filename
        filename=$(get_project_name "$file")
        local dirname=$(dirname "$file")
        
        # Skip test, benchmark, and tool projects
        if [[ "$filename" == *".Tests"* ]] || \
           [[ "$filename" == *".Benchmarks"* ]] || \
           [[ "$filename" == *"TestConsole"* ]] || \
           [[ "$filename" == "Lyo.TestConsole"* ]] || \
           [[ "$dirname" == *"/Tests/"* ]] || \
           [[ "$dirname" == *"/Benchmarks/"* ]] || \
           [[ "$dirname" == *"/Tools/"* ]] || \
           [[ "$dirname" == *"/Tools/Lyo.TestConsole"* ]]; then
            continue
        fi
        
        # Check if filename matches pattern
        if [[ "$filename" =~ ^${regex_pattern}$ ]]; then
            projects+=("$file")
        fi
    done < <(find "$ROOT_DIR" \( -name "*.csproj" -o -name "*.fsproj" \) -type f -print0 | sort -z)
    
    printf '%s\n' "${projects[@]}"
}

# Function to get full transitive closure of projects to build (requested + all Lyo deps)
get_full_build_set() {
    local projects=("$@")
    local result=()
    local seen=()
    
    # Recursive helper to add project and its deps
    add_with_deps() {
        local csproj_file="$1"
        
        # Skip if already in result
        for p in "${result[@]}"; do
            [[ "$p" == "$csproj_file" ]] && return
        done
        
        # Get dependencies first and add them
        local deps
        deps=$(get_project_dependencies "$csproj_file")
        while IFS= read -r dep; do
            if [[ -n "$dep" ]] && [[ -f "$dep" ]]; then
                add_with_deps "$dep"
            fi
        done <<< "$deps"
        
        result+=("$csproj_file")
    }
    
    for project in "${projects[@]}"; do
        [[ -n "$project" ]] && add_with_deps "$project"
    done
    
    printf '%s\n' "${result[@]}"
}

# Function to get project dependencies (Lyo.* projects)
get_project_dependencies() {
    local csproj_file="$1"
    local deps=()
    local project_dir=$(dirname "$csproj_file")
    
    # Extract ProjectReference includes that are Lyo.* projects
    while IFS= read -r line; do
        if [[ "$line" =~ ProjectReference.*Include=\"([^\"]+)\" ]]; then
            local ref_path="${BASH_REMATCH[1]//\\//}"
            # Resolve relative path from project directory
            local ref_full_path
            if [[ "$ref_path" == /* ]]; then
                # Absolute path
                ref_full_path="$ref_path"
            else
                # Relative path - resolve from project directory
                ref_full_path=$(cd "$project_dir" && cd "$(dirname "$ref_path")" && pwd)/$(basename "$ref_path")
            fi
            
            # Normalize path (try readlink -f, fallback to cd/pwd)
            if command -v readlink >/dev/null 2>&1; then
                ref_full_path=$(readlink -f "$ref_full_path" 2>/dev/null || echo "$ref_full_path")
            else
                # Fallback: use cd to resolve
                ref_full_path=$(cd "$(dirname "$ref_full_path")" && pwd)/$(basename "$ref_full_path")
            fi
            
            # Only include if it's a Lyo.* project and exists
            if [[ -f "$ref_full_path" ]] && [[ "$(get_project_name "$ref_full_path")" =~ ^Lyo\. ]]; then
                # Avoid duplicates
                local found=0
                for dep in "${deps[@]}"; do
                    if [[ "$dep" == "$ref_full_path" ]]; then
                        found=1
                        break
                    fi
                done
                if [[ $found -eq 0 ]]; then
                    deps+=("$ref_full_path")
                fi
            fi
        fi
    done < <(grep -E "ProjectReference.*Include=" "$csproj_file" || true)
    
    printf '%s\n' "${deps[@]}"
}

# ---------------------------------------------------------------------------
# Build / pack
# ---------------------------------------------------------------------------

# Full rebuild (--no-incremental) — used when source has changed or --force is set.
build_project() {
    local csproj_file="$1"
    local project_name
    project_name=$(get_project_name "$csproj_file")
    
    print_info "Building $project_name (version $VERSION)..."
    
    # --no-incremental forces a clean rebuild so DLLs always match the new source.
    # BuildProjectReferences=false skips rebuilding deps — the script already built
    # them in dependency order, so their DLLs are present and up to date.
    # Version must be set here (not only on pack) so assembly metadata stays aligned.
    if dotnet build "$csproj_file" -c "$BUILD_CONFIG" --no-incremental /p:BuildProjectReferences=false "${DOTNET_VERSION_MSBUILD_PROPS[@]}"; then
        print_success "Built $project_name"
        return 0
    else
        print_error "Failed to build $project_name"
        return 1
    fi
}

# Incremental build — used when only the package version changed (same sources).
# Ensures AssemblyVersion/FileVersion reflect /p:Version before dotnet pack --no-build.
build_project_incremental() {
    local csproj_file="$1"
    local project_name
    project_name=$(get_project_name "$csproj_file")
    
    print_info "Building $project_name (incremental, version $VERSION)..."
    
    if dotnet build "$csproj_file" -c "$BUILD_CONFIG" /p:BuildProjectReferences=false "${DOTNET_VERSION_MSBUILD_PROPS[@]}"; then
        print_success "Built $project_name (incremental)"
        return 0
    else
        print_error "Failed to build $project_name"
        return 1
    fi
}

# Function to pack a project
pack_project() {
    local csproj_file="$1"
    local project_name
    project_name=$(get_project_name "$csproj_file")
    
    print_info "Packing $project_name (version $VERSION)..."
    
    if dotnet pack "$csproj_file" -c "$BUILD_CONFIG" --no-build --output "$NUGET_OUTPUT_DIR" "${DOTNET_VERSION_MSBUILD_PROPS[@]}" /p:BuildProjectReferences=false; then
        print_success "Packed $project_name"
        return 0
    else
        print_error "Failed to pack $project_name"
        return 1
    fi
}

# ---------------------------------------------------------------------------
# Visited / skipped project tracking
# ---------------------------------------------------------------------------

# Global visited projects tracker (prevents double-processing in recursive deps)
VISITED_PROJECTS=()

# Global failed projects tracker (prevents retrying a project that already failed
# when it is reached again via a different dependent's dependency chain)
FAILED_PROJECT_PATHS=()

# Global skipped projects tracker (for summary)
SKIPPED_PROJECTS=()

# Global rebuilt-for-version projects tracker (source unchanged, version changed — incremental build then pack)
PACK_ONLY_PROJECTS=()

# Global built projects tracker (for summary)
BUILT_PROJECTS=()

# Function to check if project was already processed (success or failure)
is_visited() {
    local csproj_file="$1"
    for visited in "${VISITED_PROJECTS[@]}"; do
        [[ "$visited" == "$csproj_file" ]] && return 0
    done
    return 1
}

# Function to mark project as visited
mark_visited() {
    local csproj_file="$1"
    VISITED_PROJECTS+=("$csproj_file")
}

# Function to check if project previously failed this session
is_failed() {
    local csproj_file="$1"
    for f in "${FAILED_PROJECT_PATHS[@]}"; do
        [[ "$f" == "$csproj_file" ]] && return 0
    done
    return 1
}

# Function to record a project as failed (also marks it visited to prevent retries)
mark_failed() {
    local csproj_file="$1"
    FAILED_PROJECT_PATHS+=("$csproj_file")
    mark_visited "$csproj_file"
}

# ---------------------------------------------------------------------------
# Core build-and-pack loop
# ---------------------------------------------------------------------------

# Build and pack a project with its dependencies (depth-first, deps first).
# Three outcomes per project (unless --force):
#   source changed             → full rebuild (--no-incremental) + pack
#   source unchanged, new ver  → incremental build with VERSION props + pack (must rebuild — pack --no-build cannot refresh assembly metadata)
#   source unchanged, same ver → skip entirely
build_and_pack_with_deps() {
    local csproj_file="$1"
    local project_name project_dir
    project_name=$(get_project_name "$csproj_file")
    project_dir=$(dirname "$csproj_file")

    # Already processed this session — success or failure, don't retry
    if is_visited "$csproj_file"; then
        # Propagate failure if this project previously failed
        is_failed "$csproj_file" && return 1
        return 0
    fi

    # Recurse into Lyo dependencies first
    local deps
    deps=$(get_project_dependencies "$csproj_file")
    if [[ -n "$deps" ]]; then
        while IFS= read -r dep; do
            if [[ -n "$dep" ]] && [[ -f "$dep" ]]; then
                if ! build_and_pack_with_deps "$dep"; then
                    local dep_name
                    dep_name=$(get_project_name "$dep")
                    print_error "Failed to build dependency: $dep_name"
                    mark_failed "$csproj_file"
                    return 1
                fi
            fi
        done <<< "$deps"
    fi

    # Determine what work is needed (unless --force, which always does a full build+pack)
    local source_changed=1 version_changed=1
    if [[ $FORCE -eq 0 ]]; then
        project_source_changed  "$project_name" "$project_dir" || source_changed=0
        project_version_changed "$project_name"                 || version_changed=0
    fi

    if [[ $source_changed -eq 0 && $version_changed -eq 0 ]]; then
        # Nothing changed at all — skip entirely
        print_skip "$project_name — source and version unchanged"
        SKIPPED_PROJECTS+=("$project_name")
        mark_visited "$csproj_file"
        return 0
    fi

    if [[ $source_changed -eq 1 ]]; then
        # Source changed (or --force) — full rebuild so DLLs reflect the new code
        if ! build_project "$csproj_file"; then
            mark_failed "$csproj_file"
            return 1
        fi
    else
        # Source unchanged, only version changed — must rebuild so DLL assembly metadata matches /p:Version before dotnet pack --no-build.
        print_pack_only "$project_name — source unchanged, rebuilding for new version $VERSION then packing"
        if ! build_project_incremental "$csproj_file"; then
            mark_failed "$csproj_file"
            return 1
        fi
    fi

    if ! pack_project "$csproj_file"; then
        mark_failed "$csproj_file"
        return 1
    fi

    # Persist state so future runs know the fingerprint and version we just packed
    save_project_state "$project_name" "$(compute_project_hash "$project_dir")" "$VERSION"

    if [[ $source_changed -eq 1 ]]; then
        BUILT_PROJECTS+=("$project_name")
    else
        PACK_ONLY_PROJECTS+=("$project_name")
    fi
    mark_visited "$csproj_file"
    return 0
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

main() {
    print_info "Lyo NuGet Package Builder"
    print_info "Output directory: $NUGET_OUTPUT_DIR"
    print_info "Build configuration: $BUILD_CONFIG"
    print_info "Package version: $VERSION"
    if [[ $FORCE -eq 1 ]]; then
        print_warning "Change detection disabled (--force)"
    fi
    echo ""

    # Ensure output directory (and therefore the state file location) exists
    mkdir -p "$NUGET_OUTPUT_DIR"

    # Determine which projects to build
    local projects_to_build=()

    if [[ ${#PATTERNS[@]} -eq 0 ]]; then
        # No arguments - build all packages (Lyo.*)
        print_info "No pattern specified, building all packages..."
        while IFS= read -r project; do
            [[ -n "$project" ]] && projects_to_build+=("$project")
        done < <(find_projects "Lyo.*")
    else
        # Build projects matching provided patterns
        for pattern in "${PATTERNS[@]}"; do
            print_info "Finding projects matching pattern: $pattern"
            while IFS= read -r project; do
                if [[ -n "$project" ]]; then
                    # Avoid duplicates
                    local found=0
                    for existing in "${projects_to_build[@]}"; do
                        if [[ "$existing" == "$project" ]]; then
                            found=1
                            break
                        fi
                    done
                    if [[ $found -eq 0 ]]; then
                        projects_to_build+=("$project")
                    fi
                fi
            done < <(find_projects "$pattern")
        done
    fi

    if [[ ${#projects_to_build[@]} -eq 0 ]]; then
        print_warning "No projects found matching the specified pattern(s)"
        exit 1
    fi

    # Compute full build set (requested projects + transitive Lyo deps)
    FULL_BUILD_SET=()
    while IFS= read -r project; do
        [[ -n "$project" ]] && FULL_BUILD_SET+=("$(get_project_name "$project")")
    done < <(get_full_build_set "${projects_to_build[@]}")

    print_info "Found ${#projects_to_build[@]} project(s) to evaluate (${#FULL_BUILD_SET[@]} including deps):"
    for project in "${projects_to_build[@]}"; do
        echo "  - $(get_project_name "$project")"
    done
    echo ""

    # Build and pack each project
    local failed_projects=()

    for project in "${projects_to_build[@]}"; do
        # Skip if already processed (as a dependency of an earlier top-level project)
        if is_visited "$project"; then
            continue
        fi

        if ! build_and_pack_with_deps "$project"; then
            failed_projects+=("$(get_project_name "$project")")
            print_warning "Skipping remaining dependents of $(get_project_name "$project")"
        fi
    done

    # Summary
    echo ""
    print_info "Build Summary:"
    print_success  "Built and packed (source changed): ${#BUILT_PROJECTS[@]} package(s)"
    print_pack_only "Rebuild + pack (new version, same source): ${#PACK_ONLY_PROJECTS[@]} package(s)"
    print_skip     "Skipped (source and version unchanged): ${#SKIPPED_PROJECTS[@]} package(s)"

    if [[ ${#PACK_ONLY_PROJECTS[@]} -gt 0 ]]; then
        for name in "${PACK_ONLY_PROJECTS[@]}"; do
            echo "  - $name"
        done
    fi
    if [[ ${#SKIPPED_PROJECTS[@]} -gt 0 ]]; then
        for name in "${SKIPPED_PROJECTS[@]}"; do
            echo "  - $name"
        done
    fi

    if [[ ${#failed_projects[@]} -gt 0 ]]; then
        echo ""
        print_warning "Failed to build:"
        for failed in "${failed_projects[@]}"; do
            echo "  - $failed"
        done
        print_info "Note: This may be due to SDK version mismatches (e.g., targeting net10.0 with SDK 8.0)"
        print_info "Packages are available in: $NUGET_OUTPUT_DIR"
        exit 1
    else
        print_success "Done! Packages are available in: $NUGET_OUTPUT_DIR"
        if [[ $FORCE -eq 0 ]]; then
            print_info "Tip: run with -f / --force to rebuild all packages regardless of changes."
        fi
        exit 0
    fi
}

# Run main function (arguments already parsed into VERSION, FORCE, and PATTERNS)
main
