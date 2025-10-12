using BookSwap.Application.Abstracts;
using BookSwap.Application.Dtos.Book.Response;
using BookSwap.Application.Dtos.ExchangeOffer.Request;
using BookSwap.Application.Dtos.ExchangeOffer.Response;
using BookSwap.Core.Entities;
using BookSwap.Core.Enums;
using BookSwap.Core.Results;
using BookSwap.Infrastructure.Abstracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Tls;

namespace BookSwap.Application.Implementations
{
    public class ExchangeOfferService : IExchangeOfferService
    {
        private readonly IExchangeOfferRepositoryAsync _exchangeOfferRepository;
        private readonly IBookRepositoryAsync _bookRepository;
        private readonly IUserRepositoryAsync _userRepository;
        private readonly IWishlistItemRepositoryAsync _wishlistItemRepo;
        private readonly IOfferedBookRepositoryAsync _offeredBookRepository;
        private readonly IBookOwnershipHistoryRepositoryAsync _bookOwnershipHistoryRepository;
        public ICurrentUserService _currentUserService { get; }

        public ExchangeOfferService(IExchangeOfferRepositoryAsync exchangeOfferRepository, 
                                    IBookRepositoryAsync bookRepository, 
                                    IUserRepositoryAsync userRepositoryAsync, 
                                    ICurrentUserService currentUserService,
                                    IOfferedBookRepositoryAsync offeredBookRepositoryAsync, 
                                    IWishlistItemRepositoryAsync wishlistItemRepository, 
                                    IBookOwnershipHistoryRepositoryAsync bookOwnershipHistoryRepository)
        {
            _exchangeOfferRepository = exchangeOfferRepository;
            _bookRepository = bookRepository;
            _wishlistItemRepo = wishlistItemRepository;
            _userRepository = userRepositoryAsync;
            _currentUserService = currentUserService;
            _offeredBookRepository = offeredBookRepositoryAsync;
            _bookOwnershipHistoryRepository = bookOwnershipHistoryRepository;
        }


