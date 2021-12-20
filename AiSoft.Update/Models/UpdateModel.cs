using System;
using System.Collections.Generic;
using AiSoft.Tools.NotifyBase;

namespace AiSoft.Update.Models
{
    /// <summary>
    /// 升级配置文件模型
    /// </summary>
    public class UpdateModel : PropertyBase
    {
        private string _version = "";

        /// <summary>
        /// 升级版本号
        /// </summary>
        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        private List<FileModel> _downFiles;

        /// <summary>
        /// 下载文件地址
        /// </summary>
        public List<FileModel> DownFiles
        {
            get => _downFiles ?? (_downFiles = new List<FileModel>());
            set => SetProperty(ref _downFiles, value);
        }
    }
}