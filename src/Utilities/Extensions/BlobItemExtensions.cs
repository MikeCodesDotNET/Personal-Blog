using Azure.Storage.Blobs.Models;

using System.IO;
using System.Linq;
using System.Net.Mime;

namespace MikeCodesDotNET.Utilities.Extensions;

public static class BlobItemExtensions
{
    public static BlobItemType GetFileType(this BlobItem blobItem)
    {
        switch(blobItem.Properties.ContentType)
        {
            case MediaTypeNames.Text.Plain:
                if(blobItem.IsMarkdownDocument())
                    return BlobItemType.Markdown;
                else 
                    return BlobItemType.Text;

            case MediaTypeNames.Image.Tiff:
            case MediaTypeNames.Image.Jpeg:
            case MediaTypeNames.Image.Gif:
                return BlobItemType.Image;

            case MediaTypeNames.Application.Xml:
            case MediaTypeNames.Text.Xml:
                return BlobItemType.Xml;

            case MediaTypeNames.Application.Json:
                return BlobItemType.Json;

            case MediaTypeNames.Application.Pdf:
                return BlobItemType.Pdf;

            case MediaTypeNames.Application.Zip:
                return BlobItemType.Zip;

            default:
                return BlobItemType.Unknown;
        }
    }

    public static bool IsMarkdownDocument(this BlobItem blobItem)
    {
        if (blobItem.Properties.ContentType == "text/plain" &&
            Path.GetExtension(GetFileName(blobItem)) == ".md")
            return true;

        return false;
    }
    
    public static string GetDirectoryName(this BlobItem blobItem)
    {
        var split = blobItem.Name.Split('/');
        if (split.Length >= 2)
        {
            return split[split.Length - 2];
        }
        throw new System.Exception("Expected the blob name to have at least 1 forward slash");
    }

    public static string GetRootDirectoryName(this BlobItem blobItem)
    {
        var split = blobItem.Name.Split('/');
        if (split.Length >= 2)
        {
            return split.FirstOrDefault();
        }
        throw new System.Exception("Expected the blob name to have at least 1 forward slash");
    }


    public static string GetFileName(this BlobItem blobItem)
    {
        var split = blobItem.Name.Split('/');
        if (split.Length >= 2)
        {
            return split.LastOrDefault();
        }
        throw new System.Exception("Expected the blob name to have at least 1 forward slash");
    }


}

public enum BlobItemType
{
    Markdown,
    Image,

    Text,

    Xml,
    Json,

    Pdf,
    Zip,

    Unknown,
}

