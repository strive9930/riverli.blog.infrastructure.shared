// 1. 声明上报的 DTO 模型
namespace RiverLi.Blog.Infrastructure.Shared.OpenApi
{
    public class ReportApiDto
    {
        public string Method { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }
}