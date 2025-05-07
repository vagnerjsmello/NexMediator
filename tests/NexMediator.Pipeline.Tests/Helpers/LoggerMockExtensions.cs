using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexMediator.Pipeline.Tests.Helpers;

public static class LoggerMockExtensions
{
    public static void VerifyLogContains<T>(this Mock<ILogger<T>> logger, string expected, LogLevel? level = null)
    {
        logger.Verify(x => x.Log(
            level ?? It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(expected)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
        Times.AtLeastOnce);
    }

    public static void VerifyLog<T>(
        this Mock<ILogger<T>> logger,
        LogLevel level,
        Times times,
        Func<string, bool> messageFilter)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    messageFilter(v != null ? v.ToString()! : string.Empty)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    public static void VerifyLogWithException<T>(
        this Mock<ILogger<T>> logger,
        LogLevel level,
        Times times,
        Func<string, bool> messageFilter,
        Func<Exception, bool> exceptionFilter)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    messageFilter(v != null ? v.ToString()! : string.Empty)),
                It.Is<Exception>(ex => exceptionFilter(ex)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }
}

