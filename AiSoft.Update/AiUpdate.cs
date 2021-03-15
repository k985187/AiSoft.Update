using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using AiSoft.Tools.Extensions;
using AiSoft.Tools.Files;
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
        /// <para>是否静默升级</para>
        /// <para>如果否，则需要手动调用继续步骤</para>
        /// </summary>
        public bool IsSilent { get; set; }

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
        /// 是否解压(默认:否)
        /// </summary>
        public bool IsExtract { get; set; }

        /// <summary>
        /// 下载文件解压路径(默认:本地目录)
        /// </summary>
        public string DownFileExtractPath { get; set; }

        /// <summary>
        /// 是否删除下载文件(默认:否)
        /// </summary>
        public bool IsDeleteDownFile { get; set; }

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
        public event Action OnExtractFiled;

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

            IsSilent = false;
            DownFileSavePath = $"{AppDomain.CurrentDomain.BaseDirectory}Update";
            IsExtract = false;
            DownFileExtractPath = AppDomain.CurrentDomain.BaseDirectory;
            IsDeleteDownFile = false;
        }

        /// <summary>
        /// 开始
        /// </summary>
        public void Start()
        {
            // 比较版本
            CompareVersion();
        }

        /// <summary>
        /// 比较版本
        /// </summary>
        public void CompareVersion()
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
                    if (IsSilent)
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
        public void DownFile()
        {
            Task.Run(() =>
            {
                if (_updateModel == null)
                {
                    OnError?.Invoke("请先执行比较版本步骤");
                    OnStep?.Invoke(UpdateStepEnum.Fail);
                    return;
                }
                OnStep?.Invoke(UpdateStepEnum.DownFile);
                // 升级下载文件地址校验
                if (string.IsNullOrWhiteSpace(UpdateJsonAddress) || !_updateModel.DownFileAddress.MatchUrl())
                {
                    OnError?.Invoke($"下载升级文件地址校验失败:{_updateModel.DownFileAddress}");
                    OnStep?.Invoke(UpdateStepEnum.Fail);
                    return;
                }
                _updateModel.DownFileSavePath = $"{DownFileSavePath}\\{Path.GetFileName(_updateModel.DownFileAddress)}";
                // 创建目录
                if (!Directory.Exists(DownFileSavePath))
                {
                    Directory.CreateDirectory(DownFileSavePath);
                }
                // 验证证书
                ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
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
                        OnError?.Invoke($"下载升级文件失败:{e.Error.Message} 地址:{_updateModel.DownFileAddress}");
                        OnStep?.Invoke(UpdateStepEnum.Fail);
                        return;
                    }
                    OnDownloadFileCompleted?.Invoke();
                    if (IsSilent && IsExtract)
                    {
                        // 解压文件
                        ExtractFile();
                    }
                    else
                    {
                        // 删除文件
                        DeleteFile();
                        OnStep?.Invoke(UpdateStepEnum.Complete);
                    }
                };
                // 开始下载
                webClient.DownloadFileAsync(new Uri(_updateModel.DownFileAddress), _updateModel.DownFileSavePath);
            });
        }

        /// <summary>
        /// 解压文件
        /// </summary>
        public void ExtractFile()
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
                if (string.IsNullOrWhiteSpace(_updateModel.DownFileSavePath) || !File.Exists(_updateModel.DownFileSavePath))
                {
                    OnError?.Invoke($"解压升级文件不存在:{_updateModel.DownFileSavePath}");
                    OnStep?.Invoke(UpdateStepEnum.Fail);
                    return;
                }
                try
                {
                    // 解压
                    var compressor = new SevenZipCompressor(null);
                    compressor.Decompress(_updateModel.DownFileSavePath, DownFileExtractPath);
                    OnExtractFiled?.Invoke();
                    OnStep?.Invoke(UpdateStepEnum.Complete);
                }
                catch (Exception e)
                {
                    OnError?.Invoke($"解压升级文件失败:{e.Message}");
                    OnStep?.Invoke(UpdateStepEnum.Fail);
                }
                finally
                {
                    // 删除文件
                    DeleteFile();
                }
            });
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        private void DeleteFile()
        {
            if (IsDeleteDownFile && !string.IsNullOrWhiteSpace(_updateModel.DownFileSavePath) && File.Exists(_updateModel.DownFileSavePath))
            {
                File.Delete(_updateModel.DownFileSavePath);
            }
        }
    }
}