using System.Security.Cryptography;
using ParcelDeliverySystem.Repositories.Interfaces;
using ParcelDeliverySystem.Services.Interfaces;

namespace ParcelDeliverySystem.Services.Implementations;

/// <summary>
/// Generates unique, enterprise-formatted tracking codes (e.g. PDS-20261003-0001).
/// Validates against MongoDB to eliminate collisions.
/// </summary>
public class TrackingIdGenerator : ITrackingIdGenerator
{
    private readonly IParcelRepository _parcelRepository;
    private static int _dailySequence = 0;
    private static readonly object _lock = new();
    private static string _lastDate = string.Empty;

    public TrackingIdGenerator(IParcelRepository parcelRepository)
    {
        _parcelRepository = parcelRepository;
    }

    public async Task<string> GenerateTrackingIdAsync()
    {
        var datePrefix = DateTime.UtcNow.ToString("yyyyMMdd");

        for (int attempt = 0; attempt < 10; attempt++)
        {
            int seqNumber;
            lock (_lock)
            {
                if (_lastDate != datePrefix)
                {
                    _lastDate = datePrefix;
                    _dailySequence = 1;
                }
                else
                {
                    _dailySequence++;
                }
                seqNumber = _dailySequence;
            }

            // Create 4-character suffix with sequence + entropy
            var randomBytes = new byte[2];
            RandomNumberGenerator.Fill(randomBytes);
            var randomSuffix = BitConverter.ToString(randomBytes).Replace("-", "").Substring(0, 2);

            var candidateId = $"PDS-{datePrefix}-{seqNumber:D4}{randomSuffix}";

            var existing = await _parcelRepository.GetByTrackingIdAsync(candidateId);
            if (existing == null)
            {
                return candidateId;
            }
        }

        // Fallback with GUID segment if multiple collisions occur
        return $"PDS-{datePrefix}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}";
    }
}
