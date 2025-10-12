using BookSwap.Application.Dtos.Book.Response;
using BookSwap.Application.Dtos.WishListItem.Request;
using BookSwap.Application.Dtos.WishListItem.Response;
using BookSwap.Core.Entities;
using BookSwap.Core.Results;

namespace BookSwap.Application.Abstracts
{
    public interface IWishlistService
    {
        Task<Result<WishlistItemByIdDto>> AddWishlistItemAsync(CreateWishlistItemDto dto);
        Task<Result> UpdateWishlistItemAsync(int itemId, UpdateWishlistItemDto dto);
        Task<Result> DeleteWishlistItemAsync(int itemId);
        //Task<Result<ExchangeOffer>> RespondToWishlistAsync(int wishlistItemId, int offeredBookId);
        Task<Result<IEnumerable<WishlistItemResponseDto>>> GetPublicWishlistAsync();
        Task<Result<IEnumerable<WishlistItemResponseDto>>> GetPendingApprovalWishListItemsAsync();
        Task<Result<IEnumerable<WishlistItemResponseDto>>> GetAllApprovedWishListItemsAsync();
        Task<Result<IEnumerable<WishlistItemForUserResponseDto>>> GetWishListItemsForUserAsync();
        Task<Result<IEnumerable<WishlistItemForUserResponseDto>>> GetRejectedWishListItemsAsync();// for admin
        Task<Result<IEnumerable<WishlistItemForUserResponseDto>>> GetRejectedWishListItemsForUserAsync();// for user
        Task<Result> ApproveWishListItemAsync(ApproveWishItemRequest request);

    }

}