        public async Task<Result<ExchangeOfferResponse>> CreateExchangeOfferAsync(CreateExchangeOfferRequest request)
        {
            var sender = await _currentUserService.GetUserAsync();
            using var transaction = await _exchangeOfferRepository.BeginTransactionAsync();


            try
            {

                var requestedBook = await _bookRepository.GetByIdAsync(request.RequestedBookId);
                if (requestedBook == null)
                {
                    return Result<ExchangeOfferResponse>.NotFound("Requested book not found");
                }

                if (requestedBook.OwnerId == sender.Id)
                {
                    return Result<ExchangeOfferResponse>.BadRequest("You cannot request your own book");
                }
                if (!requestedBook.IsAvailable)
                {
                    return Result<ExchangeOfferResponse>.BadRequest("Requested book UnAvailable");
                }
                if (!requestedBook.IsApproved)
                {
                    return Result<ExchangeOfferResponse>.BadRequest("Requested book UnApproved");
                }

                //if (await _exchangeOfferRepository.HasAcceptedExchangeAsync(request.RequestedBookId))
                //{
                //    return Result<ExchangeOfferResponse>.BadRequest("This book already has an accepted exchange offer");
                //}


                if (request.OfferedBookIds == null || !request.OfferedBookIds.Any())
                    return Result<ExchangeOfferResponse>.BadRequest("You must offer at least one book");


                var OfferedBooks = await _bookRepository.GetBooksByIdsAsync(request.OfferedBookIds);

                if (OfferedBooks.Count != request.OfferedBookIds.Count)
                {
                    return Result<ExchangeOfferResponse>.NotFound("some offered books not found");
                }


                if (OfferedBooks.Any(e => e.OwnerId != sender.Id))
                {
                    return Result<ExchangeOfferResponse>.BadRequest("You can only offer books you own");
                }
                if (OfferedBooks.Any(e => e.IsAvailable == false))
                {
                    return Result<ExchangeOfferResponse>.BadRequest("some offered books unAvailable ");
                }
                if (OfferedBooks.Any(e => e.IsApproved == false))
                {
                    return Result<ExchangeOfferResponse>.BadRequest("some offered books unApproved");
                }

                //foreach (var book in OfferedBooks)
                //{

                //    if (await _exchangeOfferRepository.HasAcceptedExchangeAsync(book.Id))
                //    {
                //        return Result<ExchangeOfferResponse>.BadRequest("book already has an accepted exchange offer");
                //    }


                //}


                var newExchangeOffer = new ExchangeOffer
                {
                    SenderId = sender.Id,
                    ReceiverId = requestedBook.OwnerId,
                    RequestedBookId = requestedBook.Id,
                    CreatedAt = DateTime.UtcNow,
                    Status = ExchangeOfferStatus.Pending,
                    OfferedBooks = request.OfferedBookIds.Select(id => new OfferedBook
                    {
                        BookId = id,
                        IsSelected = false
                    }


                ).ToList()
                };
                if (request.WishlistItemId.HasValue)
                {
                    var wishITem = await _wishlistItemRepo.GetWishlistItemByIdAsync(request.WishlistItemId.Value);
                    if (wishITem is null)
                        return Result<ExchangeOfferResponse>.NotFound("Wish Item Not Found");
                    newExchangeOffer.WishlistItemId = request.WishlistItemId;
                    wishITem.Status = WishStatus.PendingExchange;
                    await _wishlistItemRepo.UpdateAsync(wishITem);
                }
                await _exchangeOfferRepository.AddAsync(newExchangeOffer);


                requestedBook.Status = BookStatus.PendingExchange;
                requestedBook.IsAvailable = false;
                var Time = DateTime.UtcNow;
                requestedBook.UpdatedAt = Time;
                await _bookRepository.UpdateAsync(requestedBook);
                foreach (var book in OfferedBooks)
                {
                    book.Status = BookStatus.PendingExchange;
                    book.IsAvailable = false;
                    book.UpdatedAt = Time;


                }
                await _bookRepository.UpdateRangeAsync(OfferedBooks);


                var senderName = await _userRepository.GetUserNameByUserIdAsync(sender.Id);
                var receiverName = await _userRepository.GetUserNameByUserIdAsync(requestedBook.OwnerId);




                await _exchangeOfferRepository.CommitAsync();
                return Result<ExchangeOfferResponse>.Success(new ExchangeOfferResponse
                {
                    Id = newExchangeOffer.Id,
                    SenderId = sender.Id,
                    SenderName = senderName ?? "",
                    ReceiverId = requestedBook.OwnerId,
                    ReceiverName = receiverName ?? "",
                    RequestedBookId = request.RequestedBookId,
                    RequestedBookTitle = requestedBook.Title,
                    OfferedBooks = OfferedBooks.Select(b => new OfferedBookResponse
                    {
                        BookId = b.Id,
                        Title = b.Title,
                        Author = b.Author,
                        ImageUrl = b.CoverImageUrl,
                        Condition = b.Condition,

                        IsSelected = false
                    }).ToList(),

                    Status = newExchangeOffer.Status,
                    CreatedAt = newExchangeOffer.CreatedAt,

                },
                "Exchange offer created successfully");
            }
            catch (Exception ex)
            {
                await _exchangeOfferRepository.RollBackAsync();
                return Result<ExchangeOfferResponse>.BadRequest(ex.Message);
            }

        }

