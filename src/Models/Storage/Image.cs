namespace MikeCodesDotNET.Models;
public class Image : BlobFileBase
{
    public string? Title { get; set; }

    public string? Caption { get; set; }

    public string? AltText { get; set; }

    public string? Description { get; set; }

    public ContentType Type => ContentType.Image;

    public static Image CreateBackgroundHeader(HeaderBackgroundImageType type = HeaderBackgroundImageType.Blue1)
    {
        var img = new Image()
        {
            Title = "Background Waves",
            Caption = string.Empty,
            AltText = "Multicoloured wave pattern",
            Description = string.Empty,            
        };

        switch (type)
        {
            case HeaderBackgroundImageType.Blue1:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Blue1.png");
                break;
            case HeaderBackgroundImageType.Blue2:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Blue2.png");
                break;
            case HeaderBackgroundImageType.Cyan:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Cyan1.png");
                break;
            case HeaderBackgroundImageType.Orange1:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Orange1.png");
                break;
            case HeaderBackgroundImageType.Orange2:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Orange2.png");
                break;
            case HeaderBackgroundImageType.Purple1:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Purple1.png");
                break;
            case HeaderBackgroundImageType.Purple2:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Purple2.png");
                break;
            case HeaderBackgroundImageType.Red1:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Red1.png");
                break;
            case HeaderBackgroundImageType.Red2:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Red2.png");
                break;
            default:
                img.Url = new System.Uri("https://mikecodes.blob.core.windows.net/images/Waves/Blue1.png");
                break;
        }

        return img;
    }
}

public enum HeaderBackgroundImageType
{
    Blue1,
    Blue2,
    Cyan,
    Orange1,
    Orange2,
    Purple1,
    Purple2,
    Red1,
    Red2
}

