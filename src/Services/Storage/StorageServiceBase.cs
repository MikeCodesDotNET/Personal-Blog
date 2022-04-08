using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Microsoft.Extensions.Configuration;

using MikeCodesDotNET.Models;

using Tewr.Blazor.FileReader;

namespace MikeCodesDotNET.Services;


public abstract class StorageServiceBase<T> where T : BlobFileBase
{
    private readonly string _containerName;
    private BlobServiceClient blobServiceClient;
    private BlobContainerClient containerClient;
    private bool isInitialized;

    public StorageServiceBase(string containerName, IConfiguration configuration)
    {
        _containerName = containerName;
        string connectionString = configuration.GetConnectionString("Storage");
        blobServiceClient = new BlobServiceClient(connectionString);
    }


    public async Task<T> SaveAsync(IFileReference file)
    {
        if (!isInitialized)
            await Initialize();

        var fileInfo = await file.ReadFileInfoAsync();
        var blobClient = containerClient.GetBlobClient(fileInfo.Name);
        using (var fs = await file.OpenReadAsync())
        {
            await blobClient.UploadAsync(fs, true);
        }

        T blobFile = (T)Activator.CreateInstance(typeof(T));
        blobFile.FileName = fileInfo.Name;
        blobFile.Url = blobClient.Uri;
        return blobFile;
    }


    public async Task<T> SaveFromUrlAsync(Uri fileUrl)
    {
        if (!isInitialized)
            await Initialize();

        HttpWebRequest aRequest = (HttpWebRequest)WebRequest.Create(fileUrl);
        HttpWebResponse aResponse = (HttpWebResponse)aRequest.GetResponse();

        using (var strm = aResponse.GetResponseStream())
        {
            var fileName = System.IO.Path.GetFileName(fileUrl.ToString());
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(strm, true);
                         
            T blobFile = (T)Activator.CreateInstance(typeof(T));
            blobFile.FileName = fileName;
            blobFile.Url = blobClient.Uri;
            return blobFile;
        }
    }


    public async Task<T> GetAsync(string id)
    {
        if (!isInitialized)
            await Initialize();

        throw new NotImplementedException();
    }


    public async Task<IList<T>> GetAsync()
    {
        if (!isInitialized)
            await Initialize();

        var results = new List<T>();
        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
        {
            var url = Path.Combine(containerClient.Uri.AbsoluteUri, blobItem.Name);

            T result = (T)Activator.CreateInstance(typeof(T));
            result.Url = new Uri(url);
            result.FileName = blobItem.Name;
            results.Add(result);
        }

        return results;
    }


    private async Task Initialize()
    {
        try
        {
            containerClient = await blobServiceClient.CreateBlobContainerAsync(_containerName);
        }
        catch
        {
            containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
        }

        if(containerClient != null)
            isInitialized = true;
    }
}
