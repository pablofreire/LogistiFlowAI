<#
.SYNOPSIS
  Drives the first business flow through the gateway of a running stack and prints the evidence.
.EXAMPLE
  docker compose --profile app up -d --build --wait
  powershell -File scripts/demo-flow.ps1
#>
param(
  [string]$Gateway = 'http://localhost:8089',
  [string]$Password = $(if ($env:DEMO_PASSWORD) { $env:DEMO_PASSWORD } else { 'Demo-Password-2026!' })
)
$ErrorActionPreference = 'Stop'

function Invoke-Api([string]$Method, [string]$Path, $Body = $null, [hashtable]$Headers = @{}) {
  $parameters = @{ Uri = "$Gateway$Path"; Method = $Method; Headers = $Headers; ContentType = 'application/json' }
  if ($null -ne $Body) { $parameters.Body = ($Body | ConvertTo-Json -Depth 5) }
  (Invoke-RestMethod @parameters).data
}

function Wait-Order([string]$OrderId, [string[]]$FinalStatuses, [hashtable]$Headers) {
  $deadline = (Get-Date).AddSeconds(30)
  do {
    $order = Invoke-Api GET "/api/orders/$OrderId" -Headers $Headers
    if ($order.status -in $FinalStatuses) { return $order }
    Start-Sleep -Milliseconds 300
  } while ((Get-Date) -lt $deadline)
  throw "Order $OrderId did not reach $($FinalStatuses -join '/') in time (last status: $($order.status))."
}

$login = Invoke-Api POST '/api/auth/login' @{ email = 'ops.manager@logistiflow.local'; password = $Password }
$auth = @{ Authorization = "Bearer $($login.accessToken)" }
Write-Host "Signed in as $($login.user.displayName) ($($login.user.role))"

$before = Invoke-Api GET '/api/dashboard/summary' -Headers $auth
$key = [Guid]::NewGuid().ToString('N')
$body = @{ customerName = 'Demo Retail'; destinationCity = 'Austin'; destinationCountry = 'US'; lines = @(@{ sku = 'SKU-1001'; quantity = 2 }) }
$order = Invoke-Api POST '/api/orders' $body ($auth + @{ 'Idempotency-Key' = $key })
$replay = Invoke-Api POST '/api/orders' $body ($auth + @{ 'Idempotency-Key' = $key })
Write-Host "Created $($order.orderNumber) [$($order.status)]; replay with the same key returned $($replay.orderNumber)"

$final = Wait-Order $order.id @('Picking', 'Failed') $auth
Write-Host "Final status: $($final.status) - $($final.statusReason)"
Write-Host 'Timeline:'
Invoke-Api GET "/api/orders/$($order.id)/history" -Headers $auth | ForEach-Object { Write-Host "  $($_.fromStatus) -> $($_.toStatus)  [$($_.source)]  $($_.reason)" }

$failed = Invoke-Api POST '/api/orders' @{ customerName = 'Demo Retail'; destinationCity = 'Austin'; destinationCountry = 'US'; lines = @(@{ sku = 'SKU-OOS-001'; quantity = 1 }) } ($auth + @{ 'Idempotency-Key' = [Guid]::NewGuid().ToString('N') })
$failedFinal = Wait-Order $failed.id @('Failed', 'Picking') $auth
Write-Host "Out-of-stock order $($failed.orderNumber): $($failedFinal.status) - $($failedFinal.statusReason)"

$after = Invoke-Api GET '/api/dashboard/summary' -Headers $auth
Write-Host "Dashboard: orders $($before.orders.totalOrders) -> $($after.orders.totalOrders), fulfillment jobs $($before.fulfillment.totalJobs) -> $($after.fulfillment.totalJobs), unavailable sources: [$($after.unavailableSources -join ', ')]"
