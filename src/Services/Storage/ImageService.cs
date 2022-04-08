using Microsoft.Extensions.Configuration;

using MikeCodesDotNET.Models;

namespace MikeCodesDotNET.Services
{
    public class ImageService : StorageServiceBase<Image>
    {
        public ImageService(IConfiguration configuration) : base("images", configuration)
        {
        }


        //private const string ImagesContainerName = "images";
        //private BlobServiceClient blobServiceClient;
        //private BlobContainerClient containerClient;
        //private bool initialized;

        //public ImageService(IConfiguration configuration)
        //{
        //    string connectionString = configuration.GetConnectionString("Storage");
        //    blobServiceClient = new BlobServiceClient(connectionString);
        //}

        //private async Task Initialize()
        //{
        //    try
        //    {
        //        containerClient = await blobServiceClient.CreateBlobContainerAsync(ImagesContainerName);
        //    }
        //    catch (Exception e)
        //    {
        //        containerClient = blobServiceClient.GetBlobContainerClient(ImagesContainerName);
        //    }
        //}


        //public async Task<Image> SaveImageAsync(IFileReference file)
        //{
        //    if (!initialized)
        //        await Initialize();

        //    var fileInfo = await file.ReadFileInfoAsync();
        //    var blobClient = containerClient.GetBlobClient(fileInfo.Name);
        //    using (var fs = await file.OpenReadAsync())
        //    {
        //        await blobClient.UploadAsync(fs, true);
        //    }

        //    var image = new Image();
        //    image.FileName = fileInfo.Name;
        //    image.Url = blobClient.Uri;

        //    return image;
        //}

        //public async Task<Image> SaveImageFromUrlAsync(Uri imageUrl)
        //{
        //    if (!initialized)
        //        await Initialize();

        //    HttpWebRequest aRequest = (HttpWebRequest)WebRequest.Create(imageUrl);
        //    HttpWebResponse aResponse = (HttpWebResponse)aRequest.GetResponse();

        //    using (var strm = aResponse.GetResponseStream())
        //    {
        //        var fileName = System.IO.Path.GetFileName(imageUrl.ToString());
        //        var blobClient = containerClient.GetBlobClient(fileName);
        //        await blobClient.UploadAsync(strm, true);

        //        var image = new Image();
        //        image.FileName = fileName;
        //        image.Url = blobClient.Uri;
        //        return image;
        //    }
        //}


        //public async Task<Image> GetImageAsync(string id)
        //{
        //    if(!initialized)
        //        await Initialize();

        //    throw new NotImplementedException();
        //}

        //public async Task<IList<Image>> GetImagesAsync()
        //{
        //    if (!initialized)
        //        await Initialize();

        //    var result = new List<Image>();
        //    await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
        //    {
        //        var url = Path.Combine(containerClient.Uri.AbsoluteUri, blobItem.Name);
        //        result.Add(new Image { Url = new Uri(url), FileName = blobItem.Name });
        //    }

        //    return result;
        //}
    }
}
