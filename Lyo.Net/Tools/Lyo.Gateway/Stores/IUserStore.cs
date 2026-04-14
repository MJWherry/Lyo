using Lyo.Web.Components.Models;

namespace Lyo.Gateway.Stores;

public interface IUserStore
{
    void AddOrUpdateUser(string tokenId, BlazorUserInfo userInfo);

    BlazorUserInfo? GetUser(string tokenId);

    void RemoveUser(string tokenId);

    IEnumerable<BlazorUserInfo> GetAllUsers();

    bool IsUserSignedIn(string tokenId);

    void UpdateUserCurrentPage(string tokenId, string currentPage);
}