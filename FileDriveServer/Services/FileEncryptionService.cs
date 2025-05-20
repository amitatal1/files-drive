using System.Security.Cryptography;

namespace FileDriveServer.Services
{
    public class FileEncryptionService
    {
        private readonly byte[] _masterEncryptionKey;
        private const int AES_KEY_SIZE_BITS = 256; // AES-256
        private const int GCM_TAG_SIZE_BITS = 128; // Standard for AES-GCM
        private const int GCM_IV_SIZE_BYTES = 12; // Standard for AES-GCM (96 bits)

        public FileEncryptionService(string masterKeyBase64)
        {
            if (string.IsNullOrEmpty(masterKeyBase64))
            {
                throw new ArgumentNullException(nameof(masterKeyBase64), "Master encryption key cannot be null or empty.");
            }
            try
            {
                _masterEncryptionKey = Convert.FromBase64String(masterKeyBase64);
                if (_masterEncryptionKey.Length * 8 != AES_KEY_SIZE_BITS)
                {
                    throw new ArgumentException($"Master key must be {AES_KEY_SIZE_BITS} bits (32 bytes) when Base64 decoded.", nameofmasterKeyBase64);
                }
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Master key is not a valid Base64 string.", nameof(masterKeyBase64), ex);
            }
        }

        public byte[] EncryptFileKey(byte[] fileKey)
        {
            byte[] iv;
            byte[] authTag;
            byte[] encryptedFileKey = EncryptData(fileKey, _masterEncryptionKey, out iv, out authTag);
            return CombineBytes(encryptedFileKey, iv, authTag);
        }

        public byte[] DecryptFileKey(byte[] combinedEncryptedFileKey)
        {
            // Separate the parts
            // Assuming the structure: encryptedKey | IV | AuthTag
            // This needs to match the EncryptFileKey logic
            int keyLength = AES_KEY_SIZE_BITS / 8; // 32 bytes
            int ivLength = GCM_IV_SIZE_BYTES; // 12 bytes
            int tagLength = GCM_TAG_SIZE_BITS / 8; // 16 bytes

            if (combinedEncryptedFileKey.Length != keyLength + ivLength + tagLength)
            {
                throw new ArgumentException("Combined encrypted file key has incorrect length.");
            }

            byte[] encryptedKeyPart = new byte[keyLength];
            byte[] ivPart = new byte[ivLength];
            byte[] tagPart = new byte[tagLength];

            Buffer.BlockCopy(combinedEncryptedFileKey, 0, encryptedKeyPart, 0, keyLength);
            Buffer.BlockCopy(combinedEncryptedFileKey, keyLength, ivPart, 0, ivLength);
            Buffer.BlockCopy(combinedEncryptedFileKey, keyLength + ivLength, tagPart, 0, tagLength);

            return DecryptData(encryptedKeyPart, _masterEncryptionKey, ivPart, tagPart);
        }

        // --- Data Encryption/Decryption (using per-file keys) ---

        // Generates a new random AES-256 key
        public byte[] GenerateNewFileKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] key = new byte[AES_KEY_SIZE_BITS / 8]; // 32 bytes for AES-256
                rng.GetBytes(key);
                return key;
            }
        }

        // Encrypts data using AES-256 GCM
        public byte[] EncryptData(byte[] data, byte[] key, out byte[] iv, out byte[] authTag)
        {
            if (data == null || data.Length == 0) throw new ArgumentNullException(nameof(data));
            if (key == null || key.Length != AES_KEY_SIZE_BITS / 8) throw new ArgumentException($"Key must be {AES_KEY_SIZE_BITS / 8} bytes.", nameof(key));

            using (AesGcm aesGcm = new AesGcm(key))
            {
                iv = new byte[GCM_IV_SIZE_BYTES]; // 12 bytes IV recommended for GCM
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv); // Generate a unique IV for each encryption
                }

                authTag = new byte[GCM_TAG_SIZE_BITS / 8]; // 16 bytes authentication tag
                byte[] cipherBytes = new byte[data.Length];

                aesGcm.Encrypt(iv, data, cipherBytes, authTag);

                return cipherBytes;
            }
        }

        // Decrypts data using AES-256 GCM
        public byte[] DecryptData(byte[] cipherBytes, byte[] key, byte[] iv, byte[] authTag)
        {
            if (cipherBytes == null || cipherBytes.Length == 0) throw new ArgumentNullException(nameof(cipherBytes));
            if (key == null || key.Length != AES_KEY_SIZE_BITS / 8) throw new ArgumentException($"Key must be {AES_KEY_SIZE_BITS / 8} bytes.", nameof(key));
            if (iv == null || iv.Length != GCM_IV_SIZE_BYTES) throw new ArgumentException($"IV must be {GCM_IV_SIZE_BYTES} bytes.", nameof(iv));
            if (authTag == null || authTag.Length != GCM_TAG_SIZE_BITS / 8) throw new ArgumentException($"AuthTag must be {GCM_TAG_SIZE_BITS / 8} bytes.", nameof(authTag));

            using (AesGcm aesGcm = new AesGcm(key))
            {
                byte[] plainBytes = new byte[cipherBytes.Length];
                try
                {
                    aesGcm.Decrypt(iv, cipherBytes, authTag, plainBytes);
                    return plainBytes;
                }
                catch (CryptographicException ex)
                {
                    // This typically means tampering or incorrect key/IV/tag
                    throw new CryptographicException("Decryption failed. Data may be corrupted or tampered with.", ex);
                }
            }
        }

        // Helper to combine bytes for storage (e.g., encrypted key, IV, tag for the master key encryption)
        private byte[] CombineBytes(byte[] a, byte[] b, byte[] c)
        {
            byte[] combined = new byte[a.Length + b.Length + c.Length];
            Buffer.BlockCopy(a, 0, combined, 0, a.Length);
            Buffer.BlockCopy(b, 0, combined, a.Length, b.Length);
            Buffer.BlockCopy(c, 0, combined, a.Length + b.Length, c.Length);
            return combined;
        }
    }
}