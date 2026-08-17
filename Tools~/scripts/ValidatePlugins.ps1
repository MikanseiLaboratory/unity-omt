param(
  [Parameter(Mandatory = $true)]
  [string]$PackageRoot
)

$PackageRoot = (Resolve-Path $PackageRoot).Path
$expected = @{
  "Runtime/Plugins/macOS/libomt.dylib" = "bf8ce200fd150b6453a5bad079bd0e392f3cd6eeb0a0934cbee9d0bc0c0ae7ec"
  "Runtime/Plugins/macOS/libvmx.dylib" = "09814f17948dec3012ace953a85df697a841c842aabbd48cc4aa4b797acfebe2"
  "Runtime/Plugins/Windows/x86_64/libomt.dll" = "83830687a9eb79630af16f8b1cb1cd5b2f6c36423fa0bdfd40ec7f5ed90a448d"
  "Runtime/Plugins/Windows/x86_64/libvmx.dll" = "a33167041939bce24729343963ca8fca373b5878a36ea475a76c2393c48b70ce"
}

$failed = $false
foreach ($rel in $expected.Keys) {
  $path = Join-Path $PackageRoot ($rel.Replace('/', [IO.Path]::DirectorySeparatorChar))
  if (-not (Test-Path $path)) {
    Write-Error "Missing $rel"
    $failed = $true
    continue
  }
  $hash = (Get-FileHash $path -Algorithm SHA256).Hash.ToLower()
  if ($hash -ne $expected[$rel]) {
    Write-Error "Hash mismatch $rel actual=$hash"
    $failed = $true
  } else {
    Write-Host "OK $rel"
  }
}

$winDll = Join-Path $PackageRoot "Runtime\Plugins\Windows\x86_64\libomt.dll"
$bytes = [IO.File]::ReadAllBytes($winDll)
$pe = [BitConverter]::ToInt32($bytes, 0x3C)
$machine = [BitConverter]::ToUInt16($bytes, $pe + 4)
if ($machine -ne 0x8664) {
  Write-Error "libomt.dll is not x64 (machine=$machine)"
  $failed = $true
} else {
  Write-Host "OK libomt.dll machine=x64"
}

if ($failed) { exit 1 }
Write-Host "Plugin validation passed"
