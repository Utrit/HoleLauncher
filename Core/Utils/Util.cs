using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HoleLauncher.Core.Utils;

public static class Util
{
    public static async Task<string> GetFileSHA(string filePath)
    {
        await using var read =  File.OpenRead(filePath);
        return Convert.ToHexString(await SHA512.HashDataAsync(read)).ToLower();
    }

    public static async Task DownloadFileAsync(string url, string outputPath, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var inputStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await inputStream.CopyToAsync(fileStream, cancellationToken);
        fileStream.Close();
    }

    public static async Task<T?> DownloadJsonAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
    {
        using var client = new HttpClient();
        try
        {
            var res = await client.GetFromJsonAsync<T>(url, cancellationToken: cancellationToken);
            return res;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request error: {e.Message}");
            return null;
        }
        catch (JsonException e)
        {
            Console.WriteLine($"JSON deserialization error: {e.Message}");
            return null;
        }
    }
    
}