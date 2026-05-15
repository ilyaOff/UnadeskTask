# PDF Processing System

Решение тестового задания от компании UNADESK.
Система для загрузки PDF файлов, извлечения текста и хранения его в базе данных с постраничным доступом.

## Архитектура

- **API Gateway** - принимает файлы от пользователя, отправляет команды в RabbitMQ
- **Background Worker** - обрабатывает PDF, извлекает текст, сохраняет в SQLite
- **RabbitMQ** - асинхронный обмен сообщениями между сервисами
- **SQLite** - хранение извлеченного текста
- **Local File Storage** - временное хранение загруженных PDF файлов

## Технологии

- .NET 8.0
- ASP.NET Core
- RabbitMQ (Docker)
- SQLite + Entity Framework Core
- Swagger



## Быстрый старт

1. Запуск RabbitMQ и PostgreSQL
```bash
# Запуск контейнера
docker-compose up -d

# Проверка статуса
docker ps
```
2. Создание очередей
Windows (PowerShell):
```powershell
.\scripts\init-rabbitmq.ps1
```

3. Настройка хранилища файлов
Измените путь в appsettings.json обоих сервисов:
```json
"FileStorage": {
  "StoragePath": "C:/temp/pdf_storage",
  "UseAbsolutePath": true
}
```
4. Запуск сервисов
Терминал 1 - Background Worker:
```bash
cd src/BackgroundWorker.App
dotnet run
```
Терминал 2 - API Gateway:
```bash
cd src/ApiGateway
dotnet run
``` 
