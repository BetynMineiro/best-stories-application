namespace App.Domain.Contracts;

/// <summary>
/// Cache implementation contract.
/// </summary>
public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? absoluteTtl = null) where T : class;
}
