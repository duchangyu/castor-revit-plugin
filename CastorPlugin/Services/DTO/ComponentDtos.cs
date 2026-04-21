namespace CastorPlugin.Services.DTO
{
    /// <summary>
    /// 结构化指纹 DTO - 用于构件登记
    /// </summary>
    public class StructuredFingerprintDto
    {
        /// <summary>
        /// 构件类别（族类别）
        /// </summary>
        public string ComponentCategory { get; set; }

        /// <summary>
        /// 关键参数键值对
        /// </summary>
        public Dictionary<string, object> KeyParameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 尺寸几何描述
        /// </summary>
        public string SizeGeometryDescription { get; set; }

        /// <summary>
        /// 材质或用途
        /// </summary>
        public string MaterialOrIntendedUse { get; set; }
    }

    /// <summary>
    /// 缩略图输入 DTO
    /// </summary>
    public class ThumbnailInputDto
    {
        public string StorageKey { get; set; }
        public string MimeType { get; set; } = "image/png";
        public long FileSizeBytes { get; set; }
        public int WidthPx { get; set; }
        public int HeightPx { get; set; }
    }

    /// <summary>
    /// 创建构件请求 DTO
    /// </summary>
    public class CreateComponentDto
    {
        /// <summary>
        /// 构件标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 构件类别
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 构件详细描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 关键属性
        /// </summary>
        public Dictionary<string, object> KeyAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 结构化指纹
        /// </summary>
        public StructuredFingerprintDto Fingerprint { get; set; }

        /// <summary>
        /// 缩略图
        /// </summary>
        public ThumbnailInputDto Thumbnail { get; set; }

        /// <summary>
        /// 是否接受归属声明
        /// </summary>
        public bool AttributionDeclarationAccepted { get; set; } = true;

        /// <summary>
        /// 是否接受非法律确权说明
        /// </summary>
        public bool NonLegalNoticeAccepted { get; set; } = true;
    }
}
