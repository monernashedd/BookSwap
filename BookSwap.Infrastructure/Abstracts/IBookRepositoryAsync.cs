using BookSwap.Core.Entities;
using BookSwap.Infrastructure.InfrastructureBases;

namespace BookSwap.Infrastructure.Abstracts
{
    public interface IBookRepositoryAsync : IGenericRepositoryAsync<Book>
    {
        Task<IEnumerable<Book>> GetPendingApprovalBooksAsync();
        Task<IEnumerable<Book>> GetApprovedBooksAsync();
        Task<IEnumerable<Book>> GetBooksForUserAsync(int userId);
        Task<IEnumerable<Book>> SearchBooksAsync(string? searchTerm);
        Task<IEnumerable<Book>> GetRejectedBooksAsync(); // للإدارة
        Task<IEnumerable<Book>> GetRejectedBooksForUserAsync(int userId); // للمستخدم
        Task<List<Book>> GetBooksByIdsAsync(IEnumerable<int> bookIds);
    }
}
