using System.IO;
using System.Text.Json;

namespace HoleLauncher.Core.Services;

public class DataProvider : IDataProvider
{
    public void Save<T>(T data) where T : class
    {
        Save(data, GetPath<T>());
    }

    public void Save<T>(T data, string path) where T : class
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllTextAsync(path, json);
    }

    public T? Load<T>() where T : class
    {
        return Load<T>(GetPath<T>());
    }
    
    public T? Load<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        var data = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(data)) return null;
        return JsonSerializer.Deserialize<T>(data);
    }

    private string GetPath<T>() where T : class
    {
        return "./" + typeof(T).Name + ".json";
    }
}