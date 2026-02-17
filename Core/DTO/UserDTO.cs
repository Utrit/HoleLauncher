namespace HoleLauncher.Core.DTO;

public class UserDTO
{
    public string Username { get; set; }
    public string BackendAddress { get; set; }
    public string SelectedInstance { get; set; }

    public UserDTO(string username, string backendAddress, string selectedInstance)
    {
        Username = username;
        BackendAddress = backendAddress;
        SelectedInstance = selectedInstance;
    }
}