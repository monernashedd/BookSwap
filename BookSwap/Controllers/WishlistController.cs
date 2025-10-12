using BookSwap.Api.Bases;
using BookSwap.Api.Extention;
using BookSwap.Application.Abstracts;
using BookSwap.Application.Dtos.Book.Response;
using BookSwap.Application.Dtos.WishListItem.Request;
using BookSwap.Application.Dtos.WishListItem.Response;
using BookSwap.Application.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookSwap.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        [HttpPost]
        public async Task<IActionResult> AddWishlistItem([FromForm] CreateWishlistItemDto dto)
        {
            var result = await _wishlistService.AddWishlistItemAsync(dto);
            return this.ToApiResult(result);
        }
        [HttpPut("{itemId}")]
        public async Task<IActionResult> UpdateWishlistItem(int itemId,[FromForm] UpdateWishlistItemDto dto)
        {
            var result = await _wishlistService.UpdateWishlistItemAsync(itemId, dto);
            return this.ToApiResult(result);
        }
        [HttpGet("my")]
        public async Task<IActionResult> GetUserWishlist()
        {
            var result = await _wishlistService.GetWishListItemsForUserAsync();
            return this.ToApiResult(result);

        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicWishlist()
        {
            var result = await _wishlistService.GetPublicWishlistAsync();
            return this.ToApiResult(result);
          
        }

        [HttpDelete("{itemId}")]
        public async Task<IActionResult> DeleteWishlistItem(int itemId)
        {
            var result = await _wishlistService.DeleteWishlistItemAsync(itemId);
            return this.ToApiResult(result);
        }
        [HttpPost("Approve")]
        [Authorize(Roles = "Admin")]
        public async Task<ApiResult> ApproveWishItem([FromBody] ApproveWishItemRequest request)
        {
            var result = await _wishlistService.ApproveWishListItemAsync(request);
            return this.ToApiResult(result);
        }
        [HttpGet("Rejected")]
        [Authorize(Roles = "Admin")]
        public async Task<ApiResult<IEnumerable<WishlistItemForUserResponseDto>>> RejectedWishListItems()
        {
            var result = await _wishlistService.GetRejectedWishListItemsAsync();
            return this.ToApiResult(result);
        }
        [HttpGet("User/Rejected")]
        [Authorize]
        public async Task<ApiResult<IEnumerable<WishlistItemForUserResponseDto>>> RejectedWishListItemsForUser()
        {
            var result = await _wishlistService.GetRejectedWishListItemsForUserAsync();
            return this.ToApiResult(result);
        }
        [HttpGet("Pending")]
        [Authorize(Roles = "Admin")]
        public async Task<ApiResult<IEnumerable<WishlistItemResponseDto>>> GetPendingApprovalWishItems()
        {
            var result = await _wishlistService.GetPendingApprovalWishListItemsAsync();
            return this.ToApiResult(result);
        }
        [HttpGet("Approved")]
        public async Task<ApiResult<IEnumerable<WishlistItemResponseDto>>> GetAllApprovedWishItems()
        {
            var result = await _wishlistService.GetAllApprovedWishListItemsAsync();
            return this.ToApiResult(result);
        }


        //[HttpPost("respond/{wishlistItemId}")]
        //public async Task<IActionResult> RespondToWishlist(int wishlistItemId, [FromBody] int offeredBookId)
        //{
        //    var result = await _wishlistService.RespondToWishlistAsync(senderId, wishlistItemId, offeredBookId);
        //    return this.ToApiResult(result);
        //}
    }
}
