using BookSwap.Core.Entities;
using BookSwap.Infrastructure.InfrastructureBases;

namespace BookSwap.Infrastructure.Abstracts
{
    public interface IWishlistItemRepositoryAsync : IGenericRepositoryAsync<WishlistItem>
    {
        Task<IEnumerable<WishlistItem>> GetWishlistItemsByUserIdAsync(int userId);
        Task<IEnumerable<WishlistItem>> GetPendingApprovalWishItemsAsync();
        Task<IEnumerable<WishlistItem>> GetApprovedWishItemsAsync();
        Task<IEnumerable<WishlistItem>> GetPublicWishItemsAsync();
        Task<IEnumerable<WishlistItem>> GetRejectedWishItemsAsync(); // للإدارة
        Task<IEnumerable<WishlistItem>> GetRejectedWishItemsForUserAsync(int userId); // للمستخدم

    }
}
