using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AiSoft.Tools.Extensions;
using AiSoft.Tools.Files;
using AiSoft.Tools.Helpers;
using AiSoft.Update.Enums;
using AiSoft.Update.Models;

namespace AiSoft.Update
{
    /// <summary>
    /// 升级类
    /// </summary>
    public class AiUpdate
    {
        /// <summary>
        /// 是否只是比较版本(默认:否)
        /// </summary>
        public bool IsOnlyCompare { get; set; }

        /// <summary>
        /// 当前版本
        /// </summary>
        public string NowVersion { get; set; }

        /// <summary>
        /// 升级配置地址
        /// </summary>
        public string UpdateJsonAddress { get; set; }

        /// <summary>
        /// 下载文件保存路径(默认:本地Update目录)
        /// </summary>
        public string DownFileSavePath { get; set; }

        /// <summary>
        /// 下载文件需要复制路径(默认:本地目录。为空不复制)
        /// </summary>
        public string DownFileCopyToPath { get; set; }

        ///// <summary>
        ///// 是否删除下载文件(默认:否)
        ///// </summary>
        //public bool IsDeleteDownFile { get; set; }

        /// <summary>
        /// 当前步骤(每个步骤开始前提示)
        /// </summary>
        public event Action<UpdateStepEnum> OnStep;

        /// <summary>
        /// 错误
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// 是否需要更新事件
        /// </summary>
        public event Action<bool, Version> OnIsNeedUpdate;

        /// <summary>
        /// 升级进度
        /// </summary>
        public event Action<DownloadProgressChangedEventArgs> OnDownloadProgressChanged;

        /// <summary>
        /// 升级完成
        /// </summary>
        public event Action OnDownloadFileCompleted;

        /// <summary>
        /// 解压完成
        /// </summary>
        public event Action OnExtractCompleted;

        /// <summary>
        /// 升级配置模型
        /// </summary>
        private UpdateModel _updateModel;

        /// <summary>
        /// 初始化类
        /// </summary>
        /// <param name="nowVersion">当前版本</param>
        /// <param name="updateJsonAddress">升级Json地址</param>
        public AiUpdate(string nowVersion, string updateJsonAddress)
        {
            NowVersion = nowVersion;
            UpdateJsonAddress = updateJsonAddress;

            IsOnlyCompare = false;
            DownFileSavePath = $"{AppDomain.CurrentDomain.BaseDirectory}Update";
            DownFileCopyToPath = AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            // 比较版本
            CompareToVersion();
        }

