using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CaroServer
{
    public class ChatServer
    {
        private readonly int _port;
        private Socket _serverSocket;
        private readonly List<Socket> _clientSockets;
        private readonly object _lock;

        // --- CÁC BIẾN QUẢN LÝ GAME (ANTI-CHEAT) ---
        private readonly int _boardSize = 15; // Kích thước bàn cờ Caro 15x15
        private int[,] _board; // Bàn cờ lưu trên Server (0: Trống, 1: Player 1, 2: Player 2)
        private int _currentTurn = 1; // 1: Lượt Player 1 (X), 2: Lượt Player 2 (O)

        // Dictionary để gán vai trò: Socket -> ID Người chơi (1 hoặc 2)
        private readonly Dictionary<Socket, int> _playerRoles;
        private int _playerCount = 0;
        // ------------------------------------------

        public ChatServer(int port)
        {
            _port = port;
            _clientSockets = new List<Socket>();
            _lock = new object();

            // Khởi tạo game state
            _board = new int[_boardSize, _boardSize];
            _playerRoles = new Dictionary<Socket, int>();
        }

        public async Task StartAsync()
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _serverSocket.Listen(10);

            Console.WriteLine($"Caro Server đang khởi chạy và lắng nghe ở cổng {_port}...");

            while (true)
            {
                try
                {
                    Socket clientSocket = await _serverSocket.AcceptAsync();

                    lock (_lock)
                    {
                        _clientSockets.Add(clientSocket);

                        // Gán vai trò cho người kết nối
                        if (_playerCount < 2)
                        {
                            _playerCount++;
                            _playerRoles[clientSocket] = _playerCount;
                            Console.WriteLine($"[+] {clientSocket.RemoteEndPoint} là Player {_playerCount}.");
                            // Gửi thông báo vai trò về cho Client
                            _ = SendToClientAsync(clientSocket, $"SYS_ROLE:{_playerCount}");
                        }
                        else
                        {
                            _playerRoles[clientSocket] = 0; // 0 là khán giả (Spectator)
                            Console.WriteLine($"[+] {clientSocket.RemoteEndPoint} tham gia với tư cách Khán giả.");
                            _ = SendToClientAsync(clientSocket, "SYS_ROLE:0");
                        }
                    }

                    _ = HandleClientAsync(clientSocket);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Lỗi kết nối] {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(Socket clientSocket)
        {
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    int receivedBytes = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (receivedBytes == 0) break;

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes).Trim();
                    Console.WriteLine($"[Log Nhận] {receivedMessage}");

                    // PHÂN TÍCH LỆNH TỪ CLIENT ĐỂ CHỐNG GIAN LẬN
                    if (receivedMessage.StartsWith("MOVE:"))
                    {
                        await ProcessMoveAsync(clientSocket, receivedMessage);
                    }
                    else
                    {
                        // Nếu là tin nhắn chat bình thường, chỉ Broadcast
                        await BroadcastMessageAsync($"CHAT:{receivedMessage}", clientSocket);
                    }
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi ngắt kết nối
            }
            finally
            {
                Console.WriteLine($"[-] {clientSocket.RemoteEndPoint} đã ngắt kết nối.");

                lock (_lock)
                {
                    _clientSockets.Remove(clientSocket);
                    if (_playerRoles.ContainsKey(clientSocket))
                    {
                        int role = _playerRoles[clientSocket];
                        _playerRoles.Remove(clientSocket);

                        // Nếu một trong 2 người chơi thoát, có thể cần reset bàn cờ (Tùy logic game của bạn)
                        if (role == 1 || role == 2)
                        {
                            Console.WriteLine($"[!] Player {role} đã thoát. Cần reset game.");
                            ResetGame();
                            _ = BroadcastMessageAsync("SYS_MSG:Đối thủ đã thoát. Bàn cờ được làm mới.", null);
                        }
                    }
                }
                clientSocket.Close();
            }
        }

        // --- HÀM KIỂM TRA LƯỢT ĐI (ANTI-CHEAT) ---
        private async Task ProcessMoveAsync(Socket clientSocket, string message)
        {
            // Cú pháp kỳ vọng: "MOVE:row:col" (VD: "MOVE:5:7")
            string[] parts = message.Split(':');

            if (parts.Length != 3 || !int.TryParse(parts[1], out int row) || !int.TryParse(parts[2], out int col))
            {
                await SendToClientAsync(clientSocket, "ERROR:Sai cú pháp lệnh đánh cờ.");
                return;
            }

            lock (_lock)
            {
                int playerRole = _playerRoles.ContainsKey(clientSocket) ? _playerRoles[clientSocket] : 0;

                // 1. Kiểm tra vai trò (Khán giả không được đánh)
                if (playerRole != 1 && playerRole != 2)
                {
                    _ = SendToClientAsync(clientSocket, "ERROR:Bạn chỉ là khán giả, không thể đánh cờ.");
                    return;
                }

                // 2. Kiểm tra lượt chơi
                if (playerRole != _currentTurn)
                {
                    _ = SendToClientAsync(clientSocket, "ERROR:Chưa đến lượt của bạn!");
                    return;
                }

                // 3. Kiểm tra ô hợp lệ (Có nằm trong bàn cờ không?)
                if (row < 0 || row >= _boardSize || col < 0 || col >= _boardSize)
                {
                    _ = SendToClientAsync(clientSocket, "ERROR:Vị trí đánh nằm ngoài bàn cờ!");
                    return;
                }

                // 4. Kiểm tra ô đã bị đánh chưa?
                if (_board[row, col] != 0)
                {
                    _ = SendToClientAsync(clientSocket, "ERROR:Ô này đã có người đánh!");
                    return;
                }

                // -> MỌI THỨ HỢP LỆ (BẮT ĐẦU CẬP NHẬT TRẠNG THÁI)
                _board[row, col] = playerRole; // Lưu nước đi vào server
                _currentTurn = _currentTurn == 1 ? 2 : 1; // Đổi lượt chơi

                // Thông báo nước đi hợp lệ cho TẤT CẢ mọi người (bao gồm cả người vừa đánh)
                // Cú pháp: "VALID_MOVE:PlayerID:row:col"
                string validMoveMsg = $"VALID_MOVE:{playerRole}:{row}:{col}";
                _ = BroadcastMessageAsync(validMoveMsg, null); // Truyền null để gửi cho tất cả
            }
        }

        // Hàm hỗ trợ gửi tin nhắn cho 1 Client cụ thể (dùng để báo lỗi invalid move)
        private async Task SendToClientAsync(Socket clientSocket, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
            }
            catch { /* Bỏ qua lỗi nếu không gửi được */ }
        }

        // Phương thức phát lại tin nhắn
        private async Task BroadcastMessageAsync(string message, Socket senderSocket)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            List<Socket> activeClients;

            lock (_lock)
            {
                activeClients = new List<Socket>(_clientSockets);
            }

            foreach (var socket in activeClients)
            {
                // Nếu senderSocket == null, gửi cho TẤT CẢ (dùng cho update trạng thái game)
                if (senderSocket == null || socket != senderSocket)
                {
                    try
                    {
                        await socket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                    }
                    catch { /* Bỏ qua nếu socket đích gặp lỗi */ }
                }
            }
        }

        // Reset bàn cờ
        private void ResetGame()
        {
            _board = new int[_boardSize, _boardSize];
            _currentTurn = 1;
            _playerCount = 0;
            _playerRoles.Clear();

            // Re-assign role cho những người còn trong room
            foreach (var socket in _clientSockets)
            {
                if (_playerCount < 2)
                {
                    _playerCount++;
                    _playerRoles[socket] = _playerCount;
                    _ = SendToClientAsync(socket, $"SYS_ROLE:{_playerCount}");
                }
                else
                {
                    _playerRoles[socket] = 0;
                    _ = SendToClientAsync(socket, "SYS_ROLE:0");
                }
            }
        }
    }
    internal class Server
    {
        static async Task Main(string[] args)
        {
            // Hỗ trợ hiển thị tiếng Việt trên Console
            Console.OutputEncoding = Encoding.UTF8;

            // Khởi tạo server ở cổng 8080
            ChatServer myServer = new ChatServer(8080);

            // Chạy server
            await myServer.StartAsync();
        }
    }
}
