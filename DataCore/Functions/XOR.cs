using System.Collections.Generic;

namespace DataCore.Functions
{
    /// <summary>
    /// Original XOR class provided by Glandu2 and adapted originally by xXExiledXx for C# 
    /// adapted and restructed for Portal by iSmokeDrow.
    /// 
    /// </summary>
	public class XOR
	{
        public static bool UseModifiedKey = false;

        public static void SetKey(byte[] key) => s_CipherTable = key;

        /// <summary>
        /// Legend of non-encrypted file extensions (files that will not be encoded during patching)
        /// </summary>
        public static List<string> UnencryptedExtensions { get; set; } = new List<string> { "mp3", "ogg", "raw", "dds", "tga", "naf", "nx3", "cob", "nfm" };

        /// <summary>
        /// Table of bytes to use for encoding files during patching
        /// </summary>
		static byte[] s_CipherTable = new byte[]
		{
            0x77, 0xe8, 0x5e, 0xec, 0xb7, 0x4e, 0xc1, 0x87, 0x4f, 0xe6, 0xf5, 0x3c, 0x1f, 0xb3, 0x15, 0x43,
            0x6a, 0x49, 0x30, 0xa6, 0xbf, 0x53, 0xa8, 0x35, 0x5b, 0xe5, 0x9e, 0x0e, 0x41, 0xec, 0x22, 0xb8,
            0xd4, 0x80, 0xa4, 0x8c, 0xce, 0x65, 0x13, 0x1d, 0x4b, 0x08, 0x5a, 0x6a, 0xbb, 0x6f, 0xad, 0x25,
            0xb8, 0xdd, 0xcc, 0x77, 0x30, 0x74, 0xac, 0x8c, 0x5a, 0x4a, 0x9a, 0x9b, 0x36, 0xbc, 0x53, 0x0a,
            0x3c, 0xf8, 0x96, 0x0b, 0x5d, 0xaa, 0x28, 0xa9, 0xb2, 0x82, 0x13, 0x6e, 0xf1, 0xc1, 0x93, 0xa9,
            0x9e, 0x5f, 0x20, 0xcf, 0xd4, 0xcc, 0x5b, 0x2e, 0x16, 0xf5, 0xc9, 0x4c, 0xb2, 0x1c, 0x57, 0xee,
            0x14, 0xed, 0xf9, 0x72, 0x97, 0x22, 0x1b, 0x4a, 0xa4, 0x2e, 0xb8, 0x96, 0xef, 0x4b, 0x3f, 0x8e,
            0xab, 0x60, 0x5d, 0x7f, 0x2c, 0xb8, 0xad, 0x43, 0xad, 0x76, 0x8f, 0x5f, 0x92, 0xe6, 0x4e, 0xa7,
            0xd4, 0x47, 0x19, 0x6b, 0x69, 0x34, 0xb5, 0x0e, 0x62, 0x6d, 0xa4, 0x52, 0xb9, 0xe3, 0xe0, 0x64,
            0x43, 0x3d, 0xe3, 0x70, 0xf5, 0x90, 0xb3, 0xa2, 0x06, 0x42, 0x02, 0x98, 0x29, 0x50, 0x3f, 0xfd,
            0x97, 0x58, 0x68, 0x01, 0x8c, 0x1e, 0x0f, 0xef, 0x8b, 0xb3, 0x41, 0x44, 0x96, 0x21, 0xa8, 0xda,
            0x5e, 0x8b, 0x4a, 0x53, 0x1b, 0xfd, 0xf5, 0x21, 0x3f, 0xf7, 0xba, 0x68, 0x47, 0xf9, 0x65, 0xdf,
            0x52, 0xce, 0xe0, 0xde, 0xec, 0xef, 0xcd, 0x77, 0xa2, 0x0e, 0xbc, 0x38, 0x2f, 0x64, 0x12, 0x8d,
            0xf0, 0x5c, 0xe0, 0x0b, 0x59, 0xd6, 0x2d, 0x99, 0xcd, 0xe7, 0x01, 0x15, 0xe0, 0x67, 0xf4, 0x32,
            0x35, 0xd4, 0x11, 0x21, 0xc3, 0xde, 0x98, 0x65, 0xed, 0x54, 0x9d, 0x1c, 0xb9, 0xb0, 0xaa, 0xa9,
            0x0c, 0x8a, 0xb4, 0x66, 0x60, 0xe1, 0xff, 0x2e, 0xc8, 0x00, 0x43, 0xa9, 0x67, 0x37, 0xdb, 0x9c
        };

        /// <summary>
        /// Performs an crypto-ciper on given buffer
        /// </summary>
        /// <param name="buffer">Byte collection to be encrypted</param>
        /// <param name="index">Index to perform encryption on buffer</param>
        public static void Cipher(ref byte[] buffer, ref byte index)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] ^= s_CipherTable[index];
                index++;
            }
        }

        /// <summary>
        /// Determines if a specific file extension is to be encrypted or not
        /// </summary>
        /// <param name="ext">File extension (.dds etc) to be checked</param>
        /// <returns>true/false</returns>
		public static bool Encrypted(string ext)
		{
            return !UnencryptedExtensions.Contains(ext);
		}
	}
}
