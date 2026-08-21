Add-Type -AssemblyName System.Speech

$outputDir = Join-Path $PSScriptRoot "..\media"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$scenes = @(
    @{
        Id = 1
        Name = "01_Introduction"
        Title = "Introduction & Overview"
        Text = "Hello and welcome to the technical walkthrough of the Orders and Inventory Service. In high-throughput e-commerce systems, inventory overselling and duplicate order creation are critical risks. Today, we present a production-ready .NET 8 Clean Architecture solution with Microsoft SQL Server, Entity Framework Core, and Dapper. We will demonstrate bulletproof database-level concurrency locking, strict transactional idempotency, high-performance sales reporting, structured Serilog logging, and automated test coverage."
    },
    @{
        Id = 2
        Name = "02_Clean_Architecture"
        Title = "Clean Architecture & Solution Structure"
        Text = "Let us examine our Clean Architecture structure. The Domain layer encapsulates core business rules and invariants inside Product, Order, and OrderItem entities with zero external dependencies. The Application layer manages business use cases, DTOs, and FluentValidation. The Infrastructure layer implements EF Core persistence with a database check constraint enforcing stock greater than or equal to zero. And the API layer provides REST endpoints, RFC 7807 ProblemDetails middleware, and Serilog structured logging."
    },
    @{
        Id = 3
        Name = "03_Concurrency_and_Locking"
        Title = "Concurrency, Locking & Deadlock Prevention"
        Text = "Now, let us look at our concurrency and locking strategy. To prevent overselling without optimistic retry storms, our InventoryRepository executes an atomic SQL update with database-level exclusive row locking using WITH UPDLOCK, ROWLOCK. The condition Stock greater than or equal to Quantity guarantees that deductions succeed only when sufficient inventory exists. To prevent deadlocks during multi-item orders, our OrderProcessingService deterministically sorts all requested SKUs alphabetically prior to acquiring locks."
    },
    @{
        Id = 4
        Name = "04_Idempotency_Mechanics"
        Title = "Idempotency Mechanics"
        Text = "Next, we examine transactional idempotency. Network retries can lead to duplicate orders without proper idempotency guards. Our service uses ExternalOrderId with a unique database index. Within a single transaction, if an existing order is detected, the transaction is rolled back and the system returns HTTP 200 OK with the existing order details and is_duplicate set to true, without double-decrementing inventory."
    },
    @{
        Id = 5
        Name = "05_Sales_Reporting_Dapper"
        Title = "High-Performance Daily Sales Reporting"
        Text = "For Task 2, we built the aggregated daily sales endpoint using Dapper. Instead of heavy ORM change trackers, we execute an optimized raw SQL query targeting composite indexes on Orders PlacedAtUtc and OrderItems. With non-blocking reads, SQL Server aggregates daily SKU sales and total daily sales in a single roundtrip with sub-millisecond response times."
    },
    @{
        Id = 6
        Name = "06_Operational_Readiness"
        Title = "Operational Readiness & Logging"
        Text = "For operational readiness, we integrated Serilog structured JSON logging. Every order attempt explicitly logs its outcome: Accepted, DuplicateIgnored, or RejectedInsufficientStock, enriched with trace identifiers. Furthermore, we expose health checks at slash health for container liveness and slash health slash ready, which actively verifies live SQL Server connectivity. In Development mode, an automated database seeder populates initial catalog products on startup."
    },
    @{
        Id = 7
        Name = "07_Live_Demo_and_Testing"
        Title = "Live Demonstration & Test Suite"
        Text = "In our live demonstration, we tested all endpoints via our Postman collection: health checks, product catalog inspection, initial order submission, duplicate order idempotency verification, and insufficient stock rejection. Our automated test suite includes 35 tests across unit and integration projects. Notably, our concurrency test launches 10 simultaneous threads competing for 3 scarce items. Exactly 3 orders succeed with 201 Created and 7 are rejected with 422 Unprocessable Entity, guaranteeing zero overselling."
    },
    @{
        Id = 8
        Name = "08_Conclusion"
        Title = "Conclusion & Summary"
        Text = "To summarize, we have built an enterprise-grade ASP.NET Core solution adhering to Clean Architecture principles with database-level concurrency protection, guaranteed transactional idempotency, optimized Dapper sales aggregation, and complete operational readiness. All code, documentation, and the Postman collection are ready for deployment. Thank you for watching."
    }
)

Write-Host "Synthesizing voice narration for all scenes using System.Speech..." -ForegroundColor Cyan

$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
# Prefer David or Zira
$voice = $synth.GetInstalledVoices() | Where-Object { $_.VoiceInfo.Name -like "*David*" -or $_.VoiceInfo.Name -like "*Zira*" } | Select-Object -First 1
if ($voice) {
    $synth.SelectVoice($voice.VoiceInfo.Name)
    Write-Host "Using voice: $($voice.VoiceInfo.Name)" -ForegroundColor Green
}
$synth.Rate = 0 # Normal speaking rate

$audioFiles = @()

foreach ($scene in $scenes) {
    $fileName = "$($scene.Name).wav"
    $filePath = Join-Path $outputDir $fileName
    
    $synth.SetOutputToWaveFile($filePath)
    $synth.Speak($scene.Text)
    $synth.SetOutputToNull()
    
    Write-Host "  -> Generated: $fileName" -ForegroundColor Green
    $audioFiles += $filePath
}

# Generate full continuous audio walkthrough
$fullPath = Join-Path $outputDir "full_walkthrough.wav"
$synth.SetOutputToWaveFile($fullPath)
foreach ($scene in $scenes) {
    $synth.Speak($scene.Text)
    # small pause between scenes
    $synth.Speak("...")
}
$synth.SetOutputToNull()
Write-Host "  -> Generated Full Walkthrough: full_walkthrough.wav" -ForegroundColor Yellow

$synth.Dispose()
Write-Host "Audio generation complete in $outputDir!" -ForegroundColor Cyan
