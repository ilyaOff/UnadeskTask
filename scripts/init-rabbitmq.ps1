# init-rabbitmq.ps1
# Скрипт инициализации RabbitMQ для системы обработки PDF

param(
    [string]$RabbitMQContainer = "pdf_rabbitmq_dev",
    [string]$RabbitMQUser = "admin",
    [string]$RabbitMQPass = "admin123"
)

Write-Host "=== Initializing RabbitMQ for PDF Processing System ===" -ForegroundColor Cyan
Write-Host ""

# Функция для выполнения команд rabbitmqadmin
function Invoke-RabbitCommand {
    param([string]$Command)
    docker exec $RabbitMQContainer rabbitmqadmin $Command `
        --vhost=/ --user=$RabbitMQUser --password=$RabbitMQPass
}