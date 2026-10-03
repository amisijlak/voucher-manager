#Requires -Version 5.1
param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [string]$CertSubject = 'CN=VoucherMgt Local Dev'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path)) {
    exit 0
}

$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq $CertSubject -and $_.NotAfter -gt (Get-Date) } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $cert) {
    $cert = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $CertSubject `
        -CertStoreLocation 'Cert:\CurrentUser\My' `
        -KeyExportPolicy Exportable `
        -NotAfter (Get-Date).AddYears(5)
    Write-Host "Created code-signing certificate: $($cert.Thumbprint)"
}

$signature = Set-AuthenticodeSignature -FilePath $Path -Certificate $cert
Write-Host "Signed $(Split-Path $Path -Leaf) => $($signature.Status)"

if ($signature.Status -notin @('Valid', 'UnknownError')) {
    Write-Warning "Authenticode status $($signature.Status): $($signature.StatusMessage)"
    exit 1
}
