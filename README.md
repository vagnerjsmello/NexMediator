# NexMediator

<p align="center">
  <img src="https://raw.githubusercontent.com/wiki/vagnerjsmello/NexMediator/assets/logo-nexmediator.png" alt="NexMediator logo" width="150" />
</p>

<p align="center"><strong>A modern, extensible, and high-performance .NET library for clean CQRS applications.</strong></p>

<p align="center">
  <a href="https://www.nuget.org/packages/NexMediator"><img src="https://img.shields.io/nuget/v/NexMediator?style=flat-square" alt="NuGet version" /></a>
  <a href="https://vagnerjsmello.github.io/NexMediator"><img src="https://img.shields.io/endpoint?url=https://vagnerjsmello.github.io/NexMediator/Summary.json&style=flat-square" alt="Coverage report" /></a>
  <img src="https://img.shields.io/badge/.NET-8.0-blueviolet?style=flat-square" />
  <img src="https://img.shields.io/badge/C%23-12.0-brightgreen?style=flat-square" />
  <img src="https://img.shields.io/badge/Pattern-CQRS-blue?style=flat-square" />
  <img src="https://img.shields.io/badge/Architecture-DDD-purple?style=flat-square" />
  <img src="https://img.shields.io/badge/Tested%20with-TDD-success?style=flat-square" />
  <img src="https://img.shields.io/badge/License-Apache%202.0-blue.svg?style=flat-square" />
</p>

---

## 📘 Overview


**NexMediator** is a clean, modular, and pluggable mediator engine for .NET 8+ applications — a robust alternative to MediatR with:

- ✅ Built-in pipeline behaviors (logging, validation, caching, transactions)
- 📣 Parallel notifications
- 📡 Async streaming with `IAsyncEnumerable<T>`
- 🧩 Full DI support and testable architecture
- ⚡ Compiled delegates (no runtime reflection)

> Inspired by CQRS, Clean Architecture and DDD.

---

## 📦 Installation

Install via CLI:

```bash
dotnet add package NexMediator
```

Or via NuGet:

```powershell
NuGet\Install-Package NexMediator
```

---

## 🚀 Quick Start

```csharp
builder.Services.AddNexMediator();
```

This registers the core engine and auto-discovers all handlers.

---

### 🧱 Built-in Pipeline Behaviors

You can add any of the built-in behaviors:

```csharp
builder.Services.AddNexMediator(options =>
{
    options.AddBehavior(typeof(LoggingBehavior<,>), 1);
    options.AddBehavior(typeof(FluentValidationBehavior<,>), 2);
    options.AddBehavior(typeof(CachingBehavior<,>), 3);
    options.AddBehavior(typeof(TransactionBehavior<,>), 4);
});
```

| Behavior                | Description                               |
|-------------------------|-------------------------------------------|
| `LoggingBehavior<,>`    | Logs execution details and correlation ID |
| `FluentValidation<,>`   | Validates using FluentValidation          |
| `CachingBehavior<,>`    | Adds response-level caching support       |
| `TransactionBehavior<,>`| Wraps command in transaction scope        |

> 🧩 Want to customize? You can implement your own `INexPipelineBehavior<TRequest, TResponse>`  
> and register it with any order:

```csharp
options.AddBehavior(typeof(MyCustomBehavior<,>), 99);
```

---

## 📖 Documentation

📚 **Complete guides, examples and usage patterns available in the Wiki**:  
👉 [https://github.com/vagnerjsmello/NexMediator/wiki](https://github.com/vagnerjsmello/NexMediator/wiki)

Includes:

- Core concepts (requests, handlers, notifications, streams)
- Usage with Minimal APIs
- Real-world example: AuctionBoardGame
- Custom behaviors, caching, transactions
- Error handling, correlation ID, tests

---

## 🤝 Contributing

We welcome contributions!  
Star ⭐ the repo, suggest improvements, or submit PRs via `feature/*` branches.

✅ Use [Conventional Commits](https://www.conventionalcommits.org)  
📦 Follow our branch strategy: `main`, `develop`, `feature/*`


See [Contributing Guide →](https://github.com/vagnerjsmello/NexMediator/wiki/08-Contributi

---

## 📄 License

Apache 2.0 License — free for commercial and open source use.

© 2024–2025 [Vagner Mello](https://github.com/vagnerjsmello)  
🔗 [LinkedIn](https://www.linkedin.com/in/vagnerjsmello)

---

## 🏷 Tags

`.NET` • `C#` • `CQRS` • `Mediator` • `Open Source` • `NuGet` • `Clean Architecture` • `Pipeline` • `Notifications` • `Streaming` • `Validation` • `Transactions` • `TDD` • `DDD`
