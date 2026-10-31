namespace Gccs.Application.Common;

public interface IApplicationTransaction
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteSerializableAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(operation, cancellationToken);
}

public sealed class ApplicationConcurrencyException(string message, Exception innerException)
    : InvalidOperationException(message, innerException);
