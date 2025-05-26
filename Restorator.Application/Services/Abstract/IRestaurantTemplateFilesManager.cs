namespace Restorator.Application.Services.Abstract
{
    public interface IRestaurantTemplateFilesManager
    {
        Task<string> UploadTemplate(byte[] template);
        Task UpdateTemplate(string templatePath, byte[] template);
        Task DeleteTemplate(string templatePath);
    }
}