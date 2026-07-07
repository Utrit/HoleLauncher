namespace HoleLauncher.Core.DTO;

public class UserDTO
{
    public string Username { get; set; }
    public string BackendAddress { get; set; }
    public string SelectedInstance { get; set; }
    public string RamAmount { get; set; }

    public UserDTO(string username, string backendAddress, string selectedInstance, string ramAmount)
    {
        Username = username;
        BackendAddress = backendAddress;
        SelectedInstance = selectedInstance;
        RamAmount = ramAmount;
    }
}