using ChatLibrary;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using System;

public class ChatService : IChatService
{
    private readonly Dictionary<string, IChatCallback> _users = new Dictionary<string, IChatCallback>();

    public async Task<bool> ConnectAsync(string username)
    {
        try
        {
            var callback = GetCallbackChannel();

            if (IsUserConnected(username))
            {
                return false;
            }

            AddUser(username, callback);
            await UpdateAllUsersAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while connecting user {username}: {ex.Message}");
            return false;
        }
    }

    private IChatCallback GetCallbackChannel() =>
        OperationContext.Current.GetCallbackChannel<IChatCallback>();

    private bool IsUserConnected(string username) =>
        _users.ContainsKey(username);

    private void AddUser(string username, IChatCallback callback) =>
        _users.Add(username, callback);

    public async Task DisconnectAsync(string username)
    {
        if (_users.ContainsKey(username))
        {
            _users.Remove(username);
            await UpdateAllUsersAsync();
        }
    }

    public async Task SendMessageToGroupAsync(string message, string sender)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        var formattedMessage = FormatMessage(message, sender);
        await SendMessageToAllUsersAsync(formattedMessage);
    }

    private string FormatMessage(string message, string sender)
    {
        var currentTime = DateTime.Now.ToString("HH:mm");
        return $"[{currentTime}] {sender}: {message}";
    }

    private async Task SendMessageToAllUsersAsync(string message)
    {
        foreach (var user in _users.Values)
        {
            user.ReceiveMessage(message);
        }
    }

    public async Task SendPrivateMessageAsync(string message, string recipient, string sender)
    {
        if (_users.TryGetValue(recipient, out var callback))
        {
            var formattedMessage = FormatPrivateMessage(message, sender);
            callback.ReceiveMessage(formattedMessage);
        }
        else
        {
            await NotifySenderUserNotFound(sender, recipient);
        }
    }

    private string FormatPrivateMessage(string message, string sender)
    {
        var currentTime = DateTime.Now.ToString("HH:mm");
        return $"[{currentTime}] [Private] {sender}: {message}";
    }

    private async Task NotifySenderUserNotFound(string sender, string recipient)
    {
        if (_users.TryGetValue(sender, out var senderCallback))
        {
            senderCallback.ReceiveMessage($"[System]: User '{recipient}' is not online.");
        }
    }

    public async Task<List<string>> GetOnlineUsersAsync() =>
        new List<string>(_users.Keys);

    private async Task UpdateAllUsersAsync()
    {
        var users = await GetOnlineUsersAsync();
        await NotifyUsersAboutUpdatedList(users);
    }

    private async Task NotifyUsersAboutUpdatedList(List<string> users)
    {
        foreach (var callback in _users.Values)
        {
            callback.UpdateUserList(users);
        }
    }
}
