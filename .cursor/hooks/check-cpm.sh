#!/bin/bash
# CPM guard: this repo uses Central Package Management (Lyo.Net/Directory.Packages.props).
# PackageReference/ProjectReference entries in .csproj files must not carry a Version attribute.
# Runs on afterFileEdit; exits 2 with a message when a violation is found in the edited csproj.

input=$(cat)
file_path=$(echo "$input" | jq -r '.file_path // .filePath // empty')

[[ -z "$file_path" || "$file_path" != *.csproj || ! -f "$file_path" ]] && exit 0

violations=$(grep -nE '<(Package|Project)Reference[^>]*\sVersion=' "$file_path" || true)

[[ -z "$violations" ]] && exit 0

cat >&2 <<EOF
CPM violation in $file_path — this repo uses Central Package Management.
Remove the Version attribute from these references and declare the version in Lyo.Net/Directory.Packages.props instead (as <PackageVersion Include="..." Version="[x.y.z,)"/>):
$violations
EOF
exit 2
