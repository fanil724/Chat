using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace ChatClient
{

    public partial class MainWindow : Window
    {
        IPEndPoint iPEnd;
        TcpClient client = null;
        StreamReader? reader = null;
        StreamWriter? writer = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void SendAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (stringAnswer.Text == string.Empty && stringAnswer.Text == "Введите сообщение")
            {
                Chat.Items.Add("Введите сообщение!!!");
                return;
            }
            await SendMessageAsync(writer, stringAnswer.Text);
            Chat.Items.Add($"Вы отправили {stringAnswer.Text}");
            stringAnswer.Text = "Введите сообщение";

        }
        private void stringAnswer_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (stringAnswer.Text == "Введите сообщение") stringAnswer.Text = string.Empty;
        }
        private void stringAnswer_LostFocus(object sender, RoutedEventArgs e)
        {
            if (stringAnswer.Text == string.Empty) stringAnswer.Text = "Введите сообщение";
        }

        private void Connnect_Click(object sender, RoutedEventArgs e)
        {
            if (LoginChat.Text == string.Empty) { Chat.Items.Add("Введите имя"); return; }

            try
            {
                client = new TcpClient();
                string host = "127.0.0.1";
                int port = 888;
                iPEnd = new IPEndPoint(IPAddress.Parse(host), port);
                client.Connect(iPEnd);
                reader = new StreamReader(client.GetStream());
                writer = new StreamWriter(client.GetStream());
                if (reader is null || writer is null) return;
                writer.WriteLineAsync(LoginChat.Text);
                writer.FlushAsync();

                Task.Run(() => ResiveMessageAsync(reader));
                Dispatcher.Invoke(() => Chat.Items.Add("Соединение успешно"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => Chat.Items.Add(ex.Message));
            }
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            client?.Close();
            writer?.Close();
            reader?.Close();
        }

        private async Task ResiveMessageAsync(StreamReader reader)
        {
            while (true)
            {
                try
                {
                    string? message = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(message)) continue;
                    Dispatcher.Invoke(() => Chat.Items.Add(message));
                }
                catch
                {
                    break;
                }
            }
        }

        private async Task SendMessageAsync(StreamWriter writer, string message)
        {
            await writer.WriteLineAsync(message);
            await writer.FlushAsync();
        }
    }


}