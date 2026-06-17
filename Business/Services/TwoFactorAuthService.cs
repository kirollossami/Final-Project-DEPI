using Business.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Business.Services;

public class TwoFactorAuthService : ITwoFactorAuthService
{
    public string GenerateSecret()
    {
        var bytes = new byte[20];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Base32Encode(bytes);
    }

    public string GenerateQrCodeUri(string email, string secret)
    {
        return $"otpauth://totp/StudentHousing:{email}?secret={secret}&issuer=StudentHousing";
    }

    public bool VerifyCode(string secret, string code)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
        {
            return false;
        }

        var key = Base32Decode(secret);
        var currentStep = GetCurrentTimeStep();
        
        // Check current step and adjacent steps (allowing for time drift)
        for (int i = -1; i <= 1; i++)
        {
            var step = currentStep + i;
            var expectedCode = GenerateTotp(key, step);
            if (expectedCode == code)
            {
                return true;
            }
        }
        
        return false;
    }

    private long GetCurrentTimeStep()
    {
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unixTimestamp / 30; // 30-second time step
    }

    private string GenerateTotp(byte[] key, long step)
    {
        var stepBytes = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(stepBytes);
        }

        using (var hmac = new HMACSHA1(key))
        {
            var hash = hmac.ComputeHash(stepBytes);
            var offset = hash[hash.Length - 1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24) |
                        ((hash[offset + 1] & 0xFF) << 16) |
                        ((hash[offset + 2] & 0xFF) << 8) |
                        (hash[offset + 3] & 0xFF);
            var otp = binary % 1000000;
            return otp.ToString().PadLeft(6, '0');
        }
    }

    private string Base32Encode(byte[] data)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder();
        
        for (int i = 0; i < data.Length; i += 5)
        {
            int byteCount = Math.Min(5, data.Length - i);
            ulong buffer = 0;
            
            for (int j = 0; j < byteCount; j++)
            {
                buffer = (buffer << 8) | data[i + j];
            }
            
            int bitCount = byteCount * 8;
            int charCount = (bitCount + 4) / 5;
            
            for (int j = 0; j < charCount; j++)
            {
                int index = (int)((buffer >> (bitCount - 5)) & 0x1F);
                result.Append(base32Chars[index]);
                bitCount -= 5;
            }
        }
        
        return result.ToString();
    }

    private byte[] Base32Decode(string base32)
    {
        const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        base32 = base32.ToUpper().Replace(" ", "");
        
        var bits = new List<bool>();
        foreach (var c in base32)
        {
            var index = base32Chars.IndexOf(c);
            if (index < 0) continue;
            
            for (int i = 4; i >= 0; i--)
            {
                bits.Add((index & (1 << i)) != 0);
            }
        }
        
        var bytes = new List<byte>();
        for (int i = 0; i + 7 < bits.Count; i += 8)
        {
            byte b = 0;
            for (int j = 0; j < 8; j++)
            {
                if (bits[i + j])
                    b |= (byte)(1 << (7 - j));
            }
            bytes.Add(b);
        }
        
        return bytes.ToArray();
    }
}
