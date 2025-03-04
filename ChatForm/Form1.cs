using ChatLibrary;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatForm
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public partial class Form1 : Form, IChatCallback
    {
        private IChatService _chatService;
        private DuplexChannelFactory<IChatService> _factory;
        private string _username;
        private string _selectedUser;

        public Form1()
        {
            InitializeComponent();
        }

        private async Task ConnectToServerAsync()
        {
            try
            {
                await InitializeChatServiceAsync();
                var connected = await ConnectToChatServiceAsync();

                if (connected)
                {
                    UpdateUIOnSuccess();
                }
                else
                {
                    UpdateUIOnFailure();
                }
            }
            catch
            {
                UpdateUIOnFailure();
            }
        }

        private async Task InitializeChatServiceAsync()
        {
            _factory = new DuplexChannelFactory<IChatService>(this, new NetTcpBinding(),
                new EndpointAddress("net.tcp://localhost:8000/ChatService"));
            _chatService = _factory.CreateChannel();
        }

        private async Task<bool> ConnectToChatServiceAsync()
        {
            return await _chatService.ConnectAsync(_username);
        }

        private void UpdateUIOnSuccess()
        {
            ChatBox.AppendText("Connected to chat." + Environment.NewLine);
            ConnectButton.Enabled = false;
            UsernameBox.Enabled = false;
        }

        private void UpdateUIOnFailure()
        {
            ChatBox.AppendText("Failed to connect." + Environment.NewLine);
        }


        public void ReceiveMessage(string message)
        {
            ChatBox.Invoke(new MethodInvoker(() => ChatBox.Text += message + Environment.NewLine));
        }

        private void UpdateUserListUI(List<string> users)
        {
            OnlineUsers.Items.Clear();
            foreach (var user in users)
            {
                OnlineUsers.Items.Add(user);
            }
        }

        public void UpdateUserList(List<string> users)
        {
            if (OnlineUsers.InvokeRequired)
            {
                OnlineUsers.Invoke(new Action(() => UpdateUserListUI(users)));
            }
            else
            {
                UpdateUserListUI(users);
            }
        }

        private async void ConnectButton_Click(object sender, EventArgs e)
        {
            _username = UsernameBox.Text;
            await ConnectToServerAsync();
        }

        private const string TimeFormat = "HH:mm";

        private string FormatMessage(string message, string sender, string recipient)
        {
            var currentTime = DateTime.Now.ToString(TimeFormat);
            return !string.IsNullOrEmpty(recipient) && recipient != sender
                ? $"[{currentTime}] [You -> {recipient}]: {message}"
                : $"[{currentTime}] {message}";
        }

        private async void SendButton_Click(object sender, EventArgs e)
        {
            var message = MessageBox.Text;
            if (string.IsNullOrEmpty(message)) return;

            string formattedMessage = FormatMessage(message, _username, _selectedUser);
            ChatBox.AppendText(formattedMessage + Environment.NewLine);

            try
            {
                if (!string.IsNullOrEmpty(_selectedUser) && _selectedUser != _username)
                {
                    await _chatService.SendPrivateMessageAsync(message, _selectedUser, _username);
                }
                else
                {
                    await _chatService.SendMessageToGroupAsync(message, _username);
                }
            }
            catch (Exception ex)
            {
                ChatBox.AppendText($"Error sending message: {ex.Message}" + Environment.NewLine);
            }

            ChatBox.SelectionStart = ChatBox.Text.Length;
            ChatBox.ScrollToCaret();
            MessageBox.Clear();
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_chatService != null && !string.IsNullOrEmpty(_username))
            {
                await _chatService.DisconnectAsync(_username);
            }
        }

        private void OnlineUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (OnlineUsers.SelectedItem != null)
            {
                if (_selectedUser != null && _selectedUser == OnlineUsers.SelectedItem.ToString())
                {
                    OnlineUsers.ClearSelected();
                    _selectedUser = null; 
                }
                else
                {
                    _selectedUser = OnlineUsers.SelectedItem.ToString();
                }
            }
            else
            {
                _selectedUser = null;
            }
        }
    }
}
