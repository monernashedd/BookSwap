using BookSwap.Core.Entities;
using BookSwap.Core.Enums;
using BookSwap.Infrastructure.InfrastructureBases;

namespace BookSwap.Infrastructure.Abstracts
{
    public interface IExchangeOfferRepositoryAsync : IGenericRepositoryAsync<ExchangeOffer>
    {
        // Task<bool> HasAcceptedExchangeAsync(int bookId);
        Task<IEnumerable<ExchangeOffer>> GetByUserAsync(int userId);
        Task<IEnumerable<ExchangeOffer>> GetOffersBySenderAsync(int senderId);
        Task<IEnumerable<ExchangeOffer>> GetOffersByReceiverAsync(int receiverId);
        Task<IEnumerable<ExchangeOffer>> GetMyOffersByStatusAsync(ExchangeOfferStatus status, int userId);
        Task<IEnumerable<ExchangeOffer>> GetMyOffersReceivedByStatusAsync(ExchangeOfferStatus status, int reciverId);
        Task<IEnumerable<ExchangeOffer>> GetMyOffersSentByStatusAsync(ExchangeOfferStatus status, int SenderId);

    }
}