        public async Task<Result<AcceptExchangeOfferResponse>> AccepteExchangeOfferRequest(AcceptExchangeOfferRequest request)
        {
            var reciver = await _currentUserService.GetUserAsync();
            using var transaction = await _exchangeOfferRepository.BeginTransactionAsync();


            try
            {

                var ExchangeOffer = await _exchangeOfferRepository.GetOfferByIdAsync(request.ExchangeOfferId);
                if (ExchangeOffer == null)
                    return Result<AcceptExchangeOfferResponse>.NotFound("Exchange offer not found");

                if (ExchangeOffer.Status != ExchangeOfferStatus.Pending)
                {
                    return Result<AcceptExchangeOfferResponse>.BadRequest($"This exchange offer is already {ExchangeOffer.Status}");
                }

                if (ExchangeOffer.ReceiverId != reciver.Id)
                {
                    return Result<AcceptExchangeOfferResponse>.BadRequest("This exchange offer does not belong to the current user");

                }


                var offeredBooks = await _offeredBookRepository.GetOfferedBooksByExchangeOfferIdAsync(request.ExchangeOfferId);
                if (!offeredBooks.Any(e => e.BookId == request.SelectedBookId))
                {
                    return Result<AcceptExchangeOfferResponse>.BadRequest("This book is not part of the offered books for this exchange offer");
                }

                var selectedOfferedBook = offeredBooks.FirstOrDefault(e => e.BookId == request.SelectedBookId)?.Book;


                if (selectedOfferedBook == null)
                {
                    return Result<AcceptExchangeOfferResponse>.NotFound("selected book is not found");
                }

                var RequestedBook = await _bookRepository.GetByIdAsync(ExchangeOffer.RequestedBookId);
                if (RequestedBook == null)
                {
                    return Result<AcceptExchangeOfferResponse>.NotFound("Requested Book  is not found");
                }

                //if (await _exchangeOfferRepository.HasAcceptedExchangeAsync(request.SelectedBookId))
                //{
                //    return Result<AcceptExchangeOfferResponse>.BadRequest("Selectedbook is already has an accepted exchange offer");
                //}

                ExchangeOffer.Status = ExchangeOfferStatus.Accepted;
                selectedOfferedBook.Status = BookStatus.Exchanged;

                RequestedBook.Status = BookStatus.Exchanged;
                var Time = DateTime.UtcNow;
                selectedOfferedBook.UpdatedAt = Time;
                RequestedBook.UpdatedAt = Time;
                foreach (var offeredBook in offeredBooks)
                {
                    if (offeredBook.BookId != request.SelectedBookId)
                    {
                        offeredBook.Book.Status = BookStatus.Available;
                        offeredBook.Book.IsAvailable = true;
                        offeredBook.Book.UpdatedAt = Time;
                    }

                    else
                    {
                        offeredBook.IsSelected = true;
                        offeredBook.Book.UpdatedAt = Time;

                    }
                }
                var senderId = ExchangeOffer.SenderId;
                var senderName = await _userRepository.GetUserNameByUserIdAsync(senderId);
                var receiverName = reciver.FirstName + " " + reciver.LastName;
                int SwapId = selectedOfferedBook.OwnerId;
                selectedOfferedBook.OwnerId = RequestedBook.OwnerId;
                RequestedBook.OwnerId = SwapId;



                var bookOwnerShipHistoryRequest = new BookOwnershipHistory
                {
                    BookId = RequestedBook.Id,
                    PreviousOwnerId = reciver.Id,
                    NewOwnerId = senderId,
                    ExchangeOfferId = ExchangeOffer.Id,
                    TransferDate = Time

                };
                var bookOwnerShipHistoryRecive = new BookOwnershipHistory
                {
                    BookId = request.SelectedBookId,
                    PreviousOwnerId = senderId,
                    NewOwnerId = reciver.Id,
                    ExchangeOfferId = ExchangeOffer.Id,
                    TransferDate = Time
                };

                if(ExchangeOffer.WishlistItemId is not null)
                {
                    ExchangeOffer.WishlistItem.Status = WishStatus.Fulfilled;  
                }


                await _exchangeOfferRepository.UpdateAsync(ExchangeOffer);

                await _bookRepository.UpdateRangeAsync(offeredBooks.Select(e => e.Book).ToList());

                await _offeredBookRepository.UpdateRangeAsync(offeredBooks.ToList());

                await _bookRepository.UpdateAsync(RequestedBook);
                await _bookOwnershipHistoryRepository.AddAsync(bookOwnerShipHistoryRequest);
                await _bookOwnershipHistoryRepository.AddAsync(bookOwnerShipHistoryRecive);


                await _exchangeOfferRepository.CommitAsync();

                return Result<AcceptExchangeOfferResponse>.Success(new AcceptExchangeOfferResponse()
                {
                    Id = request.ExchangeOfferId,
                    SenderId = senderId,
                    SenderName = senderName ?? "",
                    RequestedBookId = ExchangeOffer.RequestedBookId,
                    RequestedBookTitle = RequestedBook.Title,
                    RequestedBookImage = RequestedBook.CoverImageUrl,
                    ReceiverId = reciver.Id,
                    ReceiverName = receiverName ?? "",
                    OfferBookId = request.SelectedBookId,
                    OfferedBookTitle = selectedOfferedBook.Title,
                    OfferedBookImage = selectedOfferedBook.CoverImageUrl
                }, "Exchange offer has been accepted successfully");


            }

            catch (Exception ex)
            {
                await _exchangeOfferRepository.RollBackAsync();
                return Result<AcceptExchangeOfferResponse>.BadRequest(ex.Message);
            }
        }

