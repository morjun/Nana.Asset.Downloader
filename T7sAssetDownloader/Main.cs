using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Windows.Forms;
using T7s_Asset_Downloader.Asset;
using T7s_Asset_Downloader.Crypt;
using T7s_Asset_Downloader.Response;

namespace T7s_Asset_Downloader
{
    public partial class Main : Form
    {
        private static readonly CancellationTokenSource CancelSource = new CancellationTokenSource();
        private readonly List<string> _downloadDoneList = new List<string>();

        private readonly ProgressMessageHandler _downloadProcessMessageHandler =
            new ProgressMessageHandler(new HttpClientHandler());

        private readonly ProgressMessageHandler _postProcessMessageHandler =
            new ProgressMessageHandler(new HttpClientHandler());

        private readonly MakeRequest _request = new MakeRequest();
        private readonly Task _setNewVersion = Define.SetNewVersion();
        private string[] _listResult;
        public bool IsSeveralFiles = true;

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            _setNewVersion.Start();
            _request.HttpClientTest(_downloadProcessMessageHandler);

            if (File.Exists(Define.GetIndexPath()))
            {
                Define.JsonParse.LoadUrlIndex(Define.GetIndexPath());
                _ini_listResult();
                button_LoadAllResult.Enabled = true;
                if (File.Exists(Define.GetConfingPath()))
                {
                    Define.JsonParse.LoadConfing(Define.GetConfingPath());
                    Define._ini_Coning();
                }
                else
                {
                    Define.NOW_STAUTUS = NOW_STAUTUS.NoneConing;
                    Define.NOW_STAUTUS = NOW_STAUTUS.First;
                }
            }
            else
            {
                button_ReloadAdvance.Enabled = false;

                Define.NOW_STAUTUS = NOW_STAUTUS.NoneIndex;
                Define.NOW_STAUTUS = NOW_STAUTUS.First;
            }

            TestNew();
            ReloadNoticeLabels();
        }

        /// <summary>
        ///     Initialize default the list will show into the listbox.
        /// </summary>
        public void _ini_listResult()
        {
            _listResult = Define.GetDefaultNameList();
            ShowlistResult(Define.GetDefaultNameList(), Define.DefaultShowCount);
        }

        private void ReloadNoticeLabels()
        {
            SetNoticesText(
                Define.NOW_STAUTUS != NOW_STAUTUS.First ? "현재 버전: " + "r" + Define.NowRev : "현재 버전 : " + ">> 최신 버전을 받으십시오.",
                label_NowRev);
        }

        private void ReloadProcess(int TotalCount)
        {
            Task.Run(() =>
            {
                var NowProcess = _downloadDoneList.Count / TotalCount * 100;
                SetProgressInt(NowProcess);
            }).Wait();
        }

