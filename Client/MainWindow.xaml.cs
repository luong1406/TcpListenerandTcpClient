using Sever;
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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _networkStream;
        private ConnectionStatistics _statistics = new ConnectionStatistics();

        public MainWindow()
        {
            InitializeComponent();
            LoadConnectionSettings();
        }

        private void LoadConnectionSettings()
        {
            txtIpAddress.Text = Properties.Settings.Default.IpAddress;
            txtPort.Text = Properties.Settings.Default.Port.ToString();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (IsValidIpAddress(txtIpAddress.Text) && int.TryParse(txtPort.Text, out int port))
            {
                SaveConnectionSettings();

                string ipAddress = txtIpAddress.Text;

                try
                {
                    _client = new TcpClient(ipAddress, port);
                    _networkStream = _client.GetStream();
                    AppendMessage($"Connected to server at {ipAddress}:{port}");

                    Task.Run(() => ListenForMessages());

                    btnConnect.IsEnabled = false;
                    btnDisconnect.IsEnabled = true;
                    btnSendMessage.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    AppendMessage($"Error: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid IP address and port.", "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool IsValidIpAddress(string ipAddress)
        {
            return IPAddress.TryParse(ipAddress, out _);
        }



        private void SaveConnectionSettings()
        {
            Properties.Settings.Default.IpAddress = txtIpAddress.Text;
            Properties.Settings.Default.Port = int.Parse(txtPort.Text);
            Properties.Settings.Default.Save();
        }

        private async Task ListenForMessages()
        {
            try
            {

                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    _statistics.TotalIncomingData += bytesRead;
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    AppendMessage($"Received from server: {message}");
                }
            }
            catch (Exception ex)
            {
                //AppendMessage($"Error: {ex.Message}");
            }
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            DisconnectClient();
        
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = false;
            btnSendMessage.IsEnabled = false;
        }

        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            string message = txtMessageToSend.Text;
            SendMessage(message);
        }

        private void SendMessage(string message)
        {
            try
            {
                if (_networkStream != null && _networkStream.CanWrite)
                {
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    _networkStream.Write(data, 0, data.Length);
                    _statistics.TotalOutgoingData += data.Length;
                    AppendMessage($"Sent: {message}");
                }
            }
            catch (Exception ex)
            {
                AppendMessage($"Error: {ex.Message}");
            }
        }
      

        private void DisconnectClient()
        {
            if (_networkStream != null)
            {
                _networkStream.Close();
            }

            if (_client != null)
            {
                _client.Close();
            }
            
            AppendMessage("Disconnected from server.");
        }

        private void AppendMessage(string message)
        {
            Dispatcher.Invoke(() => txtMessages.Text += $"{message}\n");
        }
        private async void btnSendFile_Click(object sender, RoutedEventArgs e)
        {
            // Open file dialog to select a file
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                string filePath = dlg.FileName;
                await SendFileAsync(filePath);
            }
        }

        private async Task SendFileAsync(string filePath)
        {
            try
            {
                if (_networkStream != null && _networkStream.CanWrite)
                {
                    // Send file name with "FILE:" prefix
                    string fileName = Path.GetFileName(filePath);
                    string header = "FILE:" + fileName + "<EOF>";
                    byte[] fileNameBytes = Encoding.UTF8.GetBytes(header);
                    await _networkStream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length);
                    await _networkStream.FlushAsync();

                    // Send the actual file content
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await _networkStream.WriteAsync(buffer, 0, bytesRead);
                        }
                    }

                    AppendMessage("File sent to the server.");
                }
            }
            catch (Exception ex)
            {
                AppendMessage($"Error: {ex.Message}");
            }
        }

    }
}
