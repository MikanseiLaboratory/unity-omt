param(
  [Parameter(Mandatory = $true)]
  [string]$PackageRoot
)

$PackageRoot = (Resolve-Path $PackageRoot).Path
$tmp = Join-Path (Split-Path $PSScriptRoot -Parent) "tmp"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
gh release download v1.0.0.16 --repo openmediatransport/libomtnet --pattern "*.zip" --dir $tmp --clobber
$zip = Get-ChildItem $tmp -Filter *.zip | Select-Object -First 1
$extract = Join-Path $tmp "extract"
if (Test-Path $extract) { Remove-Item $extract -Recurse -Force }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($zip.FullName, $extract)

Copy-Item "$extract\Libraries\Winx64\libomt.dll" "$PackageRoot\Runtime\Plugins\Windows\x86_64\libomt.dll" -Force
Copy-Item "$extract\Libraries\Winx64\libvmx.dll" "$PackageRoot\Runtime\Plugins\Windows\x86_64\libvmx.dll" -Force
Copy-Item "$extract\Libraries\MacOS\libomt.dylib" "$PackageRoot\Runtime\Plugins\macOS\libomt.dylib" -Force
Copy-Item "$extract\Libraries\MacOS\libvmx.dylib" "$PackageRoot\Runtime\Plugins\macOS\libvmx.dylib" -Force

& "$PSScriptRoot\ValidatePlugins.ps1" -PackageRoot $PackageRoot
