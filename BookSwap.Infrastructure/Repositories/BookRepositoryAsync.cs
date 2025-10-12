using BookSwap.Core.Entities;
using BookSwap.Infrastructure.Abstracts;
using BookSwap.Infrastructure.Context;
using BookSwap.Infrastructure.InfrastructureBases;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Infrastructure.Repositories
{
    public class BookRepositoryAsync : GenericRepositoryAsync<Book>, IBookRepositoryAsync
    {
        public BookRepositoryAsync(BookSwapDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Book>> GetPendingApprovalBooksAsync()
        {
            return await GetTableNoTracking()
                .Where(b => !b.IsApproved && string.IsNullOrEmpty(b.RejectionReason))
                .Include(b => b.Owner)
                .Include(b => b.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetApprovedBooksAsync()
        {
            return await GetTableNoTracking()
                .Where(b => b.IsApproved && b.IsAvailable)
                .Include(b => b.Owner)
                .Include(b => b.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooksForUserAsync(int ownerId)
        {
            return await GetTableNoTracking()
                .Where(b => b.OwnerId == ownerId)
                .Include(b => b.Owner)
                .Include(b => b.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> SearchBooksAsync(string? searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return await GetApprovedBooksAsync();

            return await GetTableNoTracking()
                .Where(b => b.IsApproved && b.IsAvailable &&
                            (b.Title.Contains(searchTerm) ||
                             b.Author.Contains(searchTerm) ||
                             b.ISBN == null ? false : b.ISBN.Contains(searchTerm)))
                .Include(b => b.Owner)
                .Include(b => b.Category)
                .ToListAsync();
        }
        public async Task<IEnumerable<Book>> GetRejectedBooksAsync()
        {
            return await GetTableNoTracking()
                .Where(b => !b.IsApproved && !string.IsNullOrEmpty(b.RejectionReason))
                .Include(b => b.Owner)
                .Include(b => b.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetRejectedBooksForUserAsync(int ownerId)
        {
            return await GetTableNoTracking()
                .Where(b => b.OwnerId == ownerId && !b.IsApproved && !string.IsNullOrEmpty(b.RejectionReason))
                .Include(b => b.Owner)
                .Include(b => b.Category)
                .ToListAsync();
        }
        public async Task<List<Book>> GetBooksByIdsAsync(IEnumerable<int> bookIds)
        {
            return await GetTableNoTracking()
                .Where(b => bookIds.Contains(b.Id))
                
                .ToListAsync();
        }
    }
}
