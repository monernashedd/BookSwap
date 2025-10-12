using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BookSwap.Core.Entities;
using BookSwap.Core.Entities.Identity;
using BookSwap.Core.Results;
using BookSwap.Infrastructure.Repositories;
using BookSwap.Application.Dtos.Book;
using BookSwap.Application.Abstracts;
using Microsoft.AspNetCore.Http;
using BookSwap.Application.Dtos.Book.Response;
using BookSwap.Core.Enums;
using BookSwap.Infrastructure.Abstracts;
using Microsoft.AspNetCore.Authorization;
using BookSwap.Application.Dtos.Book.Request;

namespace BookSwap.Application.Implementations
{
    public class BookService : IBookService
    {
        private readonly IBookRepositoryAsync _bookRepository;
        private readonly IExchangeOfferRepositoryAsync _exchangeOfferRepository;
        private readonly UserManager<User> _userManager;
        private readonly IMediaService _mediaService;
        private readonly IOfferedBookRepositoryAsync _offeredBookRepositoryAsync;
        public ICategoryRepositoryAsync _categoryRepository { get; }
        public ICurrentUserService _currentUserService { get; }
        public BookService(
            IBookRepositoryAsync bookRepository,
            ICategoryRepositoryAsync categoryRepository,
            IExchangeOfferRepositoryAsync exchangeOfferRepository,
            UserManager<User> userManager,
            IMediaService mediaService,
            ICurrentUserService currentUserService,
            IOfferedBookRepositoryAsync offeredBookRepositoryAsync
            )
        {
            _bookRepository = bookRepository;
            _categoryRepository = categoryRepository;
            _exchangeOfferRepository = exchangeOfferRepository;
            _userManager = userManager;
            _mediaService = mediaService;
            _currentUserService = currentUserService;
            _offeredBookRepositoryAsync = offeredBookRepositoryAsync;
            
        }

