namespace AKEBDELight.Services;

public interface ICurrentUserService
{
    string GetWindowsUserName();
    string GetDisplayName();
}