        private async void TestNew()
        {
            try
            {
                SetNoticesText(">> ... 게임의 최신 버전 정보를 조회하는 중", downloadNotice);
                await _setNewVersion;

                _request._ini_PostClient();
                if (Define.NOW_STAUTUS == NOW_STAUTUS.First)
                    if (MessageBox.Show(@"전체 인덱스 파일을 한 번 다운로드 해야 합니다. 다운로드 시간이 오래 걸릴 수 있습니다. 지금 다운로드하시겠습니까?"
                            , @"Notice"
                            , MessageBoxButtons.OKCancel) != DialogResult.OK)
                    {
                        SetNoticesText(">>전체 인덱스 파일을 한 번 얻으려면, 최신 버전 얻기를 클릭하십시오.", downloadNotice);
                        return;
                    }

                SetNoticesText(">> ...최신 버전의 데이터를 자동으로 검사하고 있습니다. 기다려 주십시오", downloadNotice);

                var updateStatus = await Task.Run(async () =>
                {
                    if (await _request.MakeLoginIndexPostRequest(Define.Id, Define.GetApiName(Define.APINAME_TYPE.login_index)) == 0)
                        SetNoticesText(">> ...로그인 성공", downloadNotice);
                        // return UPDATE_STATUS.NoNecessary;
                    if (await _request.MakeUpdatePostRequest(Define.Id, Define.GetApiName(Define.APINAME_TYPE.result)) == 0)
                        SetNoticesText(">> ...리소스 업데이트 성공", downloadNotice);
                        return await Define.JsonParse.TestUpdateStatusAsync();
                    return UPDATE_STATUS.Error;
                });

                switch (updateStatus)
                {
                    case UPDATE_STATUS.Error:
                        SetNoticesText(">> Error : 게임 서버가 점검 중일 수 있습니다. 잠시 후에 다시 시도하십시오.", downloadNotice);
                        return;
                    case UPDATE_STATUS.NoNecessary:
                        SetNoticesText(">> 이미 최신 버전입니다 ", downloadNotice);
                        return;
                    case UPDATE_STATUS.Ok:
                        if (MessageBox.Show(@" 최신 버전이 감지되었습니다. 지금 업데이트하시겠습니까?"
                                , @"Notice"
                                , MessageBoxButtons.OKCancel) == DialogResult.OK)
                        {
                            Define.JsonParse.UpdateUrlIndex(Define.JsonParse, true);
                            await Task.Run(() =>
                            {
                                while (Define.IsGetNewComplete == false)
                                {
                                }

                                Define.NOW_STAUTUS = NOW_STAUTUS.Normal;
                                SetNoticesText(">> 준비...", downloadNotice);
                                ReloadNoticeLabels();
                                SetButtomEnabled(true, button_GetNew);
                                SetButtomEnabled(true, button_ReloadAdvance);
                                _ini_listResult();
                            });
                        }
                        else
                        {
                            SetNoticesText(">>준비되었습니다. 업데이트할 수 있는 새로운 버전이 있습니다 ", downloadNotice);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($@"네트워크를 검사하십시오: {e.Message}");
            }


        }


        /// <summary>
        ///     Main control download method.
        /// </summary>
        /// <param name="downloadType">下载类型:少量下载或大量下载</param>
        private async void StartDownload(DOWNLOAD_TYPE downloadType)
        {
            if (!Directory.Exists(Define.GetFileSavePath())) Directory.CreateDirectory(Define.GetFileSavePath());
            //Initialize the UI about download
            SetProgressInt(0);
            button_DownloadCancel.Visible = true;

            string[] willDownloadList;
            int totalCount;
            var nowFileIndex = 0;

            switch (downloadType)
            {
                case DOWNLOAD_TYPE.AllFiles:
                    totalCount = _listResult.Length;
                    willDownloadList = _listResult;
                    break;
                case DOWNLOAD_TYPE.SeletFiles:
                    totalCount = listBoxResult.SelectedItems.Count;
                    if (totalCount < 1)
                    {
                        MessageBox.Show(@"다운로드할 파일을 선택하지 않았습니다. 파일을 하나 이상 선택한 후 다운로드를 시작하려면 클릭하십시오.", "Notice");
                        button_DownloadCancel.Visible = false;
                        return;
                    }

                    var checkedNameList = new string[totalCount];
                    for (var i = 0; i < totalCount; i++) checkedNameList[i] = listBoxResult.SelectedItems[i].ToString();
                    willDownloadList = checkedNameList;
                    break;
                default:
                    totalCount = _listResult.Length;
                    willDownloadList = _listResult;
                    break;
            }

            //Start Downlaod
            try
            {
                var cancelToken = CancelSource.Token;
                var scheduler = new LimitedConcurrencyLevelTaskScheduler(Define.MaxDownloadTasks);
                var downloadTaskFactory = new TaskFactory(scheduler);
                if (totalCount <= 15)
                {
                    IsSeveralFiles = true;
                    foreach (var fileName in willDownloadList)
                        await downloadTaskFactory.StartNew(async nowFileName =>
                        {
                            nowFileIndex++;
                            await DownloadFiles(fileName, nowFileIndex, totalCount, AUTO_DECRYPT.Auto);
                        }, fileName);
                }
                else
                {
                    IsSeveralFiles = false;
                    Define.DownloadTaskSleep = totalCount < 200 ? 100 : totalCount > 1000 ? 500 : totalCount / 3;

                    foreach (var fileName in willDownloadList)
                        await downloadTaskFactory.StartNew(async nowFileName =>
                        {
                            nowFileIndex++;
                            Thread.Sleep(nowFileIndex % 25 == 0
                                ? 500
                                : Define.DownloadTaskSleep);
                            await DownloadFiles(nowFileName.ToString(), nowFileIndex, totalCount, AUTO_DECRYPT.Auto);
                        }, fileName, cancelToken);
                }

                cancelToken.ThrowIfCancellationRequested();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void OnDownloadDone(int totalCount)
        {
            if (_downloadDoneList.Count != totalCount) return;
            GC.Collect();
            Thread.Sleep(200);
            SetNoticesText("下载完成 >> 共 " + totalCount + " 个文件 ! !", downloadNotice);
            SetButtomVisibled(false, button_DownloadCancel);
        }


        /// <summary>
        ///     Main Download method
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileNameIndex"></param>
        /// <param name="totalCount"></param>
        /// <param name="AUTO_DECRYPT"></param>
        /// <returns></returns>
        private async Task DownloadFiles(string fileName, int fileNameIndex, int totalCount, AUTO_DECRYPT AUTO_DECRYPT)
        {
            SetNoticesText("正在下载 ... " + _downloadDoneList.Count + " / " + totalCount, downloadNotice);
            if (IsSeveralFiles)
                _downloadProcessMessageHandler.HttpReceiveProgress += (senders, es) =>
                {
                    var num = es.ProgressPercentage;
                    SetProgressInt(num);
                };
            _downloadDoneList.Add(await _request.MakeGetRequest(Define.GetUrl(fileName), Define.GetFileSavePath(),
                fileName));
            SetNoticesText("正在下载 ... " + _downloadDoneList.Count + " / " + totalCount, downloadNotice);

            if (!IsSeveralFiles) ReloadProcess(totalCount);

            if (AUTO_DECRYPT == AUTO_DECRYPT.Auto)
                if (Save.GetFileType(fileName) != ENC_TYPE.ERROR)
                    DecryptFiles.DecryptFile(Define.GetFileSavePath() + fileName,
                        Crypt.Crypt.IdentifyEncVersion(fileName));

            OnDownloadDone(totalCount);
        }

        /// <summary>
        ///     Main post method
        /// </summary>
        /// <param name="index"></param>
        /// <param name="update"></param>
        private async Task<UPDATE_STATUS> StartPost(bool index = false, bool update = false)
        {
            SetNoticesText("새 버전의 데이터를 가져오는 중... 잠시만...", downloadNotice);
            var jsonParse = new JsonParse();
            _postProcessMessageHandler.HttpSendProgress += (senders, es) =>
            {
                var num = es.ProgressPercentage;
                SetProgressInt(num);
            };
            if (!index)
                return await jsonParse.SaveDlConfing(
                    _request.MakePostRequest(Define.Id, Define.GetApiName(Define.APINAME_TYPE.result), true), true);

            if (update)
            {
                if (await _request.MakeUpdatePostRequest(Define.Id, Define.GetApiName(Define.APINAME_TYPE.result)) == 0)
                    Define.JsonParse.UpdateUrlIndex(Define.JsonParse, true);
            }
            else
            {
                jsonParse.SaveUrlIndex(
                    _request.MakePostRequest(Define.Id, Define.GetApiName(Define.APINAME_TYPE.result), true), true);
            }

            return UPDATE_STATUS.Ok;
        }

        /// <summary>
        ///     Show the list into listbox.
        /// </summary>
        /// <param name="nameList"></param>
        /// <param name="showCount"></param>
        private void ShowlistResult(string[] nameList, int showCount)
        {
            Task.Run(() =>
            {
                for (var i = 0; i < showCount; i++) SetlistResult(nameList[i]);
            });
        }

        /// <summary>
        ///     Show the list into listbox.
        /// </summary>
        /// <param name="nameList"></param>
        /// <param name="startIndex"></param>
        /// <param name="showCount"></param>
        private void ShowlistResult(string[] nameList, int startIndex, int showCount)
        {
            Task.Run(() =>
            {
                for (var i = startIndex; i < showCount; i++) SetlistResult(nameList[i]);
            });
        }

        private void Button_DownloadCancel_Click(object sender, EventArgs e)
        {
            CancelSource.Cancel();
        }

        private delegate void SetNotices(string notices, Label label);

        private delegate void SetProgress(int progress);

        private delegate void SetCallBack(object obj);

        private delegate void SetEnable(bool enabled, Button button);

        private delegate void SetVisible(bool visible, Button button);

        #region UI逻辑

        private void Button_DownloadAllFiles_Click(object sender, EventArgs e)
        {
            _downloadDoneList.Clear();
            if (_listResult.Length > 50)
                if (MessageBox.Show($@"请注意，所选文件量为{_listResult.Length}个" + @"下载可能会花费较长时间。", @"Notices",
                        MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2)
                    == DialogResult.Cancel)
                    return;
            ;
            StartDownload(DOWNLOAD_TYPE.AllFiles);
        }

        private void Button_DownloadCheckFiles_Click(object sender, EventArgs e)
        {
            _downloadDoneList.Clear();
            StartDownload(DOWNLOAD_TYPE.SeletFiles);
        }

        private void Button_OpenDownloadPath_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(Define.GetFileSavePath())) Directory.CreateDirectory(Define.GetFileSavePath());
            Process.Start("Explorer.exe", Define.GetFileSavePath());
        }

        private void Button_LoadAllResult_Click(object sender, EventArgs e)
        {
            if (_listResult == null) _listResult = Define.GetDefaultNameList();
            ShowlistResult(_listResult, Define.DefaultShowCount, _listResult.Length);
            button_LoadAllResult.Enabled = false;
        }

        private async void TextBox_SearchFiles_TextChanged(object sender, EventArgs e)
        {
            await Task.Delay(1);
            listBoxResult.Items.Clear();
            _listResult = Define.GetListResult(textBox_SeachFiles.Text);
            ShowlistResult(_listResult,
                !(_listResult.Length > Define.DefaultShowCount) ? _listResult.Length : Define.DefaultShowCount);
            button_LoadAllResult.Enabled = true;
        }

        private void Button_ShowAdvance_Click(object sender, EventArgs e)
        {
            var advance = new Advance();
            advance.Show();
        }

        private async void Button_GetNew_Click(object sender, EventArgs e)
        {
            var updateStatus = UPDATE_STATUS.Ok;

            Define.IsGetNewComplete = false;
            button_ReloadAdvance.Enabled = false;
            button_GetNew.Enabled = false;

            if (Define.NOW_STAUTUS == NOW_STAUTUS.First)
            {
                await Task.Run(async () =>
                {
                    updateStatus = await StartPost();
                });

                switch (updateStatus)
                {
                    case UPDATE_STATUS.Error:
                        SetNoticesText(">> Error: 게임 서버가 점검 중일 수 있습니다. 잠시 후에 다시 시도하십시오.", downloadNotice);
                        SetButtomEnabled(true, button_GetNew);
                        SetButtomEnabled(true, button_ReloadAdvance);
                        return;
                    case UPDATE_STATUS.Ok:
                        break;
                    case UPDATE_STATUS.NoNecessary:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                await Task.Run(async () =>
                {
                    // Define.masterRev = "252";
                    // Define.Rev = "753";
                    updateStatus = await StartPost(true);
                });
            }
            else
            {
                await Task.Run(async () =>
                {
                    Define.LastRev = Define.NowRev;
                    updateStatus = await StartPost();
                });

                switch (updateStatus)
                {
                    case UPDATE_STATUS.Error:
                        SetNoticesText(">> Error: 게임 서버가 점검 중일 수 있습니다. 잠시 후에 다시 시도하십시오.", downloadNotice);
                        SetButtomEnabled(true, button_GetNew);
                        SetButtomEnabled(true, button_ReloadAdvance);
                        return;
                    case UPDATE_STATUS.NoNecessary:
                        SetNoticesText("이미 최신 버전입니다", downloadNotice);
                        SetButtomEnabled(true, button_GetNew);
                        SetButtomEnabled(true, button_ReloadAdvance);
                        return;
                    case UPDATE_STATUS.Ok:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                await Task.Run(async () =>
                {
                    updateStatus = await StartPost(true, true);
                });
            }

            await Task.Run(() =>
            {
                while (Define.IsGetNewComplete == false)
                {
                }

                Define.NOW_STAUTUS = NOW_STAUTUS.Normal;
                SetNoticesText(">> 준비...", downloadNotice);
                ReloadNoticeLabels();
                SetButtomEnabled(true, button_GetNew);
                SetButtomEnabled(true, button_ReloadAdvance);
                _ini_listResult();
            });
        }

        private void Button_ReloadAdvance(object sender, EventArgs e)
        {
            listBoxResult.Items.Clear();
            Define._ini_Coning();
            ReloadNoticeLabels();
            _ini_listResult();
        }

        private void Button_About_Click(object sender, EventArgs e)
        {
        }

        private void Button_GetDiffList_Click(object sender, EventArgs e)
        {
            string[] namesList1 = null, namesList2 = null;

            var jsonParse = new JsonParse();

            var ofd = new OpenFileDialog
            {
                Title = "파일 선택",
                Filter = "인덱스 파일 암호화|Index.json",
                RestoreDirectory = true
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                jsonParse.LoadUrlIndex(ofd.FileName);
                namesList1 = jsonParse.FileUrls.Select(t => t.Name).ToArray();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    jsonParse.LoadUrlIndex(ofd.FileName);
                    namesList2 = jsonParse.FileUrls.Select(t => t.Name).ToArray();
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }

            Define.DiifList = namesList1.Except(namesList2).ToArray();
            _listResult = Define.DiifList;
            listBoxResult.Items.Clear();
            ShowlistResult(Define.DiifList, Define.DiifList.Length);
        }

        #endregion

        #region 委托修改UI

        public void AddListResult(object item)
        {
            listBoxResult.Items.Add(item);
        }

        private void SetNoticesText(string notice, Label label)
        {
            if (label.InvokeRequired)
            {
                SetNotices call = SetNoticesText;
                Invoke(call, notice, label);
            }
            else
            {
                label.Text = notice;
            }
        }

        private void SetButtomEnabled(bool enabled, Button button)
        {
            if (button.InvokeRequired)
            {
                var call = new SetEnable(SetButtomEnabled);
                Invoke(call, enabled, button);
            }
            else
            {
                button.Enabled = enabled;
            }
        }

        private void SetButtomVisibled(bool visibled, Button button)
        {
            if (button.InvokeRequired)
            {
                var call = new SetVisible(SetButtomVisibled);
                Invoke(call, visibled, button);
            }
            else
            {
                button.Visible = visibled;
            }
        }

        private void SetProgressInt(int progress)
        {
            if (downloadProgressBar.InvokeRequired)
            {
                var call = new SetProgress(SetProgressInt);
                Invoke(call, progress);
            }
            else
            {
                downloadProgressBar.Value = progress;
            }
        }

        private void SetlistResult(object item)
        {
            if (listBoxResult.InvokeRequired)
            {
                var call = new SetCallBack(SetlistResult);
                Invoke(call, item);
            }
            else
            {
                AddListResult(item);
            }
        }

        private void label_NowRev_Click(object sender, EventArgs e)
        {

        }

        #endregion


        //test

        //private async void DownloadFiles(string fileName, int fileNameIndex, int totalCount, TaskScheduler scheduler, AUTO_DECRYPT AUTO_DECRYPT)
        //{
        //    Action<object> download = async (NowfileName) =>
        //    {
        //        string TempNowfileName = NowfileName.ToString();
        //        SetNoticesText("正在下载 ... " + TempNowfileName + fileNameIndex + "/" + totalCount, downloadNotice);
        //        processMessageHander.HttpReceiveProgress += (senders, es) =>
        //        {
        //            int num = es.ProgressPercentage;
        //            SetProgressInt(num);
        //        };
        //        await new MakeRequest().MkaeGetRequest(Define.GetUrl(TempNowfileName), Define.GetFileSavePath(), TempNowfileName);
        //        if (AUTO_DECRYPT == AUTO_DECRYPT.Auto)
        //        {
        //            if (Save.GetFileType(TempNowfileName) != ENC_TYPE.ERROR)
        //            {
        //                DecryptFiles.DecryptFile(Define.GetFileSavePath() + TempNowfileName);
        //            }
        //        }
        //        DownloadDomeList.Add(NowfileName.ToString());
        //    };
        //    await Task.Factory.StartNew(download, fileName, CancellationToken.None, TaskCreationOptions.None, scheduler).ContinueWith((t, obj) =>
        //    {
        //        if (t.Status != TaskStatus.RanToCompletion)
        //        {
        //            DownloadFiles(fileName, fileNameIndex, totalCount, scheduler, Define.AUTO_DECRYPT);
        //        }
        //    }, fileName);
        //}
    }
}