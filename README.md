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

## 🌐 API для веб-клиента (v1)

Контракт — `DrugsEngine.Web/docs/API.md`. Ответы в camelCase, ошибки — `application/problem+json`.

| Метод | Путь | Что отдаёт |
|---|---|---|
| GET | `/api/v1/drugs/search?q&mode=exact\|semantic&sort&manufacturer&country&page&pageSize` | поиск с фасетами |
| GET | `/api/v1/drugs/suggest?q&limit` | подсказки |
| GET | `/api/v1/drugs/popular?limit` | препараты, доступные в наибольшем числе аптек |
| GET | `/api/v1/drugs/{id}` | карточка: цены по аптекам, описание из векторного индекса |
| GET | `/api/v1/drugs/{id}/analogs?limit` | похожие препараты (Python-сервис) |
| GET | `/api/v1/pharmacies?network&city&q`, `/api/v1/pharmacies/{id}` | аптеки с координатами и ассортиментом |
| GET | `/api/v1/stats`, `/health` | статистика каталога, проверка БД |

Точный поиск — ILIKE + триграммное сходство (pg_trgm, терпит опечатки). Поиск по смыслу и похожие препараты —
HTTP API DrugsEnginePythonService; если он недоступен, эти эндпоинты отвечают 503, остальное работает.

## ⚙️ Конфигурация

Значения по умолчанию заданы в `Infrastructure/Configuration/Options.cs` (`appsettings.json` намеренно не хранится в git);
переопределяются через `appsettings.*.json` или переменные окружения (`Секция__Ключ`).

| Параметр | По умолчанию | Назначение |
|---|---|---|
| `ConnectionStrings__Default` | — (обязателен) | PostgreSQL |
| `Database__MigrateOnStartup` | `true` | применять миграции при старте |
| `Parsing__Enabled`, `Parsing__Interval` | `true`, `06:00:00` | фоновый сбор цен (не блокирует запуск API) |
| `Kafka__Enabled`, `Kafka__BootstrapServers` | `true`, `kafka:9092` | события о новых препаратах для векторизации |
| `SemanticSearch__BaseUrl` | пусто (выключено) | адрес Python-сервиса, например `http://embedding_service:8000/` |
| `SemanticSearch__MinScore`, `RelativeWindow`, `MinSimilarity` | `0.8`, `0.01`, `0.95` | пороги векторного поиска, подобраны для multilingual-e5-small на реальном каталоге |
| `Kafka__PublishAllOnStartup` | `false` | однократно отправить весь каталог на индексацию (первый запуск, смена модели) |
| `Geocoding__Enabled`, `Geocoding__UserAgent` | `false`, пусто | координаты для аптек, у которых их нет на сайте (Nominatim, нужен User-Agent с контактом) |
| `Catalog__OfferMaxAge` | `3.00:00:00` | цена, которую парсер не видел дольше, считается снятой с продажи |

## 🕷️ Парсинг сети «Доктор»

- Источник — прайс `aptekadoktor.com/availability/?keyword=<буква>`: название, производитель/страна, дата обновления цены,
  цена и аптека (адрес, телефон, координаты в ссылке на карту). Часы работы — со страницы `/contact`.
- **Аптека определяется по телефону** (`DrugStore.ExternalKey`): номеров у аптек на сайте нет; внутренний номер
  назначается один раз и больше не меняется.
- **Фасовка — часть названия**: «№20» и «№50» — разные товары с разными ценами.
- Форма выпуска определяется по сокращениям в названии, действующее вещество — из скобок в конце названия
  («…№30(Пантопразол)»), только у лекарственных форм (`Infrastructure/Parsing/ProductNameAnalyzer.cs`).
- Страна, которой нет в справочнике ISO (например, «ПМР») или не указанная сайтом, сохраняется как неизвестная.
- Разбор страниц — чистые функции (`DoctorSiteParser`), покрыты тестами на фикстурах.

## 🕷️ Парсинг сети «Столетник» (100letnik.md)

- Список товаров — `sitemap-shop.xml` (~7,3 тыс. карточек), по каждой карточке — блок «Наличие в аптеках»:
  цена **в каждой аптеке** (может отличаться) и уровень остатка «Мало / Достаточно / Много» (`DrugItem.StockLevel`).
- Аптеки (~80 в 20 населённых пунктах) определяются по телефону; часы работы — со страницы `/kontakty/`.
- Координат на сайте нет — для карты включите геокодирование (`Geocoding__Enabled`, `Geocoding__UserAgent`).
- Полный проход ≈ число товаров × `Parsing__Stoletnik__RequestDelay` (по умолчанию 1 с, т. е. ~2 часа);
  `Parsing__Stoletnik__MaxProducts` ограничивает проход для проверки, `Parsing__Stoletnik__Enabled` отключает сеть.
- Разбор — чистые функции (`StoletnikSiteParser`), общие правила текста — `SourceText`.

## 🚦 Быстрый старт

### Локально
```bash
# PostgreSQL на localhost:5432 (postgres/postgres), база drugsengine
cd Infrastructure
ASPNETCORE_ENVIRONMENT=Development dotnet run      # миграции применятся сами; парсинг и Kafka в Development выключены
psql -h localhost -U postgres -d drugsengine -f ../scripts/dev-seed.sql   # демо-данные (по желанию)
```
Swagger: `http://localhost:5080/swagger` (в Development).

### Docker
```bash
docker compose up -d --build     # API на http://localhost:5080
```

## 🧪 Тесты

```bash
dotnet test DrugsEngine.sln
```

`Api.Tests` — интеграционные тесты API на настоящем PostgreSQL: каждый прогон создаёт временную базу, применяет миграции
и `scripts/dev-seed.sql`, Python-сервис подменяется фейком. Сервер задаётся `TEST_POSTGRES`
(по умолчанию `Host=localhost;Username=postgres;Password=postgres`).

---

## 📄 Лицензия
Распространяется под лицензией MIT. Автор: **Даниил Гандапас** ([@Asm-o-Dan](https://github.com/Asm-o-Dan)).
