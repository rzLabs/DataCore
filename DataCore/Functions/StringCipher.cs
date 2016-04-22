using System;

namespace DataCore.Functions
{
	public class StringCipher
	{
		private static byte[] s_CipherTable = new byte[]
		{
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			0,
			103,
			32,
			0,
			38,
			119,
			44,
			108,
			78,
			88,
			79,
			0,
			55,
			46,
			37,
			101,
			0,
			56,
			95,
			93,
			35,
			80,
			49,
			45,
			36,
			86,
			91,
			0,
			89,
			0,
			94,
			0,
			0,
			75,
			125,
			106,
			48,
			64,
			71,
			83,
			41,
			65,
			120,
			121,
			54,
			57,
			69,
			70,
			123,
			87,
			98,
			61,
			82,
			118,
			116,
			104,
			50,
			52,
			77,
			40,
			107,
			0,
			109,
			97,
			43,
			126,
			68,
			39,
			67,
			33,
			74,
			73,
			100,
			66,
			85,
			96,
			113,
			102,
			112,
			72,
			81,
			51,
			76,
			110,
			111,
			90,
			105,
			114,
			115,
			117,
			59,
			122,
			99,
			0,
			84,
			53,
			0
		};
		private static byte[] s_CharTable = new byte[]
		{
			94,
			38,
			84,
			95,
			78,
			115,
			100,
			123,
			120,
			111,
			53,
			118,
			96,
			114,
			79,
			89,
			86,
			43,
			44,
			105,
			73,
			85,
			35,
			107,
			67,
			74,
			113,
			56,
			36,
			39,
			126,
			76,
			48,
			80,
			93,
			70,
			101,
			66,
			110,
			45,
			65,
			117,
			40,
			112,
			88,
			72,
			90,
			104,
			119,
			68,
			121,
			50,
			125,
			97,
			103,
			87,
			71,
			55,
			75,
			61,
			98,
			81,
			59,
			83,
			82,
			116,
			41,
			52,
			54,
			108,
			64,
			106,
			69,
			37,
			57,
			33,
			99,
			49,
			91,
			51,
			102,
			109,
			77,
			122,
			0
		};

		public static bool IsEncoded(string hash)
		{
			return StringCipher.Encode(StringCipher.Decode(hash)) == hash;
		}

		public static string Encode(string name)
		{
			char[] array = name.ToLower().ToCharArray();
			int num = 0;
			for (int i = 0; i < array.Length; i++)
			{
				num = (int)(array[i] * '\u0011') + num + 1;
			}
			num = (array.Length + num & 31);
			if (num == 0)
			{
				num = 32;
			}
			char c = (char)StringCipher.s_CharTable[num];
			int num2 = num;
			for (int j = 0; j < array.Length; j++)
			{
				num = (int)array[j];
				for (int k = 0; k < num2; k++)
				{
					num = (int)StringCipher.s_CipherTable[num];
				}
				num2 = (num2 + 1 + (int)(array[j] * '\u0011') & 31);
				if (num2 == 0)
				{
					num2 = 32;
				}
				array[j] = (char)num;
			}
			if (array.Length > 4)
			{
				int num3 = (int)Math.Floor(0.33000001311302191 * (double)array.Length);
				int num4 = (int)Math.Floor(0.6600000262260437 * (double)array.Length);
				char c2 = array[num4];
				char c3 = array[num3];
				array[num4] = array[0];
				array[num3] = array[1];
				array[0] = c2;
				array[1] = c3;
			}
			num = 0;
			for (int l = 0; l < array.Length; l++)
			{
				num += (int)array[l];
			}
			char c4 = (char)StringCipher.s_CharTable[num % 84];
			return c4 + new string(array) + c;
		}

        /// <summary>
        /// Decodes a hash-string into a human-readable string
        /// </summary>
        /// <param name="hash">Hash-string to be decoded</param>
        /// <returns>Human-readable string</returns>
		public static string Decode(string hash)
		{
			if (hash.Length > 0)
			{
				char[] array = hash.Substring(1, hash.Length - 2).ToCharArray();
				if (array.Length > 4)
				{
					int num = (int)Math.Floor(0.33000001311302191 * (double)array.Length);
					int num2 = (int)Math.Floor(0.6600000262260437 * (double)array.Length);
					char c = array[num2];
					char c2 = array[num];
					array[num2] = array[0];
					array[num] = array[1];
					array[0] = c;
					array[1] = c2;
				}
				int num3 = Array.IndexOf<byte>(StringCipher.s_CharTable, (byte)hash[hash.Length - 1], 0, StringCipher.s_CharTable.Length);
				int num4 = num3;
				for (int i = 0; i < array.Length; i++)
				{
					num3 = (int)array[i];
					for (int j = 0; j < num4; j++)
					{
						int num5 = Array.IndexOf<byte>(StringCipher.s_CipherTable, (byte)num3, 0, StringCipher.s_CipherTable.Length);
						if (num5 < StringCipher.s_CipherTable.Length)
						{
							num3 = num5;
						}
						else
						{
							num3 = 255;
						}
					}
					array[i] = (char)num3;
					num4 = (1 + num4 + 17 * num3 & 31);
					if (num4 == 0)
					{
						num4 = 32;
					}
				}
				return new string(array);
			}
			return "";
		}
	}
}
