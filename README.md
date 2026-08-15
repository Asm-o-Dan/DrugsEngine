# 💊 DrugsEngine (.NET Core Backend & Ingestion Engine)

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20CQRS-blue)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-336791?style=flat&logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Kafka](https://img.shields.io/badge/Messaging-Apache%20Kafka-231F20?style=flat&logo=apachekafka&logoColor=white)](https://kafka.apache.org/)
[![OData](https://img.shields.io/badge/API-OData%20%2F%20REST-orange)](https://odata.org)

Высоконагруженный бэкенд-сервис и движок агрегации данных о медикаментах и аптеках. Построен по принципам **Clean Architecture** с разделением ответственности через **CQRS**, поддержкой **OData**, стримингом событий через **Apache Kafka** и отказоустойчивым модулем парсинга.

---

## 🏛️ Архитектура системы

Проект организован по многослойной структуре **Clean Architecture**:

```
DrugsEngine/
├── Domain/                   # Доменные сущности, Value Objects, валидаторы FluentValidation
│   ├── Entities/             # Drug, DrugStore, DrugItem, Country, UserProfile, FavoriteDrug
│   ├── ValueObjects/         # Address, Primitives
│   └── Validators/           # Доменная валидация бизнес-правил
├── Application/              # Use Cases, CQRS команды и запросы, DTOs, маппинг
│   ├── UseCases/
│   │   ├── Commands/         # Create/Update/Delete команды
│   │   └── Queries/          # Выборки данных
│   ├── Interfaces/           # Контракты репозиториев, Unit of Work, Kafka, парсеров
│   └── Mapping/              # AutoMapper профили
└── Infrastructure/           # Внешние зависимости, БД, API контроллеры, Парсинг
    ├── Dal/                  # DbContext, EF Core Configurations, Миграции, Репозитории
    ├── Parsing/              # Парсеры аптечных сетей (DoctorParser, VivaFarmParser, BaseParser)
    ├── Kafka/                # Продюсеры сообщений
    └── API/                  # ASP.NET Core Web API & OData Controllers
```

---

## 🚀 Ключевые возможности

1. **CQRS & Unit of Work**:
   - Разделение репозиториев на Read/Write (`IDrugReadRepository` / `IDrugWriteRepository`).
   - Транзакционная целостность с `ExecutionStrategy` для отказоустойчивости при сбоях соединений.
2. **OData Querying**:
   - Гибкая фильтрация, сортировка и пагинация по каталогу лекарств и наличию в аптеках без написания бойлерплейт-эндпоинтов.
3. **Data Ingestion & Scraping**:
   - Модульный парсинг аптечных сетей с обработкой ошибок и нормализацией данных.
4. **Event Streaming (Kafka)**:
   - Публикация событий обновления цен и номенклатуры для downstream-микросервисов (векторизация и поиск).
5. **Строгая доменная модель**:
   - Инкапсулированные инварианты, Value Objects (`Address`), автовалидация через `FluentValidation`.

---

## 🛠️ Стек технологий

- **Язык & Платформа**: C# 12, .NET 8
- **ORM & БД**: Entity Framework Core 8, PostgreSQL, Npgsql
- **API**: ASP.NET Core Web API, Microsoft.AspNetCore.OData
- **Шина сообщений**: Apache Kafka (`Confluent.Kafka`)
- **Парсинг HTML**: `HtmlAgilityPack`, `RestSharp`
- **Логирование**: `Serilog` (консоль + ротация файлов)
- **Тестирование**: xUnit, FluentAssertions

---

## 🚦 Быстрый старт

### Требования
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker & Docker Compose](https://www.docker.com/)

### 1. Запуск инфраструктуры (PostgreSQL, Kafka)
```bash
docker-compose up -d
```

### 2. Применение миграций и запуск приложения
```bash
cd Infrastructure
dotnet ef database update
dotnet run
```

API будет доступно по адресу `http://localhost:5000` (или настроенному порту).
Спецификация Swagger UI: `http://localhost:5000/swagger`.

---

## 📄 Лицензия
Распространяется под лицензией MIT. Автор: **Даниил Гандапас** ([@Asm-o-Dan](https://github.com/Asm-o-Dan)).