        /// <summary>
        /// 比较版本
        /// </summary>
        private void CompareToVersion()
        {
            Task.Run(async () =>
            {
                OnStep?.Invoke(UpdateStepEnum.IsNeedUpdate);
                // 升级地址校验
                if (string.IsNullOrWhiteSpace(UpdateJsonAddress) || !UpdateJsonAddress.MatchUrl())
                {

                    OnError?.Invoke($"升级配置地址校验失败:{UpdateJsonAddress}");
                    OnStep?.Invoke(UpdateStepEnum.Fail);
                    return;
                }
                // 获取升级Json文件并格式
                var updateString = await UpdateJsonAddress.HttpGetStringAsync();
                try
                {
                    _updateModel = updateString.JsonDeserialize<UpdateModel>();
                }
                catch
                {
                    OnError?.Invoke($"升级配置地址下载失败:{updateString}");
                    OnStep?.Invoke(UpdateStepEnum.Fail);
                    return;
                }
                // 版本号比较
                var nowVer = new Version(NowVersion);
                var newVer = new Version(_updateModel.Version);
                if (newVer.CompareTo(nowVer) > 0)
                {
                    OnIsNeedUpdate?.Invoke(true, newVer);
                    if (!IsOnlyCompare)
                    {
                        // 开始下载文件
                        DownFile();
                    }
                }
                else
                {
                    OnIsNeedUpdate?.Invoke(false, newVer);
                    OnStep?.Invoke(UpdateStepEnum.Complete);
                }
            });
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <returns></returns>
        private void DownFile()
        {
            Task.Run(() =>
            {
                if (_updateModel == null)
                {
                    OnError?.Invoke("请先执行比较版本步骤");
                    OnStep?.Invoke(UpdateStepEnum.Fail);
                    return;
                }
                if (_updateModel.DownFileAddress != null && _updateModel.DownFileAddress.Count > 0)
                {
                    OnStep?.Invoke(UpdateStepEnum.DownFile);
                    // 创建目录
                    if (!Directory.Exists(DownFileSavePath))
                    {
                        Directory.CreateDirectory(DownFileSavePath);
                    }
                    // 验证证书
                    ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
                    var failed = 0;
                    _updateModel.DownFileAddress.ForEach(async url =>
                    {
                        await Task.Run(async ()=>
                        {
                            // 升级下载文件地址校验
                            if (string.IsNullOrWhiteSpace(url) || !url.MatchUrl())
                            {
                                Interlocked.Increment(ref failed);
                                OnError?.Invoke($"下载升级文件地址校验失败:{_updateModel.DownFileAddress}");
                                //OnStep?.Invoke(UpdateStepEnum.Fail);
                                return;
                            }
                            // 下载
                            var webClient = new WebClient();
                            webClient.Headers.Add("User-Agent", "Mozilla / 5.0(compatible; MSIE 9.0; Windows NT 6.1; Trident / 5.0)");
                            webClient.DownloadProgressChanged += (sender, e) =>
                            {
                                OnDownloadProgressChanged?.Invoke(e);
                            };
                            webClient.DownloadFileCompleted += (sender, e) =>
                            {
                                if (e.Error != null)
                                {
                                    Interlocked.Increment(ref failed);
                                    OnError?.Invoke($"下载升级文件失败:{e.Error.Message} 地址:{_updateModel.DownFileAddress}");
                                    //OnStep?.Invoke(UpdateStepEnum.Fail);
                                    return;
                                }
                            };
                            var downFilePath = $"{DownFileSavePath}\\{Path.GetFileName(url)}";
                            // 开始下载
                            await webClient.DownloadFileTaskAsync(new Uri(url), downFilePath);
                        });
                    });
                    if (failed > 0)
                    {
                        OnError?.Invoke($"下载升级文件失败:{failed}个");
                        OnStep?.Invoke(UpdateStepEnum.Fail);
                        return;
                    }
                    // 下载完成
                    OnDownloadFileCompleted?.Invoke();
                    if (_updateModel.IsExtract)
                    {
                        // 解压文件
                        ExtractFile();
                    }
                    else
                    {
                        // 复制文件
                        CopyFile();
                    }
                }
                else
                {
                    OnError?.Invoke("下载升级文件列表为空");
                    OnStep?.Invoke(UpdateStepEnum.Complete);
                }
            });
        }

        /// <summary>
        /// 解压文件
        /// </summary>
        private void ExtractFile()
        {
            Task.Run(() =>
            {
                if (_updateModel == null)
                {
                    OnError?.Invoke("请先执行比较版本步骤");
                    OnStep?.Invoke(UpdateStepEnum.Fail);
                    return;
                }
                OnStep?.Invoke(UpdateStepEnum.ExtractFile);
                var failed = 0;
                _updateModel.DownFileAddress.ForEach(url =>
                {
                    var downFilePath = $"{DownFileSavePath}\\{Path.GetFileName(url)}";
                    if (!File.Exists(downFilePath))
                    {
                        Interlocked.Increment(ref failed);
                        OnError?.Invoke($"解压升级文件不存在:{downFilePath}");
                        //OnStep?.Invoke(UpdateStepEnum.Fail);
                        return;
                    }
                    try
                    {
                        // 解压
                        var compressor = new SevenZipCompressor(null);
                        compressor.Decompress(downFilePath, DownFileSavePath);
                    }
                    catch (Exception e)
                    {
                        Interlocked.Increment(ref failed);
                        OnError?.Invoke($"解压升级文件失败:{e.Message}");
                        //OnStep?.Invoke(UpdateStepEnum.Fail);
                    }
                    finally
                    {
                        // 删除文件
                        File.Delete(downFilePath);
                    }
                });
                if (failed > 0)
                {
                    OnError?.Invoke($"解压升级文件失败:{failed}个");
                    OnStep?.Invoke(UpdateStepEnum.Fail);
                    return;
                }
                OnExtractCompleted?.Invoke();
                // 复制文件
                CopyFile();
            });
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        private void CopyFile()
        {
            // 复制目录
            FileHelper.CopyDirectory(DownFileSavePath, DownFileCopyToPath, true);
            OnStep?.Invoke(UpdateStepEnum.Complete);
        }
    }
}