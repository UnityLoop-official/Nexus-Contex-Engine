#!/usr/bin/env pwsh
# Script automatico per testare Nexus Context Engine

Write-Host "🚀 Nexus Test Automation Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Verifica se il daemon è già in esecuzione
Write-Host "📡 Step 1: Verifico se il daemon è già attivo..." -ForegroundColor Yellow
$daemonRunning = $false
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5050/swagger/index.html" -TimeoutSec 2 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        $daemonRunning = $true
        Write-Host "✅ Daemon già in esecuzione sulla porta 5050" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Daemon non in esecuzione, lo avvio ora..." -ForegroundColor Yellow
}

# Step 2: Avvia il daemon se necessario
if (-not $daemonRunning) {
    Write-Host "🔧 Step 2: Avvio il Nexus Daemon..." -ForegroundColor Yellow
    $daemonPath = "c:\Users\dacan\OneDrive\Desktop\NexusDev\nexus-daemon\src\Nexus.Server"
    
    Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$daemonPath'; dotnet run" -WindowStyle Normal
    
    Write-Host "⏳ Attendo 5 secondi per l'avvio del daemon..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    # Verifica che sia partito
    $retry = 0
    while ($retry -lt 10) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:5050/swagger/index.html" -TimeoutSec 2 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "✅ Daemon avviato con successo!" -ForegroundColor Green
                break
            }
        } catch {
            $retry++
            Start-Sleep -Seconds 1
        }
    }
    
    if ($retry -eq 10) {
        Write-Host "❌ Errore: Il daemon non si è avviato correttamente" -ForegroundColor Red
        exit 1
    }
}

# Step 3: Compila l'extension VS Code
Write-Host ""
Write-Host "🔨 Step 3: Compilo l'extension VS Code..." -ForegroundColor Yellow
$vscodePath = "c:\Users\dacan\OneDrive\Desktop\NexusDev\nexus-vscode"
Set-Location $vscodePath

# Installa dipendenze se necessario
if (-not (Test-Path "$vscodePath\node_modules")) {
    Write-Host "📦 Installo le dipendenze npm..." -ForegroundColor Yellow
    npm install --silent
}

Write-Host "⚙️  Compilo TypeScript..." -ForegroundColor Yellow
npm run compile --silent

Write-Host "✅ Extension compilata!" -ForegroundColor Green

# Step 4: Test rapido dell'API
Write-Host ""
Write-Host "🧪 Step 4: Testo l'API del daemon..." -ForegroundColor Yellow

$testRequest = @{
    taskType = "Analyze"
    solutionId = ""
    solutionPath = "c:\Users\dacan\OneDrive\Desktop\NexusDev\nexus-daemon"
    targets = @()
} | ConvertTo-Json

try {
    $result = Invoke-RestMethod -Uri "http://localhost:5050/context/compile" -Method Post -Body $testRequest -ContentType "application/json"
    Write-Host "✅ API funzionante! Nodi trovati: $($result.targets.Count)" -ForegroundColor Green
    Write-Host ""
    Write-Host "📄 Esempio di output DSL:" -ForegroundColor Cyan
    Write-Host $result.bytecode.Substring(0, [Math]::Min(500, $result.bytecode.Length)) -ForegroundColor Gray
    Write-Host "..." -ForegroundColor Gray
} catch {
    Write-Host "⚠️  Errore nel test API: $_" -ForegroundColor Red
}

# Step 5: Istruzioni finali
Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "✅ Setup completato!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Prossimi passi:" -ForegroundColor Yellow
Write-Host "1. In VS Code, apri la cartella: $vscodePath"
Write-Host "2. Premi F5 per lanciare l'Extension Development Host"
Write-Host "3. Nella nuova finestra VS Code:"
Write-Host "   - Apri una cartella (es. nexus-daemon)"
Write-Host "   - Ctrl+Shift+P → 'Nexus: Open Chat Panel'"
Write-Host "   - Prova a fare domande sul codice!"
Write-Host ""
Write-Host "🌐 Swagger UI disponibile su: http://localhost:5050/swagger" -ForegroundColor Cyan
Write-Host ""
