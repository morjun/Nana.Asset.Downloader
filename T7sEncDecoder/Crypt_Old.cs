﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !CLI
using System.Windows.Forms;
#endif
using System.Threading.Tasks;
using K4os.Compression.LZ4;
using System.Security.Cryptography;

namespace T7s_Enc_Decoder
{
    class Crypt_Old
    {
        private static readonly byte[] privateKey = ConvertByte("3HGWxMZz6f2egL84");

        public static byte[] ConvertByte(object data)
        {
            Type type = data.GetType();
            if (type.Equals(typeof(int)))
            {
                return BitConverter.GetBytes((int)data);
            }
            if (type.Equals(typeof(float)))
            {
                return BitConverter.GetBytes((float)data);
            }
            if (type.Equals(typeof(string)))
            {
                return Encoding.UTF8.GetBytes(data.ToString());
            }
            if (type.Equals(typeof(byte[])))
            {
                return (byte[])data;
            }
            return null;
        }

        public static byte[] Decrypt<T>(T data, bool lz4 = false)
        {
            if (data == null)
            {
                return null;
            }
            byte[] array = new byte[16];
            byte[] array2 = ConvertByte(data);
            if (array2.Length <= 16)
            {
                return null;
            }
            byte[] array3 = new byte[array2.Length - 16];
            Buffer.BlockCopy(array2, 0, array, 0, 16);
            Buffer.BlockCopy(array2, 16, array3, 0, array2.Length - 16);
            byte[] array4;
            using (RijndaelManaged rijndaelManaged = new RijndaelManaged())
            {
                rijndaelManaged.BlockSize = 128;
                rijndaelManaged.KeySize = 128;
                rijndaelManaged.IV = array;
                rijndaelManaged.Key = privateKey;
                rijndaelManaged.Mode = CipherMode.CBC;
                rijndaelManaged.Padding = PaddingMode.PKCS7;
                using (ICryptoTransform cryptoTransform = rijndaelManaged.CreateDecryptor())
                {
                    array4 = cryptoTransform.TransformFinalBlock(array3, 0, array3.Length);
                }
            }
            var array5 = new byte[array4.Length];
            LZ4Codec.Decode(array4, 4, array4.Length - 4, array5, 0, BitConverter.ToInt32(array4, 0));
            return (!lz4) ? array4 : array5;
        }

        public static byte[] Encrypt<T>(T data, bool lz4 = false)
        {
            if (data == null)
            {
                return null;
            }
            byte[] array = ConvertByte(data);
            if (lz4)
            {
                var array2 = new byte[array.Length];
                LZ4Codec.Encode(array, 0, array.Length, array2, 0, array2.Length);
                if (array2.Length > 0)
                {
                    int value = array.Length;
                    array = new byte[array2.Length + 4];
                    Buffer.BlockCopy(BitConverter.GetBytes(value), 0, array, 0, 4);
                    Buffer.BlockCopy(array2, 0, array, 4, array2.Length);
                }
#if !CLI
                else
                {
                    MessageBox.Show("圧縮失敗！: " + data);
                }
#endif
            }
            using (RijndaelManaged rijndaelManaged = new RijndaelManaged())
            {
                rijndaelManaged.BlockSize = 128;
                rijndaelManaged.KeySize = 128;
                rijndaelManaged.Key = privateKey;
                rijndaelManaged.Mode = CipherMode.CBC;
                rijndaelManaged.Padding = PaddingMode.PKCS7;
                rijndaelManaged.GenerateIV();
                byte[] iV = rijndaelManaged.IV;
                using (ICryptoTransform cryptoTransform = rijndaelManaged.CreateEncryptor())
                {
                    byte[] array2 = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
                    byte[] array3 = new byte[iV.Length + array2.Length];
                    Buffer.BlockCopy(iV, 0, array3, 0, 16);
                    Buffer.BlockCopy(array2, 0, array3, 16, array2.Length);
                    return array3;
                }
            }
        }
    }
}
