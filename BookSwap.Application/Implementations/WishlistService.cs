using BookSwap.Application.Abstracts;
using BookSwap.Application.Dtos.WishListItem.Request;
using BookSwap.Application.Dtos.WishListItem.Response;
using BookSwap.Core.Entities;
using BookSwap.Core.Entities.Identity;
using BookSwap.Core.Enums;
using BookSwap.Core.Results;
using BookSwap.Infrastructure.Abstracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Application.Implementations
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistItemRepositoryAsync _wishlistRepository;
        private readonly UserManager<User> _userManager;

        public IBookRepositoryAsync _bookRepositoryAsync { get; }
        public IExchangeOfferRepositoryAsync _exchangeOfferRepositoryAsync { get; }
        public IMediaService _mediaService { get; }
        public ICurrentUserService _currentUserService { get; }

        public WishlistService(
            IWishlistItemRepositoryAsync wishlistRepository,
            IBookRepositoryAsync bookRepositoryAsync,
            IExchangeOfferRepositoryAsync exchangeOfferRepositoryAsync,
            IMediaService mediaService,
            ICurrentUserService currentUserService,
            UserManager<User> userManager)
        {
            _wishlistRepository = wishlistRepository;
            _bookRepositoryAsync = bookRepositoryAsync;
            _exchangeOfferRepositoryAsync = exchangeOfferRepositoryAsync;
            _mediaService = mediaService;
            _currentUserService = currentUserService;
            _userManager = userManager;
        }

        public async Task<Result<WishlistItemByIdDto>> AddWishlistItemAsync(CreateWishlistItemDto dto)
        {
            var user = await _currentUserService.GetUserAsync();
            if (user == null || user.IsBanned)
                return Result<WishlistItemByIdDto>.Failure("Cann't add Wish becauser the account is banned ", failureType: ResultFailureType.Forbidden);
            var imageUrl = await _mediaService.UploadMediaAsync("WishListItem", dto.Image);
            var item = new WishlistItem
            {
                UserId = user.Id,
                Title = dto.Title,
                Author = dto.Author,
                Description = dto.Description,
                IsApproved = false,
                ImageUrl = imageUrl,
                RejectionReason = string.Empty,
                CreatedAt = DateTime.UtcNow,
                Status= dto.Status
            };

            await _wishlistRepository.AddAsync(item);
            return Result<WishlistItemByIdDto>.Success(new WishlistItemByIdDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                ImageUrl = item.ImageUrl,
                Author = item.Author,
                IsApproved = item.IsApproved,
                RejectionReason = string.Empty,
                CreatedAt = item.CreatedAt,
                Status = item.Status,
            }, "Wish created successfully, awaiting admin approval ");
        }

        public async Task<Result> UpdateWishlistItemAsync(int itemId, UpdateWishlistItemDto dto)
        {
            var user = await _currentUserService.GetUserAsync();
            var item = await _wishlistRepository.GetByIdAsync(itemId);
            if (item == null || item.UserId != user.Id)
                return Result.NotFound("Wish Not found or it is not for you ");
            if (item.Status== WishStatus.Fulfilled || item.Status == WishStatus.PendingExchange || item.Status == WishStatus.Removed)
                return Result.BadRequest($"Cann't Edit Wish with {item.Status} status");
            if(dto.Image is not null)
            {
                await _mediaService.DeleteMediaAsync(item.ImageUrl);
                var imageUrl = await _mediaService.UploadMediaAsync("WishListItem", dto.Image);
                item.ImageUrl = imageUrl;
            }
            item.Title = dto.Title;
            item.Author = dto.Author;
            item.Description = dto.Description;
            item.IsApproved = false;
            item.RejectionReason = string.Empty;


            await _wishlistRepository.UpdateAsync(item);
            return Result.Success("Updated successfully, awaiting admin approval");
        }

        public async Task<Result> DeleteWishlistItemAsync(int itemId)
        {
            var user = await _currentUserService.GetUserAsync();
            var item = await _wishlistRepository.GetByIdAsync(itemId);
            if (item == null || item.UserId != user.Id)
                return Result.NotFound("Wish Not found or it is not for you ");
            item.Status = WishStatus.Removed;
            await _wishlistRepository.UpdateAsync(item);
            return Result.Success("Deleted Successfully");
        }

        public async Task<Result<IEnumerable<WishlistItemForUserResponseDto>>> GetWishListItemsForUserAsync()
        {
            var user = await _currentUserService.GetUserAsync();
            var items = await _wishlistRepository.GetWishlistItemsByUserIdAsync(user.Id);
            var result = items.Select(w => new WishlistItemForUserResponseDto
            {
                Id = w.Id,
                Title = w.Title,
                Author = w.Author,
                ImageUrl = w.ImageUrl,
                IsApproved = w.IsApproved,
                RejectionReason = w.RejectionReason,
                Description = w.Description,
                Status = w.Status,
                CreatedAt = w.CreatedAt
            });
            if (!result.Any())
                return Result<IEnumerable<WishlistItemForUserResponseDto>>.NotFound("Not Found Wish Items");
            return Result<IEnumerable<WishlistItemForUserResponseDto>>.Success(result);
        }
        //public async Task<Result<ExchangeOffer>> RespondToWishlistAsync(int wishlistItemId, int offeredBookId)
        //{
        //    var sender = await _currentUserService.GetUserAsync();
        //    // التحقق من الأمنية
        //    var wishlistItem = await _wishlistRepository.GetByIdAsync(wishlistItemId);
        //    if (wishlistItem == null)
        //        return Result<ExchangeOffer>.Failure("Wish Not Found ");

        //    if (!wishlistItem.IsPublic)
        //        return Result<ExchangeOffer>.Failure("Wish isnot Public");

        //    // التحقق من الكتاب المعروض
        //    var offeredBook = await _bookRepositoryAsync.GetByIdAsync(offeredBookId);
        //    if (offeredBook == null)
        //        return Result<ExchangeOffer>.Failure("Book Not Found ");

        //    //// التحقق من ملكية الكتاب
        //    //if (offeredBook.OwnerId != senderId)
        //    //    return Result<ExchangeOffer>.Failure("the book is not for you");

        //    // إنشاء عرض تبادل
        //    var offer = new ExchangeOffer
        //    {
        //        SenderId = sender.Id,
        //        ReceiverId = wishlistItem.UserId,
        //        RequestedBookId = offeredBookId,
        //        OfferedBooks = new List<OfferedBook>()
        //        {
        //            new OfferedBook()
        //            {
        //                BookId= offeredBookId,
        //                IsSelected=false,
        //            }
        //        },
        //        Status = ExchangeOfferStatus.Pending,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    await _exchangeOfferRepositoryAsync.AddAsync(offer);

        //    // إرسال إشعار
        //    var receiver = await _userManager.FindByIdAsync(wishlistItem.UserId.ToString());
        //    var notification = new Notification
        //    {
        //        UserId = receiver.Id,
        //        Message = $"لديك عرض تبادل جديد من {sender.FirstName + " " + sender.LastName} لكتاب \"{offeredBook.Title}\" مقابل أمنيتك لكتاب \"{wishlistItem.Title}\"",
        //        IsRead = false
        //    };
        //    await _notificationRepository.AddAsync(notification);
        //    await _notificationRepository.SaveChangesAsync();

        //    // إرسال بريد
        //    await _emailService.SendEmailAsync(
        //        receiver.Email,
        //        "عرض تبادل جديد",
        //        $"لديك عرض تبادل جديد من {receiver.Name} لكتاب \"{offeredBook.Title}\" مقابل أمنيتك لكتاب \"{wishlistItem.Title}\". يرجى تسجيل الدخول للرد.");

        //    return Result<ExchangeOffer>.Success(offer);
        //}
        public async Task<Result<IEnumerable<WishlistItemResponseDto>>> GetPublicWishlistAsync()
        {
            var publicItems = await _wishlistRepository.GetPublicWishItemsAsync();
            var result = publicItems.Select(i => new WishlistItemResponseDto
            {
                Id = i.Id,
                Title = i.Title,
                Author = i.Author,
                Description = i.Description,
                ImageUrl = i.ImageUrl,
                UserId = i.UserId,
                UserName = i.User.FirstName + " " + i.User.LastName,
                CreatedAt = i.CreatedAt,
                Status = i.Status,
                
            }).ToList();
            if (!result.Any())
                return Result<IEnumerable<WishlistItemResponseDto>>.NotFound("Not Found Wish Items");
            return Result<IEnumerable<WishlistItemResponseDto>>.Success(result);
        }

        public async Task<Result<IEnumerable<WishlistItemResponseDto>>> GetPendingApprovalWishListItemsAsync()
        {
            var pendingItems = await _wishlistRepository.GetPendingApprovalWishItemsAsync();
            var result = pendingItems.Select(i => new WishlistItemResponseDto
            {
                Id = i.Id,
                Title = i.Title,
                Author = i.Author,
                Description = i.Description,
                ImageUrl = i.ImageUrl,
                UserId = i.UserId,
                UserName = i.User.FirstName + " " + i.User.LastName,
                CreatedAt = i.CreatedAt,
                Status = i.Status,

            }).ToList();
            if (!result.Any())
                return Result<IEnumerable<WishlistItemResponseDto>>.NotFound("Not Found Wish Items");
            return Result<IEnumerable<WishlistItemResponseDto>>.Success(result);
        }
        public async Task<Result<IEnumerable<WishlistItemResponseDto>>> GetAllApprovedWishListItemsAsync()
        {
            var approvedItems = await _wishlistRepository.GetApprovedWishItemsAsync();
            var result = approvedItems.Select(i => new WishlistItemResponseDto
            {
                Id = i.Id,
                Title = i.Title,
                Author = i.Author,
                Description = i.Description,
                ImageUrl = i.ImageUrl,
                UserId = i.UserId,
                UserName = i.User.FirstName + " " + i.User.LastName,
                CreatedAt = i.CreatedAt,
                Status = i.Status,

            }).ToList();
            if (!result.Any())
                return Result<IEnumerable<WishlistItemResponseDto>>.NotFound("Not Found Wish Items");
            return Result<IEnumerable<WishlistItemResponseDto>>.Success(result);
        }
        public async Task<Result<IEnumerable<WishlistItemForUserResponseDto>>> GetRejectedWishListItemsAsync()
        {

            var rejectItems = await _wishlistRepository.GetRejectedWishItemsAsync();
            var result = rejectItems.Select(w => new WishlistItemForUserResponseDto
            {
                Id = w.Id,
                Title = w.Title,
                Author = w.Author,
                ImageUrl = w.ImageUrl,
                IsApproved = w.IsApproved,
                RejectionReason = w.RejectionReason,
                Description = w.Description,
                Status = w.Status,
                CreatedAt = w.CreatedAt
            });
            if (!result.Any())
                return Result<IEnumerable<WishlistItemForUserResponseDto>>.NotFound("Not Found Wish Items");
            return Result<IEnumerable<WishlistItemForUserResponseDto>>.Success(result);
        }

        public async Task<Result<IEnumerable<WishlistItemForUserResponseDto>>> GetRejectedWishListItemsForUserAsync()
        {
            var user = await _currentUserService.GetUserAsync();
            var rejectItems = await _wishlistRepository.GetRejectedWishItemsForUserAsync(user.Id);
            var result = rejectItems.Select(w => new WishlistItemForUserResponseDto
            {
                Id = w.Id,
                Title = w.Title,
                Author = w.Author,
                ImageUrl = w.ImageUrl,
                IsApproved = w.IsApproved,
                RejectionReason = w.RejectionReason,
                Description = w.Description,
                Status = w.Status,
                CreatedAt = w.CreatedAt
            });
            if(!result.Any())
                return Result<IEnumerable<WishlistItemForUserResponseDto>>.NotFound("Not Found Wish Items");
            return Result<IEnumerable<WishlistItemForUserResponseDto>>.Success(result);
        }
        [Authorize("Admin")]
        public async Task<Result> ApproveWishListItemAsync(ApproveWishItemRequest request)
        {
            var wishListItem = await _wishlistRepository.GetByIdAsync(request.WishItemId);
            if (wishListItem == null)
                return Result.NotFound("Wish Item not found");

            if (wishListItem.Status == WishStatus.Removed)
                return Result.Failure("Cannot approve a removed wishItem", failureType: ResultFailureType.Forbidden);
            wishListItem.IsApproved = request.IsApproved;
            wishListItem.RejectionReason = request.IsApproved ? string.Empty : request.RejectionReason;
            await _wishlistRepository.UpdateAsync(wishListItem);
            return Result.Success($"Wish Item {(request.IsApproved ? "approved" : "rejected")} successfully");
        }
    }
}