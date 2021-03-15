using System;

namespace AiSoft.Update.Models
{
    /// <summary>
    /// 升级配置文件模型
    /// </summary>
    internal class UpdateModel
    {
        /// <summary>
        /// 升级版本号
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 下载文件地址
        /// </summary>
        public string DownFileAddress { get; set; }

        /// <summary>
        /// 下载文件保存地址
        /// </summary>
        public string DownFileSavePath { get; set; }
    }
}