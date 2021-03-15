using System;
using System.ComponentModel;

namespace AiSoft.Update.Enums
{
    public enum UpdateStepEnum
    {
        [Description("检测中...")]
        IsNeedUpdate,
        [Description("下载中...")]
        DownFile,
        [Description("解压中...")]
        ExtractFile,
        [Description("完成")]
        Complete,
        [Description("失败")]
        Fail,
    }
}