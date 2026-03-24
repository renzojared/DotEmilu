$ErrorActionPreference = "Stop"

$BaseUrl = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5009" }
$Resource = "$BaseUrl/invoices"

Write-Host ""
Write-Host "== DotEmilu FullApp smoke test (PowerShell) =="
Write-Host "Base URL: $BaseUrl"
Write-Host ""
Write-Host "Make sure API is running first:"
Write-Host "dotnet run --project samples/DotEmilu.Samples.FullApp"
Write-Host ""

$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$number = "INV-SMOKE-$timestamp"
$updatedNumber = if ($number.Length -gt 18) { $number.Substring(0, 18) + "-U" } else { $number + "-U" }

Write-Host "1) CREATE invoice"
$createBody = @{
    number = $number
    description = "Smoke test invoice"
    amount = 123.45
    date = "2026-03-23"
} | ConvertTo-Json

$createResponse = Invoke-WebRequest `
    -Method Post `
    -Uri $Resource `
    -ContentType "application/json" `
    -Body $createBody
Write-Host "   Status: $($createResponse.StatusCode)"

Write-Host "2) LIST invoices"
$list = Invoke-RestMethod -Method Get -Uri "$Resource?pageNumber=1&pageSize=50"
$list | ConvertTo-Json -Depth 8

$items = if ($null -ne $list.Items) { $list.Items } elseif ($null -ne $list.items) { $list.items } else { @() }
if ($items.Count -eq 0) {
    throw "No invoice id found in list response."
}

$invoice = $items | Sort-Object -Property Id -Descending | Select-Object -First 1
$invoiceId = $invoice.Id
Write-Host "   Picked invoice id: $invoiceId"

Write-Host "3) GET by id"
$one = Invoke-RestMethod -Method Get -Uri "$Resource/$invoiceId"
$one | ConvertTo-Json -Depth 8

Write-Host "4) CONFIRM invoice"
$confirmBody = @{
    confirmationNotes = "Looks good to me"
} | ConvertTo-Json

$confirmResponse = Invoke-WebRequest `
    -Method Post `
    -Uri "$Resource/$invoiceId/confirm" `
    -ContentType "application/json" `
    -Body $confirmBody
Write-Host $($confirmResponse.Content)
Write-Host "   Status: $($confirmResponse.StatusCode)"

Write-Host "4.1) GET confirmed invoice"
$confirmed = Invoke-RestMethod -Method Get -Uri "$Resource/$invoiceId"
$confirmed | ConvertTo-Json -Depth 8

Write-Host "5) UPDATE invoice"
$updateBody = @{
    number = $updatedNumber
    description = "Smoke test invoice updated"
    amount = 150.00
    date = "2026-03-23"
    isPaid = $true
} | ConvertTo-Json

$updateResponse = Invoke-WebRequest `
    -Method Put `
    -Uri "$Resource/$invoiceId" `
    -ContentType "application/json" `
    -Body $updateBody
Write-Host "   Status: $($updateResponse.StatusCode)"

Write-Host "6) GET updated invoice"
$updated = Invoke-RestMethod -Method Get -Uri "$Resource/$invoiceId"
$updated | ConvertTo-Json -Depth 8

Write-Host "7) SYNC invoices"
$syncBody = @{
    invoiceIds = @($invoiceId)
} | ConvertTo-Json -Depth 2

$syncResponse = Invoke-WebRequest `
    -Method Post `
    -Uri "$Resource/sync" `
    -ContentType "application/json" `
    -Body $syncBody
Write-Host $($syncResponse.Content)
Write-Host "   Status: $($syncResponse.StatusCode)"

Write-Host "7.1) GET synced invoice"
$synced = Invoke-RestMethod -Method Get -Uri "$Resource/$invoiceId"
$synced | ConvertTo-Json -Depth 8

Write-Host "8) DELETE invoice (soft-delete)"
$deleteResponse = Invoke-WebRequest -Method Delete -Uri "$Resource/$invoiceId"
Write-Host "   Status: $($deleteResponse.StatusCode)"

Write-Host "9) GET deleted invoice (should be 404)"
try {
    Invoke-WebRequest -Method Get -Uri "$Resource/$invoiceId" | Out-Null
    Write-Host "   Status: 200 (unexpected)"
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    $content = ""
    try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $content = $reader.ReadToEnd()
    }
    catch {}
    Write-Host $content
    Write-Host "   Status: $statusCode"
}

Write-Host "10) LIST invoices after delete"
$after = Invoke-RestMethod -Method Get -Uri "$Resource?pageNumber=1&pageSize=50"
$after | ConvertTo-Json -Depth 8

Write-Host ""
Write-Host "Smoke test finished."