        public async Task<Result<BookResponse>> CreateBookAsync(CreateBookRequest request)
        {
            var user = await _currentUserService.GetUserAsync();
            var category = await _categoryRepository.GetCategoryByIdAsync(request.CategoryId);
            if (category is null)
                return Result<BookResponse>.NotFound("Not Found Category With Id");

            var imageUrl = await _mediaService.UploadMediaAsync("books", request.Image);
            var book = new Book
            {
                Title = request.Title,
                Author = request.Author,
                ISBN = request.ISBN,
                Description = request.Description,
                CoverImageUrl = imageUrl,
                IsAvailable = request.BookStatus == BookStatus.Available,
                IsApproved = false,
                OwnerId = user.Id,
                Condition = request.Condition,
                CategoryId = request.CategoryId,
                Status = request.BookStatus,// Available أو Personal
                RejectionReason = string.Empty
            };

            await _bookRepository.AddAsync(book);

            return Result<BookResponse>.Success(new BookResponse
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN,
                Description = book.Description,
                ImageUrl = book.CoverImageUrl,
                IsAvailable = book.IsAvailable,
                IsApproved = book.IsApproved,
                OwnerId = book.OwnerId,
                OwnerName = user.FirstName + " " + user.LastName ?? string.Empty,
                Condition = book.Condition,
                CategoryId = book.CategoryId,
                CategoryName = category.Name,
                Status = book.Status,
                RejectionReason = string.Empty
            }, "Book created successfully, awaiting admin approval");
        }
        public async Task<Result<BookResponse>> UpdateBookAsync(UpdateBookRequest request)
        {
            var book = await _bookRepository.GetByIdAsync(request.Id);
            if (book == null)
                return Result<BookResponse>.NotFound("Book not found");
            var user = await _currentUserService.GetUserAsync();
            if (book.OwnerId != user.Id)
                return Result<BookResponse>.Failure("You are not authorized to update this book", failureType: ResultFailureType.Forbidden);
            var category = await _categoryRepository.GetCategoryByIdAsync(request.CategoryId);
            if (category is null)
                return Result<BookResponse>.NotFound("Not Found Category With Id");

            if (book.Status == BookStatus.PendingExchange || book.Status == BookStatus.Removed)
                return Result<BookResponse>.BadRequest($"Cannot update book as it has been {book.Status.ToString()}");

            // التحقق من التغييرات التي تتطلب موافقة الـ Admin
            bool requiresAdminApproval = await RequiresAdminApprovalAsync(book, request);
            // إعادة تعيين IsApproved إذا لزم الأمر
            if (requiresAdminApproval)
            {
                book.IsApproved = false;
                book.RejectionReason = string.Empty;
            }
            if (request.Image != null)
            {
                if (!string.IsNullOrEmpty(book.CoverImageUrl))
                    await _mediaService.DeleteMediaAsync(book.CoverImageUrl);

                book.CoverImageUrl = await _mediaService.UploadMediaAsync("books", request.Image);
            }
            book.Title = request.Title;
            book.Author = request.Author;
            book.ISBN = request.ISBN;
            book.Description = request.Description;
            book.CategoryId = request.CategoryId;
            // تحديث الخصائص التي لا تتطلب موافقة الـ Admin
            book.IsAvailable = request.BookStatus == BookStatus.Available;
            book.Condition = request.Condition;
            book.Status = request.BookStatus;


            await _bookRepository.UpdateAsync(book);

            return Result<BookResponse>.Success(new BookResponse
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN ?? string.Empty,
                Description = book.Description ?? string.Empty,
                ImageUrl = book.CoverImageUrl,
                IsAvailable = book.IsAvailable,
                IsApproved = book.IsApproved,
                OwnerId = book.OwnerId,
                OwnerName = user.FirstName + " " + user.LastName ?? string.Empty,
                Condition = book.Condition,
                CategoryId = book.CategoryId,
                CategoryName = book.Category?.Name ?? string.Empty,
                Status = book.Status,
                RejectionReason = book.RejectionReason
            }, "Book updated successfully, awaiting admin approval");
        }
        public async Task<Result> DeleteBookAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null)
                return Result.NotFound("Book not found");
            var user = await _currentUserService.GetUserAsync();
            if (book.OwnerId != user.Id)
                return Result.Failure("You are not authorized to delete this book", failureType: ResultFailureType.Forbidden);

            if (book.Status == BookStatus.PendingExchange)
                return Result.BadRequest("Cannot delete book as it has been Pending exchanged");

            if (!string.IsNullOrEmpty(book.CoverImageUrl))
                await _mediaService.DeleteMediaAsync(book.CoverImageUrl);

            book.Status = BookStatus.Removed;
            book.IsAvailable = false;
            await _bookRepository.UpdateAsync(book);
            return Result.Success("Book marked as removed successfully");
        }
        public async Task<Result<BookResponse>> GetBookByIdAsync(int bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null || !book.IsApproved || book.Status == BookStatus.Removed || book.Status == BookStatus.Personal)
                return Result<BookResponse>.NotFound("Book not found, not approved, removed, or personal");

            var user = await _userManager.FindByIdAsync(book.OwnerId.ToString());
            return Result<BookResponse>.Success(new BookResponse
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                ISBN = book.ISBN ?? string.Empty,
                Description = book.Description ?? string.Empty,
                ImageUrl = book.CoverImageUrl,
                IsAvailable = book.IsAvailable,
                IsApproved = book.IsApproved,
                OwnerId = book.OwnerId,
                OwnerName = book.Owner.FirstName + " " + book.Owner.LastName ?? string.Empty,
                Condition = book.Condition,
                CategoryId = book.CategoryId,
                CategoryName = book.Category?.Name ?? string.Empty,
                Status = book.Status,
                RejectionReason = book.RejectionReason
            });
        }
        public async Task<Result<IEnumerable<BookResponse>>> GetAllApprovedBooksAsync()
        {
            var books = await _bookRepository.GetApprovedBooksAsync();
            var result = books
                .Where(b => b.Status == BookStatus.Available)
                .Select(b => new BookResponse
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN ?? string.Empty,
                    Description = b.Description ?? string.Empty,
                    ImageUrl = b.CoverImageUrl,
                    IsAvailable = b.IsAvailable,
                    IsApproved = b.IsApproved,
                    OwnerId = b.OwnerId,
                    OwnerName = b.Owner.FirstName + " " + b.Owner.LastName ?? string.Empty,
                    Condition = b.Condition,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category?.Name ?? string.Empty,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason
                });
            if(result.Count() == 0)
                return Result<IEnumerable<BookResponse>>.NotFound("Not Found Approved Books");
            return Result<IEnumerable<BookResponse>>.Success(result);
        }
        public async Task<Result<IEnumerable<BookResponse>>> GetPendingApprovalBooksAsync()
        {
            var user = await _currentUserService.GetUserAsync();
            if (!await _userManager.IsInRoleAsync(user, "Admin"))
                return Result<IEnumerable<BookResponse>>.Failure("Only admins can view pending books", failureType: ResultFailureType.Forbidden);

            var books = await _bookRepository.GetPendingApprovalBooksAsync();
            var result = books
                .Where(b => b.Status != BookStatus.Removed)
                .Select(b => new BookResponse
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN ?? string.Empty,
                    Description = b.Description ?? string.Empty,
                    ImageUrl = b.CoverImageUrl,
                    IsAvailable = b.IsAvailable,
                    IsApproved = b.IsApproved,
                    OwnerId = b.OwnerId,
                    OwnerName = b.Owner.FirstName + " " + b.Owner.LastName ?? string.Empty,
                    Condition = b.Condition,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category?.Name ?? string.Empty,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason
                });
            if (result.Count() == 0)
                return Result<IEnumerable<BookResponse>>.NotFound("Not Found Pending Approval Books");
            return Result<IEnumerable<BookResponse>>.Success(result);
        }
        public async Task<Result<IEnumerable<BookResponse>>> GetBooksForUserAsync()
        {
            var user = await _currentUserService.GetUserAsync();
            var books = await _bookRepository.GetBooksForUserAsync(user.Id);
            var result = books
                .Where(b => b.Status != BookStatus.Removed)
                .Select(b => new BookResponse
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN ?? string.Empty,
                    Description = b.Description ?? string.Empty,
                    ImageUrl = b.CoverImageUrl,
                    IsAvailable = b.IsAvailable,
                    IsApproved = b.IsApproved,
                    OwnerId = b.OwnerId,
                    OwnerName = b.Owner.FirstName + " " + b.Owner.LastName ?? string.Empty,
                    Condition = b.Condition,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category?.Name ?? string.Empty,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason
                });
            if (result.Count() == 0)
                return Result<IEnumerable<BookResponse>>.NotFound("Not Found Books");

            return Result<IEnumerable<BookResponse>>.Success(result);
        }
        public async Task<Result<IEnumerable<BookResponse>>> SearchBooksAsync(string? searchTerm)
        {
            var books = await _bookRepository.SearchBooksAsync(searchTerm);
            var result = books
                .Where(b => b.Status == BookStatus.Available && b.IsAvailable && b.IsApproved)
                .Select(b => new BookResponse
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN ?? string.Empty,
                    Description = b.Description ?? string.Empty,
                    ImageUrl = b.CoverImageUrl,
                    IsAvailable = b.IsAvailable,
                    IsApproved = b.IsApproved,
                    OwnerId = b.OwnerId,
                    OwnerName = b.Owner.FirstName + " " + b.Owner.LastName ?? string.Empty,
                    Condition = b.Condition,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category?.Name ?? string.Empty,
                    Status = b.Status,
                    RejectionReason = b.RejectionReason
                });
            if (result.Count() == 0)
                return Result<IEnumerable<BookResponse>>.NotFound("Not Found Books");

            return Result<IEnumerable<BookResponse>>.Success(result);
        }
        [Authorize("Admin")]
        public async Task<Result> ApproveBookAsync(ApproveBookRequest request)
        {
            var book = await _bookRepository.GetByIdAsync(request.BookId);
            if (book == null)
                return Result.NotFound("Book not found");

            if (book.Status == BookStatus.Removed)
                return Result.Failure("Cannot approve a removed book", failureType: ResultFailureType.Forbidden);

            book.IsApproved = request.IsApproved;
            book.RejectionReason = request.IsApproved ? string.Empty : request.RejectionReason;
            await _bookRepository.UpdateAsync(book);
            return Result.Success($"Book {(request.IsApproved ? "approved" : "rejected")} successfully");
        }
        [Authorize("Admin")]
        public async Task<Result<IEnumerable<BookResponse>>> GetRejectedBooksAsync()
        {
            var books = await _bookRepository.GetRejectedBooksAsync();
            var result = books
                .Where(b => b.Status != BookStatus.Removed)
                .Select(b => new BookResponse
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN ?? string.Empty,
                    Description = b.Description ?? string.Empty,
                    ImageUrl = b.CoverImageUrl,
                    IsAvailable = b.IsAvailable,
                    IsApproved = b.IsApproved,
                    RejectionReason = b.RejectionReason,
                    OwnerId = b.OwnerId,
                    OwnerName = b.Owner.FirstName + " " + b.Owner.LastName ?? string.Empty,
                    Condition = b.Condition,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category?.Name ?? string.Empty,
                    Status = b.Status

                });
            if (result.Count() == 0)
                return Result<IEnumerable<BookResponse>>.NotFound("Not Found Rejected Books");

            return Result<IEnumerable<BookResponse>>.Success(result);
        }

        public async Task<Result<IEnumerable<BookResponse>>> GetRejectedBooksForUserAsync()
        {
            var user = await _currentUserService.GetUserAsync();
            var books = await _bookRepository.GetRejectedBooksForUserAsync(user.Id);
            var result = books
                .Where(b => b.Status != BookStatus.Removed)
                .Select(b => new BookResponse
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    ISBN = b.ISBN ?? string.Empty,
                    Description = b.Description ?? string.Empty,
                    ImageUrl = b.CoverImageUrl,
                    IsAvailable = b.IsAvailable,
                    IsApproved = b.IsApproved,
                    RejectionReason = b.RejectionReason,
                    OwnerId = b.OwnerId,
                    OwnerName = b.Owner.FirstName + " " + b.Owner.LastName ?? string.Empty,
                    Condition = b.Condition,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category?.Name ?? string.Empty,
                    Status = b.Status
                });

            if (result.Count() == 0)
                return Result<IEnumerable<BookResponse>>.NotFound("Not Found Rejected Books");

            return Result<IEnumerable<BookResponse>>.Success(result);
        }
        private async Task<bool> RequiresAdminApprovalAsync(Book book, UpdateBookRequest request)
        {
            // التحقق مما إذا تم تعديل Author أو Description أو Image
            return request.Author != book.Author ||
                   request.Description != book.Description ||
                   request.Title != book.Title ||
                   request.ISBN != book.ISBN ||
                   request.Image != null;
        }

        public async Task<Result<IEnumerable<BookResponse>>> GetOfferedBooksByExchangeOfferId(int exchangeOfferId)
        {
            var user = await _currentUserService.GetUserAsync();
            var ExchangeOffer = await _exchangeOfferRepository.GetByIdAsync(exchangeOfferId);
            if (ExchangeOffer == null)
                return Result<IEnumerable<BookResponse>>.NotFound("Exchange offer not found");
            if (ExchangeOffer.Status != ExchangeOfferStatus.Pending)
            {
                return Result<IEnumerable<BookResponse>>.BadRequest($"This exchange offer is already {ExchangeOffer.Status}");
            }
            if (ExchangeOffer.ReceiverId != user.Id && ExchangeOffer.SenderId != user.Id)
            {
                return Result<IEnumerable<BookResponse>>.BadRequest("This exchange offer does not belong to the current user");
            }
            var books = await _offeredBookRepositoryAsync.GetOfferedBooksWithDetailsByExchangeOfferIdAsync(exchangeOfferId);
            var result = books.Select(
                b => new BookResponse
                {
                    Id = b.BookId,
                    Title = b.Book.Title,
                    Author = b.Book.Author,
                    ISBN = b.Book.ISBN ?? string.Empty,
                    Description = b.Book.Description ??
                    string.Empty,
                    ImageUrl = b.Book.CoverImageUrl,
                    IsAvailable = b.Book.IsAvailable,
                    IsApproved = b.Book.IsApproved,
                    RejectionReason = b.Book.RejectionReason,
                    OwnerId = b.Book.OwnerId,
                    OwnerName = b.Book.Owner.FirstName + " "+ b.Book.Owner.LastName ?? string.Empty,
                    Condition = b.Book.Condition,
                    CategoryId = b.Book.CategoryId,
                    CategoryName = b.Book.Category?.Name ?? string.Empty,
                    Status = b.Book.Status
                });
            return Result<IEnumerable<BookResponse>>.Success(result);
        }

        public async Task<Result<IEnumerable<BookResponse>>> GetAvailableBooksForExchangeAsync()
        {
            var user = await _currentUserService.GetUserAsync();
            var books = await _bookRepository.GetTableNoTracking()
                .Where(b => b.OwnerId == user.Id && b.IsApproved && b.IsAvailable && b.Status == BookStatus.Available)
                .Include(b=>b.Owner)
                .ToListAsync();

            var result = books.Select(b => new BookResponse
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                ISBN = b.ISBN ?? string.Empty,
                Description = b.Description ?? string.Empty,
                ImageUrl = b.CoverImageUrl,
                IsAvailable = b.IsAvailable,
                IsApproved = b.IsApproved,
                OwnerId = b.OwnerId,
                OwnerName = b.Owner.FirstName +" "+ b.Owner.LastName ?? string.Empty
            });
            if (result.Count()==0)
                 return Result<IEnumerable<BookResponse>>.NotFound("Not Found Any books available to Exchange");
            return Result<IEnumerable<BookResponse>>.Success(result, "Available books for exchange retrieved successfully");

        }
    }
}