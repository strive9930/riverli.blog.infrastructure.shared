namespace RiverLi.Blog.Infrastructure.Shared.Logging
{
    /// <summary>
    /// 详细配置类：高度可配置的日志方案，支持异步写入、结构化属性和全链路追踪
    /// </summary>
    public class LoggingOptions
    {
        public const string SectionName = "RiverLogging";
        public string SeqUrl { get; set; } = string.Empty; // 用于集中展示日志的 UI (可选)
        public string LogPath { get; set; } = "logs/river-log-.json";
        public string MinimumLevel { get; set; } = "Information";
    }
}