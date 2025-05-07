using NexMediator.Abstractions.Interfaces;

namespace NexMediator.Extensions.Tests.Helpers;

public record TestNotification(string Msg) : INexNotification;
