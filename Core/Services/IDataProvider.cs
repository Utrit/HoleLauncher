namespace HoleLauncher.Core.Services;

public interface IDataProvider
{
    public void Save<T>(T data) where T : class;
    public void Save<T>(T data, string path) where T : class;
    public T? Load<T>() where T : class;
    public T? Load<T>(string path) where T : class;
}