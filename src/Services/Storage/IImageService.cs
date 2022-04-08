using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MikeCodesDotNET.Models;
using Tewr.Blazor.FileReader;

namespace MikeCodesDotNET.Services
{
    public interface IImageService
    {
        Task<IList<Image>> GetImagesAsync();

        Task<Image> GetImageAsync(string id);

        Task<Image> SaveImageAsync(IFileReference file);

        Task<Image> SaveImageFromUrlAsync(Uri imageUrl);
    }
}
