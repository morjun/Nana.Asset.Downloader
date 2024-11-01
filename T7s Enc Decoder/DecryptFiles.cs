﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace T7s_Enc_Decoder
{
    public static class DecryptFiles
    {
        public static void DecryptFile(string filePath)
        {
            byte[] fileBytes;

            switch (Save.GetFileType(filePath))
            {
                case ENC_TYPE.JPGorPNG :
                    using (var fileStream = File.OpenWrite(Save.GetSavePath(filePath)))
                    {
                        fileBytes = Crypt.Decrypt<Byte[]>(System.IO.File.ReadAllBytes(filePath));
                        fileStream.Write(fileBytes, 0, fileBytes.Length);
                        fileStream.Close();
                    }
                    break;
                case ENC_TYPE.TXTorSQLorJSON :
                    using (var streamWriter = new StreamWriter(Save.GetSavePath(filePath)))
                    {
                        fileBytes = Crypt.Decrypt<Byte[]>(System.IO.File.ReadAllBytes(filePath), true);
                        var fileText = Encoding.UTF8.GetString(fileBytes);
                        streamWriter.Write(fileText);
                        streamWriter.Close();
                    }
                    break;
                case ENC_TYPE.BIN :
                    using (var streamWriter = new StreamWriter(Save.GetSavePath(filePath)))
                    {
                        fileBytes = Crypt.Decrypt<Byte[]>(System.IO.File.ReadAllBytes(filePath));
                        var fileText = Encoding.UTF8.GetString(fileBytes);
                        streamWriter.Write(fileText);
                        streamWriter.Close();
                    }
                    break;
                case ENC_TYPE.ERROR:
                    using (var streamWriter = new StreamWriter(Save.GetSavePath(filePath)))
                    {
                        fileBytes = Crypt.Decrypt<Byte[]>(System.IO.File.ReadAllBytes(filePath),true);
                        var fileText = Encoding.UTF8.GetString(fileBytes);
                        streamWriter.Write(fileText);
                        streamWriter.Close();
                    }
#if !CLI
                    System.Windows.Forms.MessageBox.Show(@"无法识别");
#endif
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static void DecryptForDownloader(string path)
        {
            var fileBytes = Crypt.Decrypt<byte[]>(System.IO.File.ReadAllBytes(path), true);
            using (StreamWriter streamWriter = new StreamWriter(Save.GetSavePath(path)))
            {
                string fileText = Encoding.UTF8.GetString(fileBytes);
                streamWriter.Write(fileText);
                streamWriter.Close();
            }
        }


        public static void EncryptFile(string FilePath)
        {
            byte[] FileBytes;
            using (FileStream fileStream = File.OpenWrite(Save.GetSavePath(FilePath) ))
            {
                FileBytes = Crypt.Encrypt<byte[]>(System.IO.File.ReadAllBytes(FilePath),true,true);
                fileStream.Write(FileBytes, 0, FileBytes.Length);
                fileStream.Close();
            }

            switch (Save.GetFileType(FilePath))
            {
                case ENC_TYPE.JPGorPNG:
                    using (FileStream fileStream = File.OpenWrite(Save.GetSavePath(FilePath)))
                    {
                        FileBytes = Crypt.Encrypt<byte[]>(System.IO.File.ReadAllBytes(FilePath));
                        fileStream.Write(FileBytes, 0, FileBytes.Length);
                        fileStream.Close();
                    }
                    break;
                case ENC_TYPE.TXTorSQLorJSON:
                    using (StreamWriter streamWriter = new StreamWriter(Save.GetSavePath(FilePath)))
                    {
                        FileBytes = Crypt.Encrypt<byte[]>(System.IO.File.ReadAllBytes(FilePath), true);
                        string FileText = Encoding.UTF8.GetString(FileBytes);
                        streamWriter.Write(FileText);
                        streamWriter.Close();
                    }
                    break;
                case ENC_TYPE.BIN:
                    using (StreamWriter streamWriter = new StreamWriter(Save.GetSavePath(FilePath)))
                    {
                        FileBytes = Crypt.Encrypt<byte[]>(System.IO.File.ReadAllBytes(FilePath));
                        string FileText = Encoding.UTF8.GetString(FileBytes);
                        streamWriter.Write(FileText);
                        streamWriter.Close();
                    }
                    break;
                case ENC_TYPE.ERROR:
#if !CLI
                    System.Windows.Forms.MessageBox.Show(@"无法识别");
#endif
                    break;

            }
        }
    }

    public static class Save
    {
        public static string GetSavePath(string FilePath)
        {
            string currentPath = Path.GetDirectoryName(FilePath);
            if (currentPath == "")
            {
                currentPath = ".";
            }
            if (!Directory.Exists(currentPath + "/Deconde Files"))
            {
                Directory.CreateDirectory(currentPath + "/Deconde Files");
            }
            string SavePath = currentPath + "/Deconde Files/" + Path.GetFileNameWithoutExtension(FilePath);
            return SavePath;
        }

        public static ENC_TYPE GetFileType(string FilePath)
        {
            string[] FileType = FilePath.Split('.');
            string Type = FileType[FileType.Length - 2];
            if (Equals(Type, "txt") | Equals(Type, "sql" )| Equals(Type, "json"))
            {
                return ENC_TYPE.TXTorSQLorJSON;
            }
            else if (Equals(Type, "png") | Equals(Type, "jpg"))
            {
                return ENC_TYPE.JPGorPNG;
            }
            else if (Equals(Type, "bin" ))
            {
                return ENC_TYPE.BIN;
            }
            else
            {
                return ENC_TYPE.ERROR;
            }

        }


    }

    /// <summary>
    /// 定义加密文件类型
    /// </summary>
    public enum ENC_TYPE
    {
        /// <summary>
        /// 文本或数据文件
        /// </summary>
        TXTorSQLorJSON,
        /// <summary>
        /// 图片文件（JPG or PNG）
        /// </summary>
        JPGorPNG,
        /// <summary>
        /// 其他文件（BIN）
        /// </summary>
        BIN,
        /// <summary>
        /// 无法识别
        /// </summary>
        ERROR,
    }
}
