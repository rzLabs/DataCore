using System;
using System.Text;

namespace DataCore.Functions
{
    /// <summary>
    /// Provides Encryption, Decryption and Information regarding the Rappelz data.xxx naming/encryption systems.
    /// </summary>
    public static class StringCipher
    {
        static byte[] decryptLastCharTable = new byte[] {
         0x54, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
         0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
         0x00, 0x4b, 0x00, 0x16, 0x1c, 0x49, 0x01, 0x1d, 0x2a, 0x42, 0x00, 0x11, 0x12, 0x27, 0x00, 0x00,
         0x20, 0x4d, 0x33, 0x4f, 0x43, 0x0a, 0x44, 0x39, 0x1b, 0x4a, 0x00, 0x3e, 0x00, 0x3b, 0x00, 0x00,
         0x46, 0x28, 0x25, 0x18, 0x31, 0x48, 0x23, 0x38, 0x2d, 0x14, 0x19, 0x3a, 0x1f, 0x52, 0x04, 0x0e,
         0x21, 0x3d, 0x40, 0x3f, 0x02, 0x15, 0x10, 0x37, 0x2c, 0x0f, 0x2e, 0x4e, 0x00, 0x22, 0x00, 0x03,
         0x0c, 0x35, 0x3c, 0x4c, 0x06, 0x24, 0x50, 0x36, 0x2f, 0x13, 0x47, 0x17, 0x45, 0x51, 0x26, 0x09,
         0x2b, 0x1a, 0x0d, 0x05, 0x41, 0x29, 0x0b, 0x30, 0x08, 0x32, 0x53, 0x07, 0x00, 0x34, 0x1e, 0x00};

        static byte[] decryptTablePhase2 = new byte[0x80] {
         0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
         0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
         0x21, 0x64, 0x00, 0x33, 0x37, 0x2d, 0x23, 0x62, 0x5a, 0x47, 0x00, 0x5f, 0x25, 0x36, 0x2c, 0x00,
         0x43, 0x35, 0x57, 0x70, 0x58, 0x7e, 0x4b, 0x2b, 0x30, 0x4c, 0x00, 0x79, 0x00, 0x52, 0x00, 0x00,
         0x44, 0x48, 0x68, 0x63, 0x61, 0x4d, 0x4e, 0x45, 0x6e, 0x66, 0x65, 0x40, 0x71, 0x59, 0x27, 0x29,
         0x34, 0x6f, 0x53, 0x46, 0x7d, 0x69, 0x38, 0x50, 0x28, 0x3b, 0x74, 0x39, 0x00, 0x32, 0x3d, 0x31,
         0x6a, 0x5e, 0x51, 0x7b, 0x67, 0x2e, 0x6c, 0x20, 0x56, 0x75, 0x42, 0x5b, 0x26, 0x5d, 0x72, 0x73,
         0x6d, 0x6b, 0x76, 0x77, 0x55, 0x78, 0x54, 0x24, 0x49, 0x4a, 0x7a, 0x4f, 0x00, 0x41, 0x60, 0x00};

        static char[] encryptLastCharTable = "^&T_Nsd{xo5v`rOYV+,iIU#kCJq8$\'~L0P]FeBn-Au(pXHZhwDy2}agWG7K=bQ;SRt)46l@jE%9!c1[3fmMz".ToCharArray();

        static byte[] encryptTablePhase2 = new byte[0x80]{
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x67, 0x20, 0x00, 0x26, 0x77, 0x2C, 0x6C, 0x4E, 0x58, 0x4F, 0x00, 0x37, 0x2E, 0x25, 0x65, 0x00, 0x38, 0x5F, 0x5D, 0x23, 0x50, 0x31, 0x2D, 0x24, 0x56, 0x5B, 0x00, 0x59, 0x00, 0x5E, 0x00, 0x00,
        0x4B, 0x7D, 0x6A, 0x30, 0x40, 0x47, 0x53, 0x29, 0x41, 0x78, 0x79, 0x36, 0x39, 0x45, 0x46, 0x7B, 0x57, 0x62, 0x3D, 0x52, 0x76, 0x74, 0x68, 0x32, 0x34, 0x4D, 0x28, 0x6B, 0x00, 0x6D, 0x61, 0x2B,
        0x7E, 0x44, 0x27, 0x43, 0x21, 0x4A, 0x49, 0x64, 0x42, 0x55, 0x60, 0x71, 0x66, 0x70, 0x48, 0x51, 0x33, 0x4C, 0x6E, 0x6F, 0x5A, 0x69, 0X72, 0x73, 0x75, 0x3B, 0x7A, 0x63, 0x00, 0x54, 0x35, 0x00
        };

        #region Shared

        static void prepareHash(ref byte[] hash)
        {
            byte val1, val2;
            int medianPt13 = (int)(0.33 * hash.Length);
            int medianPt23 = (int)(0.66 * hash.Length);

            val1 = hash[medianPt23];
            val2 = hash[medianPt13];

            hash[medianPt23] = hash[0];
            hash[medianPt13] = hash[1];

            hash[0] = val1;
            hash[1] = val2;
        }

