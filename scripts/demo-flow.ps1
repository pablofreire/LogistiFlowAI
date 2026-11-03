<#
.SYNOPSIS
  Drives an order through the gateway of a running stack, from intake to delivery by the simulated carrier, plus an
  out-of-stock order, and prints the evidence.
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

function Wait-Order([string]$OrderId, [string[]]$FinalStatuses, [hashtable]$Headers, [int]$TimeoutSeconds = 30) {
  $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
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

$picking = Wait-Order $order.id @('Picking', 'Failed') $auth
Write-Host "Reached $($picking.status) - $($picking.statusReason)"

$final = $picking
if ($picking.status -eq 'Picking') {
  $operatorLogin = Invoke-Api POST '/api/auth/login' @{ email = 'warehouse.operator@logistiflow.local'; password = $Password }
  $operator = @{ Authorization = "Bearer $($operatorLogin.accessToken)" }
  $job = Invoke-Api GET "/api/fulfillment/jobs/by-order/$($order.id)" -Headers $operator
  foreach ($action in 'pack', 'ship') { [void](Invoke-Api POST "/api/fulfillment/jobs/$($job.id)/$action" -Headers $operator) }
  Write-Host "$($operatorLogin.user.displayName) packed and shipped job $($job.id) from $($job.warehouseCode)"

  # The carrier simulator reports one tracking step every few seconds.
  $final = Wait-Order $order.id @('Delivered', 'Failed') $auth -TimeoutSeconds 90
  $shipment = Invoke-Api GET "/api/shipping/shipments/by-order/$($order.id)" -Headers $auth
  Write-Host "Shipment $($shipment.trackingNumber) with $($shipment.carrierName): $($shipment.status)"
  $shipment.trackingEvents | ForEach-Object { Write-Host "  $($_.code)  $($_.description)" }
}

Write-Host "Final status: $($final.status) - $($final.statusReason)"
Write-Host 'Timeline:'
Invoke-Api GET "/api/orders/$($order.id)/history" -Headers $auth | ForEach-Object { Write-Host "  $($_.fromStatus) -> $($_.toStatus)  [$($_.source)]  $($_.reason)" }

$failed = Invoke-Api POST '/api/orders' @{ customerName = 'Demo Retail'; destinationCity = 'Austin'; destinationCountry = 'US'; lines = @(@{ sku = 'SKU-OOS-001'; quantity = 1 }) } ($auth + @{ 'Idempotency-Key' = [Guid]::NewGuid().ToString('N') })
$failedFinal = Wait-Order $failed.id @('Failed', 'Picking') $auth
Write-Host "Out-of-stock order $($failed.orderNumber): $($failedFinal.status) - $($failedFinal.statusReason)"

$after = Invoke-Api GET '/api/dashboard/summary' -Headers $auth
Write-Host "Dashboard: orders $($before.orders.totalOrders) -> $($after.orders.totalOrders), fulfillment jobs $($before.fulfillment.totalJobs) -> $($after.fulfillment.totalJobs), unavailable sources: [$($after.unavailableSources -join ', ')]"
Write-Host "Average processing time (s): to picking $($after.orders.averageSecondsToPicking), to shipped $($after.orders.averageSecondsToShipped), to delivered $($after.orders.averageSecondsToDelivered); delivered shipments $($before.shipping.delivered) -> $($after.shipping.delivered)"
