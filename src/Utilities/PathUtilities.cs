﻿using System;
using System.IO;
using Microsoft.AspNetCore.Components;

namespace MikeCodesDotNET.Utilities;


public static class PathUtilities
{

    public static string NormalizeMarkdownPath(string path)
    {
        var result = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if(!result.EndsWith(".md", StringComparison.InvariantCultureIgnoreCase))
        {
            if(File.Exists(result + ".md"))
            {
                result += ".md";
            }
            else if(Directory.Exists(path))
            {
                result = Path.Combine(result, "index.md");
            }
        }
        return result;
    }

    public static string PhysicalPathToContent(
        this NavigationManager navigationManager,
        string webRootPath,
        string physicalPath,
        bool isDirectory)
    {
        var directory = Path.GetDirectoryName(physicalPath);
        var fileName = Path.GetFileNameWithoutExtension(physicalPath);
        var path = Path.Combine(directory ?? string.Empty, fileName);
        var result = Path.GetRelativePath(webRootPath, path).Replace('\\', '/');

        if(isDirectory && !result.EndsWith('/'))
        {
            result += '/';
        }

        var url = (navigationManager.Uri + result);
        return url;
    }

}
