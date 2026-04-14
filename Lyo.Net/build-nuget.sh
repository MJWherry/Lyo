#!/bin/bash

# Build and pack NuGet packages for Lyo libraries
# Usage:
#   ./build-nuget.sh                    # Build all packages
#   ./build-nuget.sh -v 2.0.0           # Build all packages with version 2.0.0
#   ./build-nuget.sh Lyo.Encryption     # Build specific package
#   ./build-nuget.sh -v 1.5.0 Lyo.Encryption  # Build specific package with version
#   ./build-nuget.sh -v 1.0.27 Lyo.Sms.Twilio  # Build only SMS packages + deps at 1.0.27
#
# When building specific packages with -v, only that package and its Lyo deps are built.
# Packaging uses SDK-style `dotnet pack`; project/package dependencies come from the project graph,
# not from hand-authored nuspec file lists.
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
PATTERNS=()

while [[ $# -gt 0 ]]; do
    case $1 in
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        -h|--help)
            echo "Build and pack NuGet packages for Lyo libraries"
            echo ""
            echo "Usage:"
            echo "  $0 [OPTIONS] [PATTERN...]"
            echo ""
            echo "Options:"
            echo "  -v, --version VERSION    Specify package version (default: 1.0.0)"
            echo "  -h, --help              Show this help message"
            echo ""
            echo "Examples:"
            echo "  $0                                    # Build all packages (version 1.0.0)"
            echo "  $0 -v 2.0.0                          # Build all packages with version 2.0.0"
            echo "  $0 Lyo.Encryption                    # Build specific package"
            echo "  $0 -v 1.5.0 Lyo.Encryption           # Build specific package with version"
            echo "  $0 Lyo.Encryption.*                  # Build all packages matching pattern"
            echo "  $0 Lyo.Encryption Lyo.Compression    # Build multiple packages"
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

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
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

# Function to build a project
build_project() {
    local csproj_file="$1"
    local project_name
    project_name=$(get_project_name "$csproj_file")
    
    print_info "Building $project_name (version $VERSION)..."
    
    # Build with output visible on error — Version must be set here (not only on pack) so DLLs match the .nupkg.
    # AssemblyVersion follows Version (numeric base for semver prereleases) via the .NET SDK.
    if dotnet build "$csproj_file" -c "$BUILD_CONFIG" --no-incremental "${DOTNET_VERSION_MSBUILD_PROPS[@]}"; then
        print_success "Built $project_name"
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
    
    # Force SDK-style pack so package contents come from the project, not nuspec file globs.
    if dotnet pack "$csproj_file" -c "$BUILD_CONFIG" --no-build --output "$NUGET_OUTPUT_DIR" "${DOTNET_VERSION_MSBUILD_PROPS[@]}" /p:BuildProjectReferences=false /p:NuspecFile=; then
        print_success "Packed $project_name"
        return 0
    else
        print_error "Failed to pack $project_name"
        return 1
    fi
}

# Global visited projects tracker
VISITED_PROJECTS=()

# Function to check if project was already processed
is_visited() {
    local csproj_file="$1"
    for visited in "${VISITED_PROJECTS[@]}"; do
        if [[ "$visited" == "$csproj_file" ]]; then
            return 0
        fi
    done
    return 1
}

# Function to mark project as visited
mark_visited() {
    local csproj_file="$1"
    VISITED_PROJECTS+=("$csproj_file")
}

# Function to build and pack a project with its dependencies
build_and_pack_with_deps() {
    local csproj_file="$1"
    local project_name
    project_name=$(get_project_name "$csproj_file")
    
    # Check if already processed
    if is_visited "$csproj_file"; then
        return 0
    fi
    
    # Get dependencies
    local deps
    deps=$(get_project_dependencies "$csproj_file")
    
    # Build dependencies first
    if [[ -n "$deps" ]]; then
        while IFS= read -r dep; do
            if [[ -n "$dep" ]] && [[ -f "$dep" ]]; then
                if ! build_and_pack_with_deps "$dep"; then
                    print_error "Failed to build dependency: $dep"
                    return 1
                fi
            fi
        done <<< "$deps"
    fi
    
    # Build and pack this project
    if ! build_project "$csproj_file"; then
        return 1
    fi
    
    if ! pack_project "$csproj_file"; then
        return 1
    fi
    
    # Mark as visited
    mark_visited "$csproj_file"
    
    return 0
}

# Main function
main() {
    print_info "Lyo NuGet Package Builder"
    print_info "Output directory: $NUGET_OUTPUT_DIR"
    print_info "Build configuration: $BUILD_CONFIG"
    print_info "Package version: $VERSION"
    echo ""
    
    # Create output directory if it doesn't exist
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
    
    print_info "Found ${#projects_to_build[@]} project(s) to build (${#FULL_BUILD_SET[@]} including deps):"
    for project in "${projects_to_build[@]}"; do
        echo "  - $(get_project_name "$project")"
    done
    echo ""
    
    # Build and pack each project
    local failed_projects=()
    local success_count=0
    
    for project in "${projects_to_build[@]}"; do
        # Skip if already processed (as a dependency)
        if is_visited "$project"; then
            continue
        fi
        
        if build_and_pack_with_deps "$project"; then
            ((success_count++))
        else
            failed_projects+=("$(get_project_name "$project")")
            print_warning "Skipping remaining dependencies of $(get_project_name "$project")"
        fi
    done
    
    # Summary
    echo ""
    print_info "Build Summary:"
    print_success "Successfully built and packed: $success_count package(s)"
    
    if [[ ${#failed_projects[@]} -gt 0 ]]; then
        print_warning "Some projects failed to build:"
        for failed in "${failed_projects[@]}"; do
            echo "  - $failed"
        done
        print_info "Note: This may be due to SDK version mismatches (e.g., targeting net10.0 with SDK 8.0)"
        print_info "Successfully built and packed: $success_count package(s)"
        print_info "Packages are available in: $NUGET_OUTPUT_DIR"
        exit 1
    else
        print_success "All packages built and packed successfully!"
        print_info "Packages are available in: $NUGET_OUTPUT_DIR"
        exit 0
    fi
}

# Run main function (arguments already parsed into VERSION and PATTERNS)
main

