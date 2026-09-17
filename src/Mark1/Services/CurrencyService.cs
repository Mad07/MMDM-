using System.Text.Json;

namespace Mark1.Services
{
    /// <summary>
    /// Looks up the live USD-to-CRC rate from api.frankfurter.dev. Any failure (network, timeout,
    /// unexpected response) is swallowed and the caller's fixed rate is returned instead - this is
    /// meant to degrade silently, never to break a page load.
    /// </summary>
    public class CurrencyService : ICurrencyService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CurrencyService> _logger;

        public CurrencyService(HttpClient httpClient, ILogger<CurrencyService> logger)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            _logger = logger;
        }

        public async Task<decimal> GetUsdToCrcRateAsync(decimal fixedRate, bool useLiveRate)
        {
            if (!useLiveRate)
            {
                return fixedRate;
            }

            try
            {
                var response = await _httpClient.GetAsync("https://api.frankfurter.dev/v1/latest?base=USD&symbols=CRC");
                if (!response.IsSuccessStatusCode)
                {
                    return fixedRate;
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                if (doc.RootElement.TryGetProperty("rates", out var rates) &&
                    rates.TryGetProperty("CRC", out var crc))
                {
                    return crc.GetDecimal();
                }

                return fixedRate;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live exchange rate lookup failed, falling back to fixed rate.");
                return fixedRate;
            }
        }
    }
}
