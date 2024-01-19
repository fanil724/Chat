using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;

namespace Chat
{

    public partial class MainWindow : Window
    {
        ServerObject server;
        public MainWindow()
        {
            server = new ServerObject(this.Dispatcher, this);
            InitializeComponent();
            Task.Run(server.ListenAsync);
        }



        private void stringAnswer_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (stringAnswer.Text == "Введите сообщение") stringAnswer.Text = string.Empty;
        }
        private void stringAnswer_LostFocus(object sender, RoutedEventArgs e)
        {
            if (stringAnswer.Text == string.Empty) stringAnswer.Text = "Введите сообщение";
        }

        private async void SendAnswer_Click(object sender, RoutedEventArgs e)
        {
            if (stringAnswer.Text == string.Empty && stringAnswer.Text == "Введите сообщение")
            {
                Chat.Items.Add("Введите сообщение!!!");
                return;
            }
            await server.BroadcastMessageAsync($"Сервер отправил: {stringAnswer.Text}", "");
            stringAnswer.Text = "Введите сообщение";
        }

    }
    public class ServerObject
    {
        TcpListener tcplistener = new TcpListener(IPAddress.Any, 888);
        List<ClientObject> clients = new List<ClientObject>();
        public Dispatcher dispatcher { get; set; }
        public MainWindow main { get; set; }

        public ServerObject(Dispatcher dispatcher, MainWindow main)
        {
            this.dispatcher = dispatcher;
            this.main = main;
        }

        protected internal void RemoveConnetion(string id)
        {
            ClientObject? client = clients.FirstOrDefault(x => x.Id == id);
            if (client != null) clients.Remove(client);
            client?.Close();
        }
        protected internal async Task ListenAsync()
        {
            try
            {
                tcplistener.Start();
                dispatcher.Invoke(() => main.Chat.Items.Add("Сервер запущен, Ожидайте подключение...."));
                while (true)
                {
                    TcpClient tcpClient = await tcplistener.AcceptTcpClientAsync();
                    ClientObject client = new ClientObject(tcpClient, this);
                    clients.Add(client);
                    _ = Task.Run(client.ProcessAsync);
                }
            }
            catch (Exception ex)
            {
                dispatcher.Invoke(() => main.Chat.Items.Add(ex.ToString()));
            }
            finally { Disconnect(); }
        }
        protected internal async Task BroadcastMessageAsync(string message, string id)
        {
            foreach (ClientObject client in clients)
            {
                if (client.Id != id)
                {
                    await client.Writer.WriteLineAsync(message);
                    await client.Writer.FlushAsync();
                }
            }
        }
        protected internal void Disconnect()
        {
            foreach (ClientObject client in clients)
            {
                client.Close();
            }
            tcplistener.Stop();
        }
    }

    public class ClientObject
    {
        protected internal string Id { get; } = Guid.NewGuid().ToString();
        protected internal StreamWriter Writer { get; }
        protected internal StreamReader Reader { get; }

        TcpClient client;
        ServerObject serverObject;

        public ClientObject(TcpClient tcpClient, ServerObject server)
        {
            client = tcpClient;
            serverObject = server;
            var stream = client.GetStream();
            Writer = new StreamWriter(stream);
            Reader = new StreamReader(stream);
        }

        public async Task ProcessAsync()
        {
            try
            {
                string? userName = await Reader.ReadLineAsync();
                string? message = $"{userName} вошел в чат";
                await serverObject.BroadcastMessageAsync(message, Id);
                serverObject.dispatcher.Invoke(() => serverObject.main.Chat.Items.Add(message));
                while (true)
                {
                    try
                    {
                        message = await Reader.ReadLineAsync();
                        if (message == null) continue;
                        message = $"{userName}: {message}";
                        serverObject.dispatcher.Invoke(() => serverObject.main.Chat.Items.Add(message));
                        await serverObject.BroadcastMessageAsync(message, Id);
                    }
                    catch
                    {
                        message = $"{userName} покинул чат";
                        serverObject.dispatcher.Invoke(() => serverObject.main.Chat.Items.Add(message));
                        await serverObject.BroadcastMessageAsync(message, Id);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                serverObject.dispatcher.Invoke(() => serverObject.main.Chat.Items.Add(ex.Message));
            }
            finally
            {
                serverObject.RemoveConnetion(Id);
            }
        }

        protected internal void Close()
        {
            client?.Close();
            Writer?.Close();
            Reader?.Close();
        }
    }

}


