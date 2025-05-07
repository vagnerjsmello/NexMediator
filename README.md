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


![NuGet](https://img.shields.io/nuget/v/NexMediator.Core?style=flat-square)

> NuGet: [NexMediator.Core](https://www.nuget.org/packages/NexMediator.Core/)  
> Install:  
> ```bash
> dotnet add package NexMediator.Core
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

---

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

---

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

---

## 🚀 Features

- ✅ Request / Command / Query handling with pipeline support
- 📣 Publish notifications to multiple handlers **in parallel**
- 📡 Stream large datasets with support for **async enumerables**
- 🧱 Customizable pipeline for **validation**, **logging**, **caching**, and **transactions**
- 🧠 Delegate caching using **Expression Trees**
- 🔐 Full support for **dependency injection** and **scoped lifetimes**
- 🧪 Designed for **testability** and **modularization**

---

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

---

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

---

## 🛠 Setup & Configuration

```csharp
// In Startup.cs or Program.cs
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

---

## 📚 Docs

NexMediator is modularized in the following packages:

| Package                            | Description                              |
|-----------------------------------|------------------------------------------|
| `NexMediator.Core`                | Main mediator engine                     |
| `NexMediator.Abstractions`        | Contracts for requests, handlers, etc.   |
| `NexMediator.Extensions`          | DI helpers for registration              |
| `NexMediator.Pipeline.Behaviors`  | Built-in middleware (logging, caching…)  |

📖 Full documentation is available in the `/docs` folder or project wiki (coming soon).

---

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

---

## 📄 License

NexMediator is **Apache 2.0 Licensed** — free for commercial and personal use.

> © 2024–2025 [NexDevBR](https://github.com/nexdevbr) | Crafted with ❤ in C#.

---

## 🏷 Tags

`.NET` • `C#` • `CQRS` • `Mediator` • `Open Source` • `Apache-2.0` • `NuGet` • `Clean Architecture` • `Pipeline` • `Notifications` • `Request Handling` • `Streaming` • `FluentValidation` • `Transaction` • `Expression Trees` • `Chain of Responsibility` • `Middleware` • `Microservices` • `DDD`
