using System;
using System.Windows.Forms;
using HtmlAgilityPack;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace T7s_Asset_Downloader.Asset
{
    internal static class GetVersion
    {
        private const string Url = "https://play.google.com/store/apps/details?id=jp.ne.donuts.t7s&hl=ja";

        private static HtmlDocument _htmlDocument;
        private static string SelectHtmlToString(string selectString, string param = " ", bool attributes = false)
        {

            var NewVersionDivSelectString = "div.sMUprd:nth-child(1) > div:nth-child(2)";

            if (attributes)
                return _htmlDocument.DocumentNode.SelectSingleNode(
                    NewVersionDivSelectString +
                    selectString).Attributes[param].Value;

            return _htmlDocument.DocumentNode.SelectSingleNode(
                    NewVersionDivSelectString +
                    selectString).InnerText
                .Replace("\t", "")
                .Replace(" ", "")
                .Replace("\r", "")
                .Replace("\n", "")
                .Replace(param, "");
        }

        public static GameVersion GetNewVersion()
        {
            try
            {
                var web = new HtmlWeb();
                _htmlDocument = web.Load(Url);
                var tempVersion = SelectHtmlToString("");
                return new GameVersion
                {
                    Version = tempVersion,
                    VersionCode = SelectHtmlToString("", tempVersion).Substring(1),
                    DownloadPath = Url + SelectHtmlToString("", "href", true)
                };
            }
            catch (Exception e)
            {
                MessageBox.Show($@"게임 버전 검사 실패! : {e.Message}");
                return new GameVersion
                {
                    Version = Define.Ver,
                    VersionCode = Define.Blt,
                };
            }
        }

        public class GameVersion
        {
            public string Version { get; set; }
            public string VersionCode { get; set; }
            public string DownloadPath { get; set; }
        }
    }
}