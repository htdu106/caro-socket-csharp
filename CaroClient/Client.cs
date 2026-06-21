using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CaroServer;

namespace CaroClient
{
    public class ClientChat
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly string _userName;
        private Socket _clientSocket;

        // Constructor khởi tạo thông tin kết nối
        public ClientChat(string ipAddress, int port, string userName)
        {
            _ipAddress = ipAddress;
            _port = port;
            // Nếu người dùng không nhập tên, gán tên mặc định
            _userName = string.IsNullOrWhiteSpace(userName) ? "Người dùng ẩn danh" : userName;
        }

        // Bắt đầu quá trình kết nối và chat
        public async Task StartAsync()
        {
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Console.WriteLine($"Đang cố gắng kết nối đến Server ({_ipAddress}:{_port})...");
                await _clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(_ipAddress), _port));
                Console.WriteLine("✅ Kết nối thành công! Bạn có thể bắt đầu chat.\n");

                // Bắt đầu task lắng nghe tin nhắn ngầm
                _ = ReceiveMessagesAsync();

                // Chạy vòng lặp gửi tin nhắn ở luồng chính
                await SendMessagesLoopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lỗi] Không thể kết nối tới Server: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        // Vòng lặp chờ người dùng gõ phím và gửi đi
        private async Task SendMessagesLoopAsync()
        {
            while (true)
            {
                string message = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(message)) continue;
                if (message.ToLower() == "exit") break; // Gõ 'exit' để thoát

                // Gộp tên và tin nhắn
                string fullMessage = $"{_userName}: {message}";
                byte[] data = Encoding.UTF8.GetBytes(fullMessage);
                await _clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
            }
        }

        // Task chạy ngầm để nhận tin nhắn từ Server
        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int receivedBytes = await _clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);

                    // Nhận được 0 byte đồng nghĩa Server ngắt kết nối
                    if (receivedBytes == 0)
                    {
                        Console.WriteLine("\n[Hệ thống] Server đã đóng kết nối.");
                        break;
                    }

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    Console.WriteLine($"\n> {receivedMessage}");
                }
            }
            catch
            {
                Console.WriteLine("\n[Hệ thống] Đã mất kết nối tới máy chủ.");
            }
        }

        // Đóng dọn Socket an toàn
        private void Disconnect()
        {
            if (_clientSocket != null)
            {
                _clientSocket.Close();
                _clientSocket = null;
            }
        }
    }
    internal class Client
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            
            Console.Write("Nhập tên hiển thị của bạn: ");
            string userName = Console.ReadLine();

            // Khởi tạo Client truyền vào IP, Port và Username
            ClientChat myClient = new ClientChat("127.0.0.1", 8080, userName);

            // Bắt đầu chạy Client
            await myClient.StartAsync();
        }
    }
}