        public static bool IsEncoded(string hash) { return Encode(Decode(hash)) == hash; }

        public static bool IsEncoded(byte[] hash)
        {
            string nameHash = ByteConverterExt.ToString(hash);
            return Encode(Decode(hash)) == nameHash;
        }

        #endregion

        #region Decrypt

        static int decryptLastChar(byte c) { return decryptLastCharTable[c]; }

        static void decryptPhase2(ref byte[] hash, int seed)
        {
            int i, j;
            byte computeVar;
            int computeLoop = seed;
            int hashSize = hash.Length;

            for (i = 0; i < hashSize; i++)
            {
                computeVar = hash[i];
                for (j = 0; j < computeLoop; j++)
                {
                    computeVar = decryptTablePhase2[computeVar];
                    if (computeVar == 0x00) { computeVar = 0xFF; }
                }

                hash[i] = computeVar;
                computeLoop = (computeLoop + 17 * computeVar) % 32 + 1;
            }
        }

        public static string Decode(byte[] hash)
        {
            byte[] reducedHash = new byte[hash.Length - 2];
            if (hash.Length == 0) { return null; }

            Array.Copy(hash, 1, reducedHash, 0, hash.Length - 2);

            prepareHash(ref reducedHash);
            decryptPhase2(ref reducedHash, decryptLastChar(hash[hash.Length - 1]));

            return Encoding.ASCII.GetString(reducedHash);
        }

        public static string Decode(string hashName)
        {
            byte[] hash = Encoding.ASCII.GetBytes(hashName);
            byte[] reducedHash = new byte[hash.Length - 2];
            if (hash.Length == 0) { return null; }

            Array.Copy(hash, 1, reducedHash, 0, hash.Length - 2);

            prepareHash(ref reducedHash);
            decryptPhase2(ref reducedHash, decryptLastChar(hash[hash.Length - 1]));

            return Encoding.ASCII.GetString(reducedHash);
        }

        #endregion

        #region Encrypt

        static void encryptNameToHash(ref byte[] name, uint encodedSeed)
        {
            int i, j;
            byte computeVar;
            uint computeLoop = encodedSeed;

            for (i = 0; i < name.Length; i++)
            {
                computeVar = name[i];

                for (j = 0; j < computeLoop; j++) { computeVar = encryptTablePhase2[computeVar]; }

                computeLoop = (uint)(computeLoop + 17 * name[i]) % 32 + 1;
                name[i] = computeVar;
            }
        }

        static int computeLegacySeed(byte[] name)
        {
            int i, computeVar;

            for (i = 0, computeVar = 0; i < name.Length; i++) { computeVar += name[i] * 17; }

            computeVar = (i * 2 + computeVar - 1) % 32 + 1;
            return computeVar;
        }

        static char computeFirstChar(byte[] reducedHash)
        {
            int i, computeVar;

            for (i = 0, computeVar = 0; i < reducedHash.Length; i++) { computeVar = reducedHash[i] + computeVar; }

            return encryptLastCharTable[computeVar % 0x54];
        }

        public static string Encode(string name)
        {
            byte[] nameHash = Encoding.ASCII.GetBytes(name);
            byte[] reducedHash = new byte[name.Length];
            byte[] hash = new byte[nameHash.Length + 2];

            if (name.Length == 0) { return null; }

            int encodeSeed = computeLegacySeed(nameHash);

            Array.Copy(nameHash, reducedHash, nameHash.Length);
            encryptNameToHash(ref reducedHash, (uint)encodeSeed);
            prepareHash(ref reducedHash);
            Array.Copy(reducedHash, 0, hash, 1, reducedHash.Length);
            hash[name.Length + 1] = (byte)encryptLastCharTable[encodeSeed];

            hash[0] = (byte)computeFirstChar(reducedHash);
            return Encoding.ASCII.GetString(hash);
        }

        public static string Encode(byte[] hash)
        {
            return Encode(ByteConverterExt.ToString(hash));
        }

        #endregion
      
        public static int GetID(string value)
        {
            string plainHash = IsEncoded(value) ? value : Encode(value);
            byte[] hash = Encoding.Default.GetBytes(plainHash);

            return GetID(hash);
        }

        public static int GetID(byte[] hash)
        {
            int checksum = 0;

            if (hash == null) return 1;

            for (int i = 0; i < hash.Length; i++) { checksum = checksum * 31 + toLower(hash[i]); }

            if (checksum < 0) { checksum = -checksum; }
            return (checksum & 0x07) + 1;
        }

        public static string GetPath(string hashStr)
        {
            int checksum = 0;
            foreach (char c in hashStr.ToLower())
                checksum += c;

            return (checksum / 100).ToString("D3");
        }

        static int toLower(byte b)
        {
            if (b >= 'A' && b <= 'Z') { return b - ('A' - 'a'); }
            
            return b;
        }
    }
}
