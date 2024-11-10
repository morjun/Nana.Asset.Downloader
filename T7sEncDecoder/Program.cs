using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if !CLI
using System.Windows.Forms;
#endif

namespace T7s_Enc_Decoder
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
#if CLI
            if (args.Length > 0)
            {
                foreach (var pattern in args)
                {
                    string[] files = System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory(), pattern);

                    foreach (var filePath in files)
                    {
                      try {
                        DecryptFiles.DecryptFile(filePath);
                      }
                      catch (System.Security.Cryptography.CryptographicException ex) {
                        Console.WriteLine("암호화 예외가 발생했습니다: " + ex.Message);
                      }
                    }
                }
            }
            else
            {
                Console.WriteLine("Usage: T7s_Enc_Decoder.exe <file1> <file2> ...");
            }
#else
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
#endif
        }
    }
}
