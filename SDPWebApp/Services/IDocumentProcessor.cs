using SDPWebApp.Models;
namespace SDPWebApp.Services
{
    public interface IDocumentProcessor
    {
        Task<Document> ProcessAsync(IFormFile file);
    }
}
