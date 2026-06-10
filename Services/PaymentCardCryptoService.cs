using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using otelturizmnew.Models.Payments;

namespace otelturizmnew.Services;

public sealed class PaymentCardCryptoService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[]? _key;
    private readonly ILogger<PaymentCardCryptoService> _logger;

    public bool IsConfigured => _key is not null;

    public PaymentCardCryptoService(IConfiguration configuration, ILogger<PaymentCardCryptoService> logger)
    {
        _logger = logger;
        var keyBase64 = configuration["PaymentCards:EncryptionKeyBase64"];
        if (string.IsNullOrWhiteSpace(keyBase64))
        {
            _logger.LogWarning("PaymentCards:EncryptionKeyBase64 tanimli degil; kayitli kart sifreleme devre disi.");
            return;
        }

        try
        {
            var key = Convert.FromBase64String(keyBase64.Trim());
            if (key.Length != 32)
            {
                _logger.LogWarning("PaymentCards:EncryptionKeyBase64 32 bayt (AES-256) olmali; kayitli kart sifreleme devre disi.");
                return;
            }

            _key = key;
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "PaymentCards:EncryptionKeyBase64 gecersiz; kayitli kart sifreleme devre disi.");
        }
    }

    public byte[] Encrypt(PaymentCardPayload payload)
    {
        if (_key is null)
        {
            throw new InvalidOperationException("Kayitli kart sifreleme yapilandirilmamis.");
        }

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(_key!, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var packed = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, packed, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, packed, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, packed, NonceSize + TagSize, ciphertext.Length);
        return packed;
    }

    public PaymentCardPayload Decrypt(byte[] encryptedPayload)
    {
        if (_key is null)
        {
            throw new InvalidOperationException("Kayitli kart sifreleme yapilandirilmamis.");
        }

        if (encryptedPayload.Length <= NonceSize + TagSize)
        {
            throw new CryptographicException("Sifreli kart verisi gecersiz.");
        }

        var nonce = encryptedPayload.AsSpan(0, NonceSize);
        var tag = encryptedPayload.AsSpan(NonceSize, TagSize);
        var ciphertext = encryptedPayload.AsSpan(NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key!, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return JsonSerializer.Deserialize<PaymentCardPayload>(plaintext)
            ?? throw new CryptographicException("Kart verisi cozulemedi.");
    }

    public static string CreateToken()
        => Guid.NewGuid().ToString("N");

    public static string MaskPan(string pan)
    {
        var digits = new string((pan ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
        {
            return "****";
        }

        return $"**** **** **** {digits[^4..]}";
    }

    public static string DetectBrand(string pan)
    {
        var digits = new string((pan ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith('4'))
        {
            return "Visa";
        }

        if (digits.Length >= 2 && int.TryParse(digits[..2], out var prefix2) && prefix2 is >= 51 and <= 55)
        {
            return "Mastercard";
        }

        if (digits.StartsWith("6011", StringComparison.Ordinal) || digits.StartsWith('5'))
        {
            return "Mastercard";
        }

        return "Kart";
    }

    public static bool IsValidPan(string pan)
    {
        var digits = new string((pan ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length is < 13 or > 19)
        {
            return false;
        }

        var sum = 0;
        var alternate = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9)
                {
                    n -= 9;
                }
            }

            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }

    public static string ExtractLastFour(string pan)
    {
        var digits = new string((pan ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? digits[^4..] : digits.PadLeft(4, '0');
    }
}
