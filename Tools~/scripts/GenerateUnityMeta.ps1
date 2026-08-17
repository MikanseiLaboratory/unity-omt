param(
  [Parameter(Mandatory = $true)]
  [string]$PackageRoot
)

$PackageRoot = (Resolve-Path $PackageRoot).Path

function Get-StableGuid([string]$relative) {
  $md5 = [System.Security.Cryptography.MD5]::Create()
  try {
    $bytes = $md5.ComputeHash([Text.Encoding]::UTF8.GetBytes("omt-unity:" + $relative.Replace('\','/')))
    return ([guid]$bytes).ToString("N")
  } finally {
    $md5.Dispose()
  }
}

function Write-Meta([string]$path, [string]$contents) {
  if (-not (Test-Path $path)) {
    Set-Content -Path $path -Value $contents.TrimEnd() -Encoding utf8
  }
}

$folderMeta = @"
fileFormatVersion: 2
guid: {0}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

$scriptMeta = @"
fileFormatVersion: 2
guid: {0}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: {1}
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

$asmdefMeta = @"
fileFormatVersion: 2
guid: {0}
AssemblyDefinitionImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

$computeMeta = @"
fileFormatVersion: 2
guid: {0}
ComputeShaderImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

$defaultMeta = @"
fileFormatVersion: 2
guid: {0}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

$textMeta = @"
fileFormatVersion: 2
guid: {0}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

$winPluginMeta = @"
fileFormatVersion: 2
guid: {0}
PluginImporter:
  externalObjects: {{}}
  serializedVersion: 2
  iconMap: {{}}
  executionOrder: {{}}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      : Any
    second:
      enabled: 0
      settings:
        Exclude Editor: 0
        Exclude Linux64: 1
        Exclude OSXUniversal: 1
        Exclude Win: 1
        Exclude Win64: 0
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: x86_64
        DefaultValueInitialized: true
        OS: Windows
  - first:
      Standalone: Win64
    second:
      enabled: 1
      settings:
        CPU: x86_64
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

$macPluginMeta = @"
fileFormatVersion: 2
guid: {0}
PluginImporter:
  externalObjects: {{}}
  serializedVersion: 2
  iconMap: {{}}
  executionOrder: {{}}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      : Any
    second:
      enabled: 0
      settings:
        Exclude Editor: 0
        Exclude Linux64: 1
        Exclude OSXUniversal: 0
        Exclude Win: 1
        Exclude Win64: 1
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: AnyCPU
        DefaultValueInitialized: true
        OS: OSX
  - first:
      Standalone: OSXUniversal
    second:
      enabled: 1
      settings:
        CPU: AnyCPU
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

Get-ChildItem $PackageRoot -Recurse -Directory | Where-Object { $_.FullName -notmatch "Samples~" } | ForEach-Object {
  $rel = $_.FullName.Substring($PackageRoot.Length).TrimStart('\','/').Replace('\','/')
  Write-Meta ($_.FullName + ".meta") ($folderMeta -f (Get-StableGuid $rel))
}

$resourceGuid = Get-StableGuid "Runtime/Resources/OmtResources.asset"
$encodeGuid = Get-StableGuid "Runtime/Resources/OmtEncode.compute"
$decodeGuid = Get-StableGuid "Runtime/Resources/OmtDecode.compute"
$resourcesScriptGuid = Get-StableGuid "Runtime/Internal/OmtResources.cs"

Get-ChildItem $PackageRoot -Recurse -File | Where-Object {
  $_.FullName -notmatch "Samples~" -and $_.Extension -ne ".meta"
} | ForEach-Object {
  $rel = $_.FullName.Substring($PackageRoot.Length).TrimStart('\','/').Replace('\','/')
  $guid = Get-StableGuid $rel
  $metaPath = $_.FullName + ".meta"
  switch ($_.Extension) {
    ".cs" {
      $defaults = "[]"
      if ($_.Name -eq "OmtSender.cs" -or $_.Name -eq "OmtReceiver.cs") {
        $defaults = ("`n  - _resources`n  - {{fileID: 11400000, guid: {0}, type: 2}}" -f $resourceGuid)
      }
      Write-Meta $metaPath ($scriptMeta -f $guid, $defaults)
    }
    ".asmdef" { Write-Meta $metaPath ($asmdefMeta -f $guid) }
    ".compute" { Write-Meta $metaPath ($computeMeta -f $guid) }
    ".json" { Write-Meta $metaPath ($textMeta -f $guid) }
    ".md" { Write-Meta $metaPath ($textMeta -f $guid) }
    ".dll" { Write-Meta $metaPath ($winPluginMeta -f $guid) }
    ".dylib" { Write-Meta $metaPath ($macPluginMeta -f $guid) }
    default { Write-Meta $metaPath ($defaultMeta -f $guid) }
  }
}

$asset = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $resourcesScriptGuid, type: 3}
  m_Name: OmtResources
  encoderCompute: {fileID: 7200000, guid: $encodeGuid, type: 3}
  decoderCompute: {fileID: 7200000, guid: $decodeGuid, type: 3}
"@
Set-Content -Path (Join-Path $PackageRoot "Runtime\Resources\OmtResources.asset") -Value $asset.TrimEnd() -Encoding utf8
Write-Meta (Join-Path $PackageRoot "Runtime\Resources\OmtResources.asset.meta") ($defaultMeta -f $resourceGuid)
Write-Host "Generated Unity .meta files under $PackageRoot"
