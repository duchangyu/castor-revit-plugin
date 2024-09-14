using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CastorPlugin.Services.DTO
{
    internal class NftWorksCandidates
    {
        public string Name { get; set; } // 资产名称

        public int Type { get; set; } // 资产类型


        /// <summary>
        /// 获取或设置Web3资产的唯一标识符。
        /// </summary>
        public string FingerPrintHash { get; set; } // unique id of web3 asset

        /// <summary>
        /// 获取或设置Web3资产对象的JSON表示。
        /// </summary>
        public string FingerPrintInJson { get; set; } // json表示的web3 asset对象

        /// <summary>
        /// 获取或设置asset对象的缩略图的base64表示。
        /// </summary>
        public string Thumbnail { get; set; } // base64表示的asset对象的缩略图

        ///// <summary>
        ///// 获取或设置一个值，指示该资产是否已被抢占。
        ///// </summary>
        //public bool IsAcquired { get; set; } // 是不是被抢占了

        ///// <summary>
        ///// 获取或设置抢占该资产的人的用户ID。
        ///// </summary>
        //public int AcquiredBy { get; set; } // 被抢占的人的用户ID

        ///// <summary>
        ///// 获取或设置资产的初次上传时间。
        ///// </summary>
        //public DateTime UploadedOn { get; set; } // 初次上传的时间

        ///// <summary>
        ///// 获取或设置资产被抢占的时间。
        ///// </summary>
        //public DateTime AcquiredOn { get; set; } // 被抢占的时间
    }
}
