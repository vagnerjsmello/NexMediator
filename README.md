# NexMediator

## Description:

NexMediator is a lightweight .NET library that implements the Mediator pattern for CQRS. It lets you:

- **Send** commands and queries and receive a single response.  
- **Publish** notifications to many handlers at once.  
- **Stream** data asynchronously in a simple way.

NexMediator also includes a customizable pipeline where you can add behaviors like validation, caching, logging or transaction management. You register your handlers, validators and behaviors with dependency injection, and the library automatically finds and runs them in the order you choose. This makes your code clean, modular and easy to extend.
