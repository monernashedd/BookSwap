using BookSwap.Core.Entities;
using BookSwap.Core.Entities.Identity;
using BookSwap.Core.Enums;
using BookSwap.Infrastructure.Abstracts;
using BookSwap.Infrastructure.Context;
using BookSwap.Infrastructure.InfrastructureBases;
using Microsoft.EntityFrameworkCore;
namespace BookSwap.Infrastructure.Repositories
{
    public class WishlistItemRepositoryAsync : GenericRepositoryAsync<WishlistItem>, IWishlistItemRepositoryAsync
    {
        public WishlistItemRepositoryAsync(BookSwapDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<WishlistItem>> GetApprovedWishItemsAsync()
        {
            return await GetTableNoTracking()
                 .Where(w => w.IsApproved && w.Status != WishStatus.Removed)
                 .Include(w => w.User)
                 .Include(w => w.ExchangeOffer)
                 .ToListAsync();
        }
        public async Task<IEnumerable<WishlistItem>> GetPublicWishItemsAsync()
        {
            return await GetTableNoTracking()
                 .Where(w => w.IsApproved && w.Status == WishStatus.Public)
                 .Include(w => w.User)
                 .Include(w => w.ExchangeOffer)
                 .ToListAsync();
        }

        public async Task<IEnumerable<WishlistItem>> GetPendingApprovalWishItemsAsync()
        {
            return await GetTableNoTracking()
                .Where(w => !w.IsApproved && string.IsNullOrEmpty(w.RejectionReason) && w.Status != WishStatus.Removed)
                .Include(w => w.User)
                .Include(w => w.ExchangeOffer)
                .ToListAsync();
        }

        public async Task<IEnumerable<WishlistItem>> GetRejectedWishItemsAsync()
        {
            return await GetTableNoTracking()
                          .Where(w => !w.IsApproved && !string.IsNullOrEmpty(w.RejectionReason) && w.Status != WishStatus.Removed)
                          .Include(w => w.User)
                          .Include(w => w.ExchangeOffer)
                          .ToListAsync();
        }

        public async Task<IEnumerable<WishlistItem>> GetRejectedWishItemsForUserAsync(int userId)
        {
            return await GetTableNoTracking()
               .Where(w =>w.UserId== userId && !w.IsApproved && !string.IsNullOrEmpty(w.RejectionReason) && w.Status != WishStatus.Removed)
               .Include(w => w.User)
               .Include(w => w.ExchangeOffer)
               .ToListAsync();
        }

        public async Task<IEnumerable<WishlistItem>> GetWishlistItemsByUserIdAsync(int userId)
        {
            return await _dbContext.WishlistItems
                .Where(w => w.UserId == userId)
                .Include(w => w.ExchangeOffer)
                .ToListAsync();
        }
    }
}