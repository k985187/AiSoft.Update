using System;
using AiSoft.Tools.NotifyBase;
using Newtonsoft.Json;

namespace AiSoft.Update.Models
{
    public class FileModel : PropertyBase
    {
        private string _downFileAddress;

        /// <summary>
        /// 下载文件地址
        /// </summary>
        public string DownFileAddress
        {
            get => _downFileAddress;
            set => SetProperty(ref _downFileAddress, value);
        }

        private bool _isExtract;

        /// <summary>
        /// 是否解压(默认:否)
        /// </summary>
        public bool IsExtract
        {
            get => _isExtract;
            set => SetProperty(ref _isExtract, value);
        }

        private string _localSavePath;

        /// <summary>
        /// 本地保存路径
        /// </summary>
        [JsonIgnore]
        public string LocalSavePath
        {
            get => _localSavePath;
            set => SetProperty(ref _localSavePath, value);
        }

        private bool _downCompleted;

        /// <summary>
        /// 下载完成
        /// </summary>
        [JsonIgnore]
        public bool DownCompleted
        {
            get => _downCompleted;
            set => SetProperty(ref _downCompleted, value);
        }

        private bool _extractCompleted;

        /// <summary>
        /// 解压完成
        /// </summary>
        [JsonIgnore]
        public bool ExtractCompleted
        {
            get => _extractCompleted;
            set => SetProperty(ref _extractCompleted, value);
        }
    }
}