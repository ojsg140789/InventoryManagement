param(
    [string]$BaseUrl = "http://localhost:5080",
    [ValidateRange(1, 20)][int]$ConcurrencyRounds = 3
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$client.BaseAddress = [Uri]($BaseUrl.TrimEnd('/') + '/')
$client.Timeout = [TimeSpan]::FromSeconds(30)

function Send-Request([string]$method, [string]$path, $body, [int]$expected) {
    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($method), $path)
    try {
        if ($null -ne $body) {
            $request.Content = [System.Net.Http.StringContent]::new(
                (ConvertTo-Json $body -Compress), [System.Text.Encoding]::UTF8, "application/json")
        }
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        try {
            $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if ([int]$response.StatusCode -ne $expected) {
                throw "$method $path expected $expected; got $([int]$response.StatusCode): $content"
            }
            if ($content.StartsWith('{') -or $content.StartsWith('[')) { return (ConvertFrom-Json $content) }
            return $content
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}

function Assert-Condition([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
}

try {
    Send-Request GET "health/ready" $null 200 | Out-Null
    $swagger = Send-Request GET "swagger/v1/swagger.json" $null 200
    Assert-Condition ($null -ne $swagger.paths.'/api/products') "OpenAPI is missing product endpoints."
    $suffix = [Guid]::NewGuid().ToString("N").Substring(0, 12)
    $product = Send-Request POST "api/products" @{ sku = "SMOKE-$suffix"; name = "Smoke product" } 201
    $warehouse = Send-Request POST "api/warehouses" @{ code = "WH-$suffix"; name = "Smoke warehouse" } 201
    Send-Request POST "api/products" @{ sku = " smoke-$suffix "; name = "Duplicate" } 409 | Out-Null
    Send-Request POST "api/warehouses" @{ code = " wh-$suffix "; name = "Duplicate" } 409 | Out-Null
    Send-Request GET "api/products/$($product.id)" $null 200 | Out-Null
    Send-Request GET "api/products" $null 200 | Out-Null
    Send-Request GET "api/warehouses" $null 200 | Out-Null
    $pair = "productId=$($product.id)&warehouseId=$($warehouse.id)"
    Send-Request GET "api/stocks?$pair" $null 404 | Out-Null
    $movement = @{ productId = $product.id; warehouseId = $warehouse.id; quantity = 10 }
    Send-Request POST "api/stock-movements/entries" $movement 201 | Out-Null
    $stock = Send-Request GET "api/stocks?$pair" $null 200
    Assert-Condition ($stock.quantity -eq 10) "Entry did not persist ten units."
    $movement.quantity = 4
    Send-Request POST "api/stock-movements/outputs" $movement 201 | Out-Null
    $movement.quantity = 7
    Send-Request POST "api/stock-movements/outputs" $movement 409 | Out-Null
    $movement.quantity = 0
    Send-Request POST "api/stock-movements/entries" $movement 400 | Out-Null
    $movement.quantity = -1
    Send-Request POST "api/stock-movements/outputs" $movement 400 | Out-Null
    $movement.quantity = [int]::MaxValue
    Send-Request POST "api/stock-movements/entries" $movement 400 | Out-Null
    $stock = Send-Request GET "api/stocks?$pair" $null 200
    $history = @(Send-Request GET "api/stock-movements?$pair" $null 200)
    Assert-Condition ($stock.quantity -eq 6 -and $history.Count -eq 2) "Rejected operations changed stock/history."
    Send-Request GET "api/products/$([Guid]::NewGuid())" $null 404 | Out-Null
    Send-Request GET "api/stocks" $null 400 | Out-Null

    $rounds = @()
    for ($round = 1; $round -le $ConcurrencyRounds; $round++) {
        $raceProduct = Send-Request POST "api/products" @{ sku = "RACE-$round-$suffix"; name = "Concurrency product" } 201
        $racePair = "productId=$($raceProduct.id)&warehouseId=$($warehouse.id)"
        $race = @{ productId = $raceProduct.id; warehouseId = $warehouse.id; quantity = 1 }
        Send-Request POST "api/stock-movements/entries" $race 201 | Out-Null
        $json = ConvertTo-Json $race -Compress
        $body1 = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, "application/json")
        $body2 = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, "application/json")
        try {
            # Start both requests before awaiting either: two independent HTTP requests.
            $task1 = $client.PostAsync("api/stock-movements/outputs", $body1)
            $task2 = $client.PostAsync("api/stock-movements/outputs", $body2)
            $response1 = $task1.GetAwaiter().GetResult()
            $response2 = $task2.GetAwaiter().GetResult()
            try {
                $statuses = @([int]$response1.StatusCode, [int]$response2.StatusCode) | Sort-Object
                Assert-Condition (($statuses -join ',') -eq '201,409') "Expected one success and one conflict; got $statuses."
            } finally { $response1.Dispose(); $response2.Dispose() }
        } finally { $body1.Dispose(); $body2.Dispose() }
        $finalStock = Send-Request GET "api/stocks?$racePair" $null 200
        $finalHistory = @(Send-Request GET "api/stock-movements?$racePair" $null 200)
        $exits = @($finalHistory | Where-Object type -eq "Exit")
        Assert-Condition ($finalStock.quantity -eq 0 -and $exits.Count -eq 1 -and $finalHistory.Count -eq 2) "Concurrent withdrawals corrupted stock/history."
        $rounds += [pscustomobject]@{ productId = $raceProduct.id; statuses = $statuses; finalQuantity = $finalStock.quantity; successfulExits = $exits.Count }
    }
    [pscustomobject]@{
        result = "PASS"
        baseUrl = $BaseUrl
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString("O")
        smokeProductId = $product.id
        warehouseId = $warehouse.id
        finalSmokeQuantity = $stock.quantity
        smokeMovements = $history.Count
        concurrency = $rounds
    } | ConvertTo-Json -Depth 6
} finally { $client.Dispose() }
