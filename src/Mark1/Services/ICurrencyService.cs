namespace Mark1.Services
{
    public interface ICurrencyService
    {
        /// <summary>Returns CRC-per-USD rate: the live rate from api.frankfurter.dev when useLiveRate is true and reachable, otherwise fixedRate.</summary>
        Task<decimal> GetUsdToCrcRateAsync(decimal fixedRate, bool useLiveRate);
    }
}