        public async Task<Result<bool>> CancelExchangeOfferRequest(CancelExchangeOfferRequest request)
        {
            var sender = await _currentUserService.GetUserAsync();           
            using var transaction = await _exchangeOfferRepository.BeginTransactionAsync();

            try
            {

                var ExchangeOffer = await _exchangeOfferRepository.GetOfferByIdAsync(request.ExchangeOfferId);
                if (ExchangeOffer == null)
                {
                    return Result<bool>.BadRequest("Exchange Offer not found");
                }
                if (ExchangeOffer.SenderId != sender.Id)
                {
                    return Result<bool>.Failure("You are not authorized to Cancelled this Exchangeoffer", failureType: ResultFailureType.Forbidden);
                }
                if (ExchangeOffer.Status != ExchangeOfferStatus.Pending)
                {
                    return Result<bool>.BadRequest($"This exchange offer is already {ExchangeOffer.Status}");
                }
                var requestBook = await _bookRepository.GetByIdAsync(ExchangeOffer.RequestedBookId);
                var OfferedBooks = await _offeredBookRepository.GetOfferedBooksByExchangeOfferIdAsync(request.ExchangeOfferId);
                if (requestBook == null)
                    return Result<bool>.NotFound("Requested book not found");
                if (!OfferedBooks.Any())
                    return Result<bool>.NotFound("No offered books found for this exchange offer");
                var Time = DateTime.UtcNow;

                requestBook.IsAvailable = true;
                requestBook.Status = BookStatus.Available;
                requestBook.UpdatedAt = Time;
                foreach (var OfferedBook in OfferedBooks)
                {
                    OfferedBook.Book.IsAvailable = true;
                    OfferedBook.Book.Status = BookStatus.Available;
                    OfferedBook.Book.UpdatedAt = Time;

                }
                ExchangeOffer.Status = ExchangeOfferStatus.Cancelled;
                await _bookRepository.UpdateAsync(requestBook);
                await _bookRepository.UpdateRangeAsync(OfferedBooks.Select(e => e.Book).ToList());
                await _exchangeOfferRepository.UpdateAsync(ExchangeOffer);
                if (ExchangeOffer.WishlistItemId is not null)
                {
                    ExchangeOffer.WishlistItem.Status = WishStatus.Public;
                }
                await _exchangeOfferRepository.CommitAsync();
                return Result<bool>.Success(true, "Cancelled Exchange offer has been successfully");

            }
            catch (Exception ex)
            {
                await _exchangeOfferRepository.RollBackAsync();
                return Result<bool>.BadRequest(ex.Message);
            }



        }

