using BookSwap.Application.Dtos.Book.Response;
using BookSwap.Application.Dtos.Book;
using BookSwap.Core.Results;
using BookSwap.Application.Dtos.Book.Request;

namespace BookSwap.Application.Abstracts
{
    public interface IBookService
    {
        Task<Result<BookResponse>> CreateBookAsync(CreateBookRequest request);
        Task<Result<BookResponse>> UpdateBookAsync(UpdateBookRequest request);
        Task<Result> DeleteBookAsync(int bookId);
        Task<Result<BookResponse>> GetBookByIdAsync(int bookId);
        Task<Result<IEnumerable<BookResponse>>> GetAllApprovedBooksAsync();
        Task<Result<IEnumerable<BookResponse>>> GetPendingApprovalBooksAsync();
        Task<Result<IEnumerable<BookResponse>>> GetBooksForUserAsync();
        Task<Result<IEnumerable<BookResponse>>> SearchBooksAsync(string? searchTerm);
        Task<Result> ApproveBookAsync(ApproveBookRequest request);
        Task<Result<IEnumerable<BookResponse>>> GetRejectedBooksAsync();
        Task<Result<IEnumerable<BookResponse>>> GetRejectedBooksForUserAsync();
        Task<Result<IEnumerable<BookResponse>>> GetOfferedBooksByExchangeOfferId(int exchangeOfferId);
        Task<Result<IEnumerable<BookResponse>>> GetAvailableBooksForExchangeAsync();
    }
}
