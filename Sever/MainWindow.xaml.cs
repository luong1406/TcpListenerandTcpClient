using NLog;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace Sever
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpListener _server;
        private bool _isServerRunning = false;
        private List<TcpClient> _clients = new List<TcpClient>();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private ConnectionStatistics _statistics = new ConnectionStatistics();

        public MainWindow()
        {
            InitializeComponent();
            StartStatisticsUpdate();
        }

        private void btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = txtIpAddress.Text;
            int port = int.Parse(txtPort.Text);

            Task.Run(() => StartServer(ipAddress, port));

            btnStartServer.IsEnabled = false;
            btnStopServer.IsEnabled = true;
            btnSendMessage.IsEnabled = true;
        }

        private async Task StartServer(string ipAddress, int port)
        {
            try
            {
                _server = new TcpListener(IPAddress.Parse(ipAddress), port);
                _server.Start();
                _isServerRunning = true;
                AppendMessage($"Server started on {ipAddress}:{port}");
                Logger.Info($"Server started on {ipAddress}:{port}");
                while (_isServerRunning)
                {
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    _clients.Add(client);
                    AppendMessage("Client connected.");
                    Logger.Info("Client connected.");
                    _ = Task.Run(() => HandleClientAsync(client));
                }
            }
            catch (Exception ex)
            {
                AppendMessage($"Error: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (NetworkStream networkStream = client.GetStream())
                {
                    _statistics.IncomingConnections++;
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        if (receivedData.StartsWith("FILE:"))
                        {
                            // File transfer detected
                            string fileName = receivedData.Substring(5).Replace("<EOF>", ""); // Remove "FILE:" prefix and EOF
                            await ReceiveFileAsync(networkStream, fileName);
                            AppendMessage($"File {fileName} received from client.");
                            Logger.Info($"File {fileName} received from client.");
                        }
                        else
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            AppendMessage($"Received: {message}");
                            Logger.Info($"Received: {message}");

                            // Echo the message back to the client 
                            await networkStream.WriteAsync(buffer, 0, bytesRead);
                            _statistics.TotalOutgoingData += bytesRead;
                        }
                    }
                }
                _statistics.OutgoingConnections++;
                _clients.Remove(client);
                AppendMessage("Client disconnected.");
                Logger.Info("Client disconnected.");
            }
            catch (Exception ex)
            {
                AppendMessage($"Error: {ex.Message}");
                Logger.Error(ex, "Error handling client.");
            }
          
        }

        private async Task ReceiveFileAsync(NetworkStream networkStream, string fileName)
        {
            try
            {
                string filePath = Path.Combine("ReceivedFiles", fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                    }
                }
                Logger.Info($"File saved as {filePath}");
                AppendMessage($"File saved as {filePath}");
            }
            catch (Exception ex)
            {
                AppendMessage($"Error receiving file: {ex.Message}");
                Logger.Error(ex, "Error receiving file.");
            }
        }

        private void btnStopServer_Click(object sender, RoutedEventArgs e)
        {
            StopServer();

            btnStartServer.IsEnabled = true;
            btnStopServer.IsEnabled = false;
            btnSendMessage.IsEnabled = false;
        }

        private void StopServer()
        {
            if (_server != null)
            {
                _isServerRunning = false;
                _server.Stop();
                Logger.Info("Server stopped.");
                AppendMessage("Server stopped.");
            }
        }

        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            SendMessage(txtMessageToSend.Text);
        }

        private void SendMessage(string message)
        {
            try
            {
                foreach (var client in _clients)
                {
                    if (client.GetStream().CanWrite)
                    {
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        client.GetStream().Write(data, 0, data.Length);
                    }
                }
                Logger.Info($"Sent to clients: {message}");
                AppendMessage($"Sent to clients: {message}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error sending message.");
                AppendMessage($"Error: {ex.Message}");
            }
        }

        private void AppendMessage(string message)
        {
            Dispatcher.Invoke(() => txtMessages.Text += $"{message}\n");
        }
        private void UpdateStatistics()
        {
            Dispatcher.Invoke(() =>
            {
                txtStatistics.Text = $"Incoming Connections: {_statistics.IncomingConnections}\n" +
                                     $"Outgoing Connections: {_statistics.OutgoingConnections}\n" +                                   
                                     $"Total Incoming Data: {_statistics.TotalOutgoingData} bytes\n" +
                                     $"Server Uptime: {_statistics.Duration.ToString(@"hh\:mm\:ss")}";
            });
        }

        // Call this method periodically using a timer
        private void StartStatisticsUpdate()
        {
            var timer = new System.Timers.Timer(100);
            timer.Elapsed += (sender, e) => UpdateStatistics();
            timer.Start();
        }


    }
}