        public async Task<Result<bool>> RejectedExchangeOfferRequest(CancelExchangeOfferRequest request)
        {
            var reciver = await _currentUserService.GetUserAsync();
            using var transaction = await _exchangeOfferRepository.BeginTransactionAsync();
            try
            {
                var ExchangeOffer = await _exchangeOfferRepository.GetOfferByIdAsync(request.ExchangeOfferId);
                if (ExchangeOffer == null)
                {
                    return Result<bool>.BadRequest("Exchange Offer not found");
                }
                if (ExchangeOffer.ReceiverId != reciver.Id)
                {
                    return Result<bool>.Failure("You are not authorized to  Rejected this Exchangeoffer", failureType: ResultFailureType.Forbidden);
                }
                if (ExchangeOffer.Status != ExchangeOfferStatus.Pending)
                {
                    return Result<bool>.BadRequest($"This exchange offer is already {ExchangeOffer.Status}");
                }
                var requestBook = await _bookRepository.GetByIdAsync(ExchangeOffer.RequestedBookId);
                var OfferedBooks = await _offeredBookRepository.GetOfferedBooksByExchangeOfferIdAsync(request.ExchangeOfferId);
                if (requestBook == null)
                    return Result<bool>.NotFound("Requested book not found");
                if (!OfferedBooks.Any())
                    return Result<bool>.NotFound("No offered books found for this exchange offer");
                var Time = DateTime.UtcNow;

                requestBook.IsAvailable = true;
                requestBook.Status = BookStatus.Available;
                requestBook.UpdatedAt = Time;
                foreach (var OfferedBook in OfferedBooks)
                {
                    OfferedBook.Book.IsAvailable = true;
                    OfferedBook.Book.Status = BookStatus.Available;
                    OfferedBook.Book.UpdatedAt = Time;
                }
                ExchangeOffer.Status = ExchangeOfferStatus.Rejected;
                await _bookRepository.UpdateAsync(requestBook);

                await _bookRepository.UpdateRangeAsync(OfferedBooks.Select(e => e.Book).ToList());
                await _exchangeOfferRepository.UpdateAsync(ExchangeOffer);

                if (ExchangeOffer.WishlistItemId is not null)
                {
                    ExchangeOffer.WishlistItem.Status = WishStatus.Public;
                }
                await _exchangeOfferRepository.CommitAsync();
                return Result<bool>.Success(true, "Rejected Exchange offer has been successfully");
            }
            catch (Exception ex)
            {
                await _exchangeOfferRepository.RollBackAsync();
                return Result<bool>.BadRequest(ex.Message);
            }

        }
        public async Task<Result<IEnumerable<ExchangeOfferResponse>>> GetMyOffersSentByStatusAsync(ExchangeOfferStatus status)
        {
            var user = await _currentUserService.GetUserAsync();
            var offers = await _exchangeOfferRepository.GetMyOffersSentByStatusAsync(status, user.Id);
            var result = offers.Select(e => new ExchangeOfferResponse
            {
                Id = e.Id,
                SenderId = e.SenderId,
                SenderName = e.Sender.FirstName + e.Sender.LastName ?? "",
                ReceiverId = e.ReceiverId,
                ReceiverName = e.Receiver.FirstName + e.Receiver.LastName ?? "",
                RequestedBookId = e.RequestedBookId,
                RequestedBookTitle = e.RequestedBook.Title,
                OfferedBooks = e.OfferedBooks.Select(OfferedBook => new OfferedBookResponse
                {
                    BookId = OfferedBook.Book.Id,
                    Title = OfferedBook.Book.Title,
                    Author = OfferedBook.Book.Author,
                    ImageUrl = OfferedBook.Book.CoverImageUrl,
                    Condition = OfferedBook.Book.Condition,
                    IsSelected = OfferedBook.IsSelected
                }).ToList(),

                Status = e.Status,
                CreatedAt = e.CreatedAt,
            });
            return Result<IEnumerable<ExchangeOfferResponse>>.Success(result);
        }
        public async Task<Result<IEnumerable<ExchangeOfferResponse>>> GetMyOffersReceivedByStatusAsync(ExchangeOfferStatus status)
        {
            var user = await _currentUserService.GetUserAsync();
            var offers = await _exchangeOfferRepository.GetMyOffersReceivedByStatusAsync(status, user.Id);
            var result = offers.Select(e => new ExchangeOfferResponse
            {
                Id = e.Id,
                SenderId = e.SenderId,
                SenderName = e.Sender.FirstName + e.Sender.LastName ?? "",
                ReceiverId = e.ReceiverId,
                ReceiverName = e.Receiver.FirstName + e.Receiver.LastName ?? "",
                RequestedBookId = e.RequestedBookId,
                RequestedBookTitle = e.RequestedBook.Title,
                OfferedBooks = e.OfferedBooks.Select(OfferedBook => new OfferedBookResponse
                {
                    BookId = OfferedBook.Book.Id,
                    Title = OfferedBook.Book.Title,
                    Author = OfferedBook.Book.Author,
                    ImageUrl = OfferedBook.Book.CoverImageUrl,
                    Condition = OfferedBook.Book.Condition,
                    IsSelected = OfferedBook.IsSelected
                }).ToList(),

                Status = e.Status,
                CreatedAt = e.CreatedAt,
            });
            return Result<IEnumerable<ExchangeOfferResponse>>.Success(result);
        }
    }
}
