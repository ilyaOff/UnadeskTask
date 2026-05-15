#!/usr/bin/env pwsh
# init-rabbitmq.ps1 - Скрипт инициализации очередей RabbitMQ

param(
    [Parameter(Mandatory = $false)]
    [string]$ContainerName = "pdf_rabbitmq_dev",
    
    [Parameter(Mandatory = $false)]
    [string]$RabbitMQUser = "admin",
    
    [Parameter(Mandatory = $false)]
    [string]$RabbitMQPass = "admin123",
    
    [Parameter(Mandatory = $false)]
    [string]$VirtualHost = "/",
    
    [Parameter(Mandatory = $false)]
    [int]$StartupDelay = 10,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipDelay
)

# Функция для выполнения команд rabbitmqadmin
function Invoke-RabbitCommand {
    param([string]$Command)
    
    $fullCommand = "rabbitmqadmin $Command --vhost=$VirtualHost --user=$RabbitMQUser --password=$RabbitMQPass"
    docker exec $ContainerName $fullCommand.Split(' ') | ForEach-Object { $_ }
}

# Функция для проверки доступности RabbitMQ
function Test-RabbitMQReady {
    param([int]$MaxRetries = 30, [int]$RetryInterval = 2)
    
    Write-Host "Checking if RabbitMQ is ready..." -ForegroundColor Yellow
    
    for ($i = 1; $i -le $MaxRetries; $i++) {
        $result = docker exec $ContainerName rabbitmq-diagnostics ping 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "RabbitMQ is ready!" -ForegroundColor Green
            return $true
        }
        Write-Host "Waiting for RabbitMQ... ($i/$MaxRetries)" -ForegroundColor Gray
        Start-Sleep -Seconds $RetryInterval
    }
    
    Write-Host "RabbitMQ failed to start within timeout" -ForegroundColor Red
    return $false
}

# Функция для создания очереди
function Ensure-Queue {
    param([string]$QueueName, [bool]$Durable = $true)
    
    $durableValue = $Durable.ToString().ToLower()
    Write-Host "  Creating queue: $QueueName (durable=$durableValue)" -ForegroundColor Gray
    Invoke-RabbitCommand "declare queue name=$QueueName durable=$durableValue"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    Queue created: $QueueName" -ForegroundColor Green
    } else {
        Write-Host "    Failed to create queue: $QueueName" -ForegroundColor Red
    }
}

# Функция для создания обменника
function Ensure-Exchange {
    param([string]$ExchangeName, [string]$Type = "direct", [bool]$Durable = $true)
    
    $durableValue = $Durable.ToString().ToLower()
    Write-Host "  Creating exchange: $ExchangeName (durable=$durableValue)" -ForegroundColor Gray
    Invoke-RabbitCommand "declare exchange name=$ExchangeName type=$Type durable=$durableValue"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    Exchange created: $ExchangeName" -ForegroundColor Green
    } else {
        Write-Host "    Failed to create exchange: $ExchangeName" -ForegroundColor Red
    }
}

# Функция для создания привязки
function Ensure-Binding {
    param([string]$Source, [string]$DestinationType, [string]$Destination, [string]$RoutingKey)
    
    Write-Host "  Creating binding: $Source -> $Destination ($RoutingKey)" -ForegroundColor Gray
    Invoke-RabbitCommand "declare binding source=$Source destination_type=$DestinationType destination=$Destination routing_key=$RoutingKey"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "    Binding created" -ForegroundColor Green
    } else {
        Write-Host "    Failed to create binding" -ForegroundColor Red
    }
}

# ============================================
# Основной скрипт
# ============================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  RabbitMQ Queue Initialization" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Container: $ContainerName"
Write-Host "  User: $RabbitMQUser"
Write-Host "  Virtual Host: $VirtualHost"
Write-Host ""

# Проверяем, запущен ли контейнер
$containerStatus = docker ps --filter "name=$ContainerName" --format "table {{.Status}}" 2>$null
if (-not $containerStatus) {
    Write-Host "Error: Container '$ContainerName' is not running!" -ForegroundColor Red
    Write-Host "Please run: docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

# Ждем готовности RabbitMQ
if (-not $SkipDelay) {
    if (-not (Test-RabbitMQReady)) {
        exit 1
    }
} else {
    Write-Host "Skipping readiness check (using manual delay: ${StartupDelay}s)" -ForegroundColor Yellow
    Start-Sleep -Seconds $StartupDelay
}

Write-Host ""
Write-Host "Initializing queues..." -ForegroundColor Yellow
Write-Host ""

# 1. Создание очередей
Write-Host "[1/3] Creating queues..." -ForegroundColor Cyan
Ensure-Queue -QueueName "pdf.processing.queue"
Ensure-Queue -QueueName "rpc.get_documents"
Ensure-Queue -QueueName "rpc.get_pages"
Ensure-Queue -QueueName "pdf.error.queue"

Write-Host ""

# 2. Создание обменников
Write-Host "[2/3] Creating exchanges..." -ForegroundColor Cyan
Ensure-Exchange -ExchangeName "pdf.exchange" -Type "direct"

Write-Host ""

# 3. Создание привязок
Write-Host "[3/3] Creating bindings..." -ForegroundColor Cyan
Ensure-Binding -Source "pdf.exchange" -DestinationType "queue" -Destination "pdf.processing.queue" -RoutingKey "pdf.process"

Write-Host ""

# Вывод статуса очередей
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Current queue status" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

docker exec $ContainerName rabbitmqadmin list queues --vhost=$VirtualHost --user=$RabbitMQUser --password=$RabbitMQPass

Write-Host ""
Write-Host "Initialization completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Management UI: http://localhost:15672" -ForegroundColor Yellow
Write-Host "  Login: $RabbitMQUser" -ForegroundColor Yellow