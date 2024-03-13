using System.Threading.Tasks;

namespace OrchardCore.ContentManagement
{
    public interface IContentHandleManager
    {
        Task<string> GetContentItemIdAsync(string handle);
    }
}
