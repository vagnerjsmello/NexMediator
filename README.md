# NexMediator

🔷 **NexMediator** is a modern, extensible, and high-performance .NET library implementing the **Mediator** design pattern. It provides a robust alternative to MediatR for CQRS-based architectures — with powerful support for **request/response**, **notifications**, and **streaming**, all underpinned by a fully **pluggable pipeline**.

![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8.0-blueviolet?style=flat-square)
![CSharp](https://img.shields.io/badge/C%23-12.0-brightgreen?style=flat-square)
![Pattern: Mediator](https://img.shields.io/badge/Pattern-Mediator-green?style=flat-square)
![Pattern: CQRS](https://img.shields.io/badge/Pattern-CQRS-blue?style=flat-square)
![Target: Microservices](https://img.shields.io/badge/Target-Microservices-orange?style=flat-square)
![Architecture: Clean](https://img.shields.io/badge/Architecture-Clean-blue?style=flat-square)
![Architecture: DDD](https://img.shields.io/badge/Architecture-DDD-purple?style=flat-square)
![Tested with: TDD](https://img.shields.io/badge/Tested%20with-TDD-success?style=flat-square)


![NuGet NexMediator](https://img.shields.io/nuget/v/NexMediator?style=flat-square)

NuGet: [NexMediator](https://www.nuget.org/packages/NexMediator)  

Install via cli:  
> ```bash
> dotnet add package NexMediator
> ```

Install via package manager:
> ```bash
> NuGet\Install-Package NexMediator
> ```

---

## 📘 Table of Contents

- [✨ About NexMediator](#-about-nexmediator)
- [💡 Name Inspiration](#-name-inspiration)
- [🚀 Features](#-features)
- [🧱 Core Concepts](#-core-concepts)
- [⚙️ Pipeline Behaviors](#️-pipeline-behaviors)
- [🛠 Setup & Configuration](#-setup--configuration)
- [📦 Example Usage](#-example-usage)
- [📚 Docs](#-docs)
- [🤝 Contributing](#-contributing)
- [📄 License](#-license)


## ✨ About NexMediator

**NexMediator** is a clean, high-performance mediator engine for .NET, offering advanced capabilities like:

- 🔁 Asynchronous **request/response** handling
- 📣 Parallel **notification broadcasting**
- 📡 Real-time **streaming support**
- 🧩 Configurable **pipeline behaviors** (logging, validation, caching, transactions)
- ⚡ Optimized performance via **compiled delegates** (no reflection at runtime)
- 🔐 Full support for **dependency injection** and **scoped lifetimes**
- 🧪 Designed for **testability** and **modularization**

Inspired by the **CQRS** pattern, it enables clean separation between commands, queries, and side effects — without adding runtime overhead.


## 💡 Name Inspiration

**NexMediator** is short, modern, and meaningful:

### 🔷 Nex
- **Next** → The evolution of MediatR.
- **Nexus** → A central connection point — perfect for a mediator.
- **Nex** → Clean, minimal, trendy.

### 🔶 Mediator
- Instantly recognizable to .NET devs.
- Communicates its core role in CQRS and request orchestration.

Together: **NexMediator** → a clean and powerful orchestration layer for modern .NET apps.



## 🚀 Features

- ✅ Request / Command / Query handling with pipeline support
- 📣 Publish notifications to multiple handlers **in parallel**
- 📡 Stream large datasets with support for **async enumerables**
- 🧱 Customizable pipeline for **validation**, **logging**, **caching**, and **transactions**
- 🧠 Delegate caching using **Expression Trees**
- 🔐 Full support for **dependency injection** and **scoped lifetimes**
- 🧪 Designed for **testability** and **modularization**



## 🧱 Core Concepts

| Component                     | Description                                                                 |
|------------------------------|-----------------------------------------------------------------------------|
| `INexRequest<TResponse>`     | Base for commands & queries (immutable, typed)                              |
| `INexCommand<TResponse>`     | Requests that **modify state**                                              |
| `INexQuery<TResponse>`       | Requests that **fetch data** without side-effects                           |
| `INexNotification`           | Event-style messages — broadcast to many handlers                           |
| `INexRequestHandler<T,R>`    | Business logic for requests                                                 |
| `INexNotificationHandler<T>` | Handle domain or system-wide notifications                                  |
| `INexStreamRequest<T>`       | Request type that supports `IAsyncEnumerable<T>` streaming                  |
| `INexPipelineBehavior<T,R>`  | Middleware for cross-cutting concerns                                       |



## ⚙️ Pipeline Behaviors

Built-in behaviors:

- **🪵 LoggingBehavior** – Logs execution time, errors, and correlation ID
- **🔎 FluentValidationBehavior** – Validates requests before they reach handlers
- **🧠 CachingBehavior** – Adds response-level caching (with invalidation support)
- **🧾 TransactionBehavior** – Wraps command handlers in transaction scopes

🧩 You can also define your own by implementing:
```csharp
INexPipelineBehavior<TRequest, TResponse>
```



## 🛠 Setup & Configuration

```csharp
// In Startup.cs or Program.cs

//Nexmediator handlers
services.AddNexMediator();
```
Or

```csharp
//NexMediator with Behaviors
services.AddNexMediator(options =>
{
    options.AddBehavior(typeof(LoggingBehavior<,>), order: 1);
    options.AddBehavior(typeof(FluentValidationBehavior<,>), order: 2);
    options.AddBehavior(typeof(CachingBehavior<,>), order: 3);
    options.AddBehavior(typeof(TransactionBehavior<,>), order: 4);
});
```

✨ Supports automatic scanning of handlers and validators using **Scrutor**.

---

## 📦 Example Usage

### Command
```csharp
public class CreateOrder : INexCommand<OrderResult> { ... }

public class CreateOrderHandler : INexRequestHandler<CreateOrder, OrderResult>
{
    public async Task<OrderResult> Handle(CreateOrder request, CancellationToken ct)
    {
        // Business logic here
    }
}
```

### Query
```csharp
public class GetUserProfileQuery : INexQuery<UserProfile>
{
    public int UserId { get; init; }
}

public class GetUserProfileHandler : INexRequestHandler<GetUserProfileQuery, UserProfile>
{
    public async Task<UserProfile> Handle(GetUserProfileQuery query, CancellationToken ct)
    {
        // Fetch and return user profile without side effects
    }
}


### Notification
```csharp
public class OrderCreated : INexNotification { ... }

public class AuditLogHandler : INexNotificationHandler<OrderCreated> { ... }

public class EmailNotifier : INexNotificationHandler<OrderCreated> { ... }
```

### Stream Request
```csharp
public class FetchEvents : INexStreamRequest<EventData> { ... }

public class FetchEventsHandler : INexStreamRequestHandler<FetchEvents, EventData>
{
    public async IAsyncEnumerable<EventData> Handle(FetchEvents request, ...)
    {
        // yield return each event
    }
}
```

### Transactional Request

Requests that implement `ITransactionalRequest<TResponse>` will be executed within a transactional scope (e.g., database transaction). If the handler fails, changes are rolled back automatically.

```csharp
public class UpdateUserCommand : INexCommand<UserResult>, ITransactionalRequest<UserResult>
{
    public int Id { get; init; }
    public string NewEmail { get; init; }
}

public class UpdateUserHandler : INexRequestHandler<UpdateUserCommand, UserResult>
{
    public async Task<UserResult> Handle(UpdateUserCommand command, CancellationToken ct)
    {
        // Changes will run inside a transaction
    }
}
```

### Cacheable Request 

Implement `ICacheableRequest<TResponse>` to enable response-level caching with automatic keying and expiration control.

```csharp
public class GetUserProfileQuery : INexQuery<UserProfile>, ICacheableRequest<UserProfile>
{
    public int UserId { get; init; }

    public string CacheKey => $"UserProfile:{UserId}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(15);
}
```

### 🧹 Invalidate Cache

Use `IInvalidateCacheableRequest` to remove cache entries when executing a state-changing command. Useful for cache coherence after updates.

```csharp
public class UpdateUserCommand : INexCommand<UserResult>, ITransactionalRequest<UserResult>, IInvalidateCacheableRequest
{
    public int Id { get; init; }
    public string NewEmail { get; init; }

    public IReadOnlyCollection<string> KeysToInvalidate => new[]
    {
        $"UserProfile:{Id}"
    };
}
```

> 💡 Combine `ITransactionalRequest` + `IInvalidateCacheableRequest` to ensure cache invalidation only happens after successful transaction commit.


## 📚 Docs

NexMediator is modularized in the following packages:

| Package                            | Description                              |
|-----------------------------------|------------------------------------------|
| `NexMediator`                     | Package meta for Nuget                   |
| `NexMediator.Core`                | Main mediator engine                     |
| `NexMediator.Abstractions`        | Contracts for requests, handlers, etc.   |
| `NexMediator.Extensions`          | DI helpers for registration              |
| `NexMediator.Pipeline.Behaviors`  | Built-in middleware (logging, caching…)  |

📖 Full documentation is available in the `/docs` folder or project wiki (coming soon).



## 🤝 Contributing

We welcome contributions!

- Open an issue or suggestion
- Fork and create a `feature/<your-feature>` branch
- Use [Conventional Commits](https://www.conventionalcommits.org/)
- Submit a PR to `develop`

🛠 Branch strategy:
- `main` – stable releases  
- `develop` – integrates all new features  
- `feature/*` – work in progress branches

## 📄 License

NexMediator is **Apache 2.0 Licensed** — free for commercial and personal use.

> © 2024–2025 Vagner Mello | ([GitHub Profile](https://github.com/vagnerjsmello)) | ([LinkeIn Profile](https://linkedin/in/vagnerjsmello)) | Crafted with ❤ in C#.


## 🏷 Tags

`.NET` • `C#` • `CQRS` • `Mediator` • `Open Source` • `Apache-2.0` • `NuGet` • `Clean Architecture` • `Pipeline` • `Notifications` • `Request Handling` • `Streaming` • `FluentValidation` • `Transaction` • `Expression Trees` • `Chain of Responsibility` • `Middleware` • `Microservices` • `DDD`

