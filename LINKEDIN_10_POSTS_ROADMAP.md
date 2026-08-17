# 🗺️ 10-Post LinkedIn Content Roadmap: Junior ➔ Middle Engineering Path

> **Narrative Theme:** *«The Engineering Growth Path: Real production bugs, why naive Junior code breaks at scale, and how to engineer resilient Middle/Senior systems.»*  
> **Source Codebase:** [Asm-o-Dan/DrugsEngine](https://github.com/Asm-o-Dan/DrugsEngine) & [Asm-o-Dan/DrugsEnginePythonService](https://github.com/Asm-o-Dan/DrugsEnginePythonService)

---

```mermaid
journey
    title 10-Stage Junior to Middle Engineering Evolution
    section Level 1: Syntax & Validation
      #1: The Unicode Ё Trap & Culture Parsing: 5: Junior
      #2: The Self-Breaking PUT Handler: 5: Junior
      #3: Undefined Symbol Crash on Kafka EOF: 4: Junior
    section Level 2: Contracts & Microservices
      #4: Vectorizing JSON String vs Query: 4: Junior+
      #5: Ghost UUIDs & Entity Deserialization: 3: Junior+
      #6: Socket Starvation & Recursive Retries: 3: Middle-
    section Level 3: Concurrency & State
      #7: Static State Race Conditions: 2: Middle
      #8: Dirty EF Core ChangeTracker on Rollback: 2: Middle
      #9: The 10-Second Ephemeral Kafka Producer: 1: Middle
      #10: Ingestion N+1 Storm & GUID Misalignment: 1: Middle+
```

---

## 📅 Roadmap Overview (Sorted from Simple to Complex)

| # | Topic | GitHub Issue | Anti-Pattern / Root Cause | Status |
|---|---|---|---|---|
| **1** | **The Unicode `Ё` Trap & Invariant Culture** | — (manual) | `[А-Яа-я]` regex excludes `Ё`/`ё` (Unicode 1025/1105) | ✅ **Published** ([View Post](https://www.linkedin.com/feed/update/urn:li:share:7494505914821939200)) |
| **2** | **The Self-Breaking `PUT` Controller** | [DrugsEngine#4](https://github.com/Asm-o-Dan/DrugsEngine/issues/4) | `Guid.NewGuid()` in constructor makes `id != command.Drug.Id` always true | ✅ **Published** ([View Post](https://www.linkedin.com/feed/update/urn:li:share:7495231217789968384)) |
| **3** | **Undefined Symbol Crash on Kafka EOF** | [PythonService#1](https://github.com/Asm-o-Dan/DrugsEnginePythonService/issues/1) | `KafkaError` not imported, `NameError` crashes consumer on EOF | ⏳ Scheduled |
| **4** | **Raw JSON String Vectorization** | [PythonService#2](https://github.com/Asm-o-Dan/DrugsEnginePythonService/issues/2) | Raw JSON passed to `vectorize_text()` polluting embeddings | ⏳ Scheduled |
| **5** | **Ghost UUIDs & Broken Microservice Contracts** | [PythonService#3](https://github.com/Asm-o-Dan/DrugsEnginePythonService/issues/3) | `from_json` drops incoming `Id`, Qdrant gets random UUIDs | ⏳ Scheduled |
| **6** | **Socket Starvation & Infinite Recursion** | [DrugsEngine#7](https://github.com/Asm-o-Dan/DrugsEngine/issues/7) | `new RestClient()` per call + recursive retry = port exhaustion | ⏳ Scheduled |
| **7** | **Static Mutable State & Race Conditions** | [DrugsEngine#8](https://github.com/Asm-o-Dan/DrugsEngine/issues/8) | Static `Dictionary` race conditions + bootstrap deadlock | ⏳ Scheduled |
| **8** | **Dirty EF Core ChangeTracker on Rollback** | [DrugsEngine#9](https://github.com/Asm-o-Dan/DrugsEngine/issues/9) | `RollbackAsync()` doesn't clear in-memory tracked entities | ⏳ Scheduled |
| **9** | **Ephemeral Kafka Producer & 10s Flush** | [DrugsEngine#10](https://github.com/Asm-o-Dan/DrugsEngine/issues/10) | Rebuilding producer per message + synchronous `Flush(10s)` | ⏳ Scheduled |
| **10** | **Ingestion N+1 Storm & Premature Lookups** | [DrugsEngine#11](https://github.com/Asm-o-Dan/DrugsEngine/issues/11) | Sequential `foreach` loop = 10,000+ DB roundtrips per scrape | ⏳ Scheduled |

---

## 📝 Detailed Post Blueprints

---

### 📌 Post 1: The Unicode `Ё` Trap & Invariant Culture Parsing
- **Hook Options:**
  - *«Why did our parser silently drop 14% of medication names? Because of one letter: `Ё`.»*
  - *«The classic Junior regex bug that breaks in Russian and crashes on Linux Docker:»*
- **Junior Approach:** Using `^[А-Яа-я]+$` assuming Russian alphabet is contiguous in Unicode.
- **The Failure:** `Ё` is code point 1025, while `А-Я` is 1040–1071. All medicines with "Щёлково" or "Зелёная" fail validation.
- **The Fix:** Using `[А-Яа-яЁё]` and explicit `CultureInfo.InvariantCulture` for float parsing.
- **Hashtags:** `#dotnet #csharp #regex #cleancode #unicode #backend`

---

### 📌 Post 2: The Self-Breaking `PUT` Controller
- **Hook Options:**
  - *«Our API returned 400 Bad Request on 100% of update calls. Here is the 3-line logic flaw:»*
  - *«Entity constructors that generate default GUIDs can silently break your REST API.»*
  - *«Why you should NEVER instantiate rich domain entities inside ASP.NET Core controllers:»*
- **Junior Approach:** Instantiating `new Drug(name, ...)` in the controller for an update request, expecting it to represent the target record, and verifying `id != command.Drug.Id`.
- **The Failure:** `BaseEntity` initializes `Id = Guid.NewGuid()` in its constructor. The freshly instantiated entity gets a new random GUID that never matches the incoming route `id`, causing a 100% failure rate with `BadRequest("Идентификатор в URL и теле запроса не совпадают.")`.
- **The Fix:** Decouple web controllers from domain model instantiation using clean CQRS command contracts (`UpdateDrugCommand(Guid Id, string Name, ...)`), encapsulate state transitions in domain methods (`drug.UpdateDetails(...)`), and load/persist tracked entities inside MediatR handlers via `IUnitOfWork`.
- **Hashtags:** `#dotnet #restapi #aspnetcore #csharp #webapi #cleanarchitecture #cqrs #softwareengineering`

---

### 📌 Post 3: Undefined Symbol Crash in Kafka Event Loops
- **Hook Options:**
  - *«A single unimported enum crashed our Python consumer service every time Kafka hit partition EOF.»*
  - *«Dynamic typing in Python is great — until an error handler crashes on a missing import in production.»*
  - *«Why untested error paths are ticking time bombs in event-driven Python microservices:»*
- **Junior Approach:** Writing exception/event handling code that references `KafkaError._PARTITION_EOF` without linting, type-checking, or isolated test coverage for failure paths.
- **The Failure:** `confluent_kafka` was imported as `from confluent_kafka import Consumer, KafkaException, Message`. Because `KafkaError` was omitted, the moment broker EOF or connectivity errors occurred, Python threw `NameError: name 'KafkaError' is not defined`, crashing the container and stalling the consumer group.
- **The Fix:** Import `KafkaError` explicitly, implement automated pre-commit static analysis (`ruff check` / `mypy`), and write deterministic unit tests mocking broker signals.
- **Hashtags:** `#python #apachekafka #microservices #eventdriven #devops #backend #qualityengineering`

---

### 📌 Post 4: Serializing Serialization (Vectorizing Raw JSON Strings)
- **Hook Options:**
  - *«We wondered why semantic search for 'Aspirin' was matching random vitamins. Then we inspected the vector payload.»*
  - *«When your AI model embeds JSON syntax instead of user queries:»*
  - *«Why raw message decoding in event consumers is a silent vector search killer:»*
- **Junior Approach:** Reading message body as raw text `body.decode('utf-8')` and passing it straight into `vectorize_text()`, assuming payload text is always a pure search query.
- **The Failure:** When upstream services send structured messages `{"query": "Парацетамол", "limit": 5}`, transformer models (LaBSE/e5) tokenize JSON keys, quotes, and punctuation (`{"`, `query`, `:`). Vectors cluster around JSON boilerplate rather than pharmaceutical query concepts, destroying search relevance.
- **The Fix:** Introduce explicit message contracts (`SearchQueryMessage`), parse and validate payloads prior to embedding generation, and pass only clean search strings into vectorization pipelines while propagating filter and limit parameters to Qdrant.
- **Hashtags:** `#ai #nlp #vectorsearch #qdrant #python #microservices #machinelearning #systemdesign`

---

### 📌 Post 5: Ghost UUIDs & Desynchronized Vector Databases
- **Hook Options:**
  - *«What happens when your PostgreSQL database and Qdrant vector index use completely different IDs for the same drug?»*
  - *«The silent dataclass bug that broke relational integrity across our AI microservices:»*
  - *«Default factories in Python dataclasses vs cross-service entity identities:»*
- **Junior Approach:** Using `id: str = field(default_factory=lambda: str(uuid.uuid4()))` in the consumer domain model, expecting `from_json()` to handle it automatically without mapping the incoming `Id`.
- **The Failure:** .NET backend assigns and publishes persistent PostgreSQL GUIDs. The consumer's `from_json` dropped the incoming `Id`, causing Qdrant points to be stored under new random UUIDs. AI search queries returned point IDs that did not exist in PostgreSQL, breaking all downstream relational lookups.
- **The Fix:** Preserve canonical entity identities across serialization boundaries by strictly mapping incoming IDs in `from_json` / Pydantic schemas, and enforce integration tests verifying cross-database ID consistency.
- **Hashtags:** `#vectordatabase #qdrant #postgresql #architecture #distributed #python #csharp #microservices`

---

### 📌 Post 6: Socket Starvation & Infinite Recursion in HTTP Scraping
- **Hook Options:**
  - *«It worked on 10 pages. It crashed on 10,000. How `new RestClient()` exhausted our OS ports in 2 minutes:»*
  - *«Why recursion is an anti-pattern for HTTP retry policies in .NET:»*
  - *«How a dead 404 URL locked our entire scraping ingestion pipeline forever:»*
- **Junior Approach:** `var client = new RestClient()` inside method + recursive self-call `return await FetchPageContent(url)` whenever `!response.IsSuccessful`.
- **The Failure:** 
  1. Creating new HTTP clients per request leaves sockets lingering in `TIME_WAIT`, exhausting ephemeral OS ports under load.
  2. Recursive retries without a base case on client errors (404/403) cause infinite recursive asynchronous loops, eating stack frames and permanently stalling scraping threads.
- **The Fix:** Maintain a singleton/reusable `RestClient` instance, replace recursion with an iterative retry loop capped at 3 attempts with exponential backoff delay, fast-fail on 4xx client errors, and accept `CancellationToken`.
- **Hashtags:** `#dotnet #csharp #networking #webscraping #resilience #highload #architecture #devops`

---

### 📌 Post 7: Static Mutable State & Concurrency Race Conditions
- **Hook Options:**
  - *«Enforcing business uniqueness using static in-memory Dictionaries: Why this Junior shortcut breaks in production.»*
  - *«We ran 4 concurrent requests and corrupted our internal dictionary buckets with 100% CPU lockups.»*
  - *«The bootstrap Catch-22 bug where the first pharmacy of any network could NEVER be created:»*
- **Junior Approach:** Storing business state in `public static Dictionary<string, HashSet<int>>` inside domain classes and checking it inside FluentValidation validators.
- **The Failure:**
  1. **Bootstrap Deadlock**: Validator checked if the network already existed in the dictionary before allowing constructor completion, failing all initial creations.
  2. **Thread Safety & Collisions**: `Dictionary` and `HashSet` are not thread-safe. Concurrent writes corrupt hash table internal buckets, throwing random exceptions or hanging CPU.
  3. **Transaction Poisoning**: If a database transaction rolled back, the in-memory static state was never reverted, blocking future valid inserts.
- **The Fix:** Maintain domain entity purity and statelessness, delete static global collections, enforce composite uniqueness via PostgreSQL / EF Core unique indexes (`builder.HasIndex(ds => new { ds.DrugNetwork, ds.Number }).IsUnique()`), and validate existence via repository lookups.
- **Hashtags:** `#concurrency #csharp #dotnet #threading #cleanarchitecture #entityframework #systemdesign`

---

### 📌 Post 8: Dirty EF Core ChangeTracker on Transaction Rollback
- **Hook Options:**
  - *«Why EF Core will try to re-save broken entities even AFTER you roll back your database transaction:»*
  - *«The hidden trap of `IDbContextTransaction.RollbackAsync()`: SQL rolls back, but memory stays dirty.»*
  - *«How a failed save poisoned all subsequent retry attempts in our ASP.NET Core request pipeline:»*
- **Junior Approach:** Catching an exception during `ExecuteTransactionAsync()`, calling `await transaction.RollbackAsync()`, and assuming EF Core's tracking context is automatically reverted.
- **The Failure:** `RollbackAsync()` acts only on the database connection/transaction. In-memory entities remain in EF Core's `ChangeTracker` in `Added` or `Modified` states. Any subsequent attempt to save changes or retry the operation with new instances throws `InvalidOperationException` due to entity tracking key conflicts or duplicate inserts.
- **The Fix:** Always invoke `_dbContext.ChangeTracker.Clear()` inside the transaction rollback catch block to detach all dirty tracked entities, and replace raw `Console.WriteLine` with structured logging via injected `ILogger<UnitOfWork>`.
- **Hashtags:** `#entityframework #efcore #dotnet #csharp #postgresql #databases #architecture #cleancode`

---

### 📌 Post 9: Ephemeral Kafka Producers & 10-Second Flush Latency
- **Hook Options:**
  - *«We turned Apache Kafka into a synchronous 10-second bottleneck by using it like a short-lived DB connection.»*
  - *«Why `using var producer = Build()` is the worst way to produce Kafka messages in .NET:»*
  - *«How a single `Flush(10s)` call starved our ASP.NET Core thread pool under moderate traffic:»*
- **Junior Approach:** Creating and disposing a native `ProducerBuilder` inside each method call + invoking synchronous `producer.Flush(10s)` to wait for acknowledgment.
- **The Failure:**
  1. Re-instantiating `librdkafka` per message spawns multiple background threads, triggers expensive broker metadata handshakes, and churns TCP connections.
  2. Disposing the producer immediately eliminates message batching and compression (`linger.ms`), crippling throughput by orders of magnitude.
  3. Synchronously blocking thread pool threads for up to 10 seconds causes thread pool starvation and massive HTTP request queuing.
- **The Fix:** Register `IProducer<Null, string>` as a long-lived Singleton in DI, enable micro-batching (`LingerMs = 5`), publish messages asynchronously via non-blocking `Produce()`, and flush once upon graceful application shutdown (`IDisposable`).
- **Hashtags:** `#kafka #eventdriven #dotnet #csharp #highload #distributed #performance #architecture`

---

### 📌 Post 10: The Ingestion N+1 Storm & Premature Lookups
- **Hook Options:**
  - *«How we turned a 2-hour scraping job into a 45-second batch import.»*
  - *«The ultimate Clean Architecture anti-pattern: Calling MediatR commands inside a sequential foreach loop.»*
  - *«Why querying `WHERE DrugId = '00000000-0000-0000-0000-000000000000'` was creating duplicate items in PostgreSQL:»*
- **Junior Approach:** Executing `CreateOrUpdateDrugItemCommand` in a sequential `foreach` loop over thousands of parsed items, querying database for existing records before parent IDs are even resolved.
- **The Failure:**
  1. **Premature Empty GUID Query**: Calling `GetByDrugAndPharmacyAsync` with unassigned `Guid.Empty` always returned `null`, silently duplicating rows on every ingestion run.
  2. **10,000+ DB Roundtrips (N+1 Storm)**: For 2,000 items, the scraper opened 2,000 separate DB transactions and executed 10,000 individual SQL queries, saturating IOPS and locking connection pools.
- **The Fix:** Ensure correct foreign key resolution ordering prior to existence queries, replace sequential transactions with batch ingestion pipelines, pre-fetch lookup tables into in-memory dictionaries, and perform bulk upserts via `AddRangeAsync`.
- **Hashtags:** `#performance #highload #sql #dotnet #postgresql #systemdesign #cleanarchitecture #csharp`

---

## 🎨 LinkedIn Strict Formatting & Visual Guidelines

1. **No Markdown Dividers**: Never use `---`, `***`, or `___`. Use clean blank lines for spacing.
2. **No Markdown Bold Asterisks**: Never use `**bold**` or `# headers` (LinkedIn renders them as raw text). Use UPPERCASE and emojis (`📌`, `🔍`, `🛠️`, `💡`) for structure.
3. **High-Res Visual Cards**: Every post has a dedicated 4:3 infographic illustration saved in `C:\DrugEngine\assets\posts\`.
4. **Image Publishing**: Always use `share_linkedin_post_with_image` MCP tool to publish posts with attached infographic cards.

---

## 🤖 Automated Publishing Pipeline Workflow

```mermaid
flowchart LR
    Schedule["⏰ Cron Schedule (Every 2 Days)"] --> Check["🔍 Check Roadmap Status"]
    Check -- "Unpublished posts exist" --> RoadMap["🗺️ Parse Roadmap for next issue"]
    Check -- "All posts published" --> GenRoadmap["🏗️ Generate New 10-Post Roadmap from Closed Issues"]
    GenRoadmap --> RoadMap
    RoadMap --> GitHub["📥 Fetch Closed Issue Body from GitHub"]
    GitHub --> Critic["🕵️ Analyze Anti-Pattern & Solution"]
    Critic --> Image["🎨 Generate Infographic Card (linkedin-infographic-gen)"]
    Image --> LinkedIn["📢 Publish Post with Image via share_linkedin_post_with_image"]
```
