// LiveSetBuilder.Core/Storage/IRepository.cs
namespace LiveSetBuilder.Core.Storage;

public interface IRepository<T>
{
    Task<T?> GetAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<int> InsertAsync(T item);
    Task<int> UpdateAsync(T item);
    Task<int> DeleteAsync(int id);
}
