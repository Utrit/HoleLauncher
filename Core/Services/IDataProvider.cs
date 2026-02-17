namespace HoleLauncher.Core.Services;

public interface IDataProvider
{
    public void Save<T>(T data) where T : class;
    public T? Load<T>() where T : class;
}