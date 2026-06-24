using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CaroServer
{
    public class GameRoom
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public List<Socket> Players { get; } = new();
        public Dictionary<Socket, int> PlayerRoles { get; } = new(); // Socket -> 1: P1, 2: P2, 0: Spectator
        public int[,] Board { get; set; }
        public int BoardSize { get; } = 15;
        public int CurrentTurn { get; set; } = 1;
        public string GameMode { get; set; } = "Classic"; // Classic, Timed, BO3
        public bool IsGameStarted { get; set; } = false;
        public int Score1 { get; set; } = 0;
        public int Score2 { get; set; } = 0;
        public CancellationTokenSource? TimerCts { get; set; }

        public GameRoom(int roomId, string roomName)
        {
            RoomId = roomId;
            RoomName = roomName;
            Board = new int[BoardSize, BoardSize];
        }

        public void ResetGameBoard()
        {
            Board = new int[BoardSize, BoardSize];
            CurrentTurn = 1;
        }

        public void ResetRoomState()
        {
            ResetGameBoard();
            IsGameStarted = false;
            Score1 = 0;
            Score2 = 0;
            TimerCts?.Cancel();
        }

        public bool CheckWin(int row, int col, int player)
        {
            int[,] directions = { { 0, 1 }, { 1, 0 }, { 1, 1 }, { 1, -1 } };

            for (int i = 0; i < 4; i++)
            {
                int dr = directions[i, 0];
                int dc = directions[i, 1];
                int count = 1;

                int r = row + dr;
                int c = col + dc;
                while (r >= 0 && r < BoardSize && c >= 0 && c < BoardSize && Board[r, c] == player)
                {
                    count++;
                    r += dr;
                    c += dc;
                }

                r = row - dr;
                c = col - dc;
                while (r >= 0 && r < BoardSize && c >= 0 && c < BoardSize && Board[r, c] == player)
                {
                    count++;
                    r -= dr;
                    c -= dc;
                }

                if (count >= 5) return true;
            }

            return false;
        }

        public bool IsBoardFull()
        {
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (Board[r, c] == 0) return false;
                }
            }
            return true;
        }
    }

    public class ChatServer
    {
        private readonly int _port;
        private Socket? _serverSocket;
        private readonly List<Socket> _clientSockets = new();
        private readonly Dictionary<Socket, SemaphoreSlim> _clientSemaphores = new();
        private readonly Dictionary<Socket, string> _clientNames = new();
        private readonly Dictionary<Socket, int> _clientRooms = new(); // Socket -> RoomId (-1: Lobby)
        private readonly Dictionary<int, GameRoom> _activeRooms = new();
        private int _nextRoomId = 1;
        private readonly object _lock = new();

        public ChatServer(int port)
        {
            _port = port;
        }

        public async Task StartAsync()
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _port));
            _serverSocket.Listen(20);

            Console.WriteLine($"Caro Server (.NET 10) Lobby đang lắng nghe ở cổng {_port}...");

            while (true)
            {
                try
                {
                    Socket clientSocket = await _serverSocket.AcceptAsync();

                    lock (_lock)
                    {
                        _clientSockets.Add(clientSocket);
                        _clientSemaphores[clientSocket] = new SemaphoreSlim(1, 1);
                        _clientRooms[clientSocket] = -1; // Mặc định ở Lobby

                        // Đặt tên mặc định dựa trên cổng kết nối
                        string endpointStr = clientSocket.RemoteEndPoint?.ToString() ?? "";
                        string portStr = endpointStr.Split(':').Last();
                        _clientNames[clientSocket] = $"User_{portStr}";

                        Console.WriteLine($"[+] {clientSocket.RemoteEndPoint} kết nối. Gán tên: {_clientNames[clientSocket]}");

                        // Đồng bộ tên của chính họ và danh sách phòng
                        _ = SendToClientAsync(clientSocket, MessageProtocol.CreateUser(_clientNames[clientSocket]));
                        _ = SendToClientAsync(clientSocket, MessageProtocol.CreateRoomList(GetRoomListString()));

                        // Broadcast chat chào mừng ở Lobby
                        _ = BroadcastToLobbyAsync(MessageProtocol.CreateLobbyChat($"[Hệ thống] {_clientNames[clientSocket]} đã tham gia sảnh chờ."), null);
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
            StringBuilder messageBuffer = new StringBuilder();

            try
            {
                while (true)
                {
                    int receivedBytes = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (receivedBytes == 0) break;

                    string chunk = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    messageBuffer.Append(chunk);

                    string tempBuffer = messageBuffer.ToString();
                    int newlineIndex;

                    while ((newlineIndex = tempBuffer.IndexOf('\n')) >= 0)
                    {
                        string message = tempBuffer.Substring(0, newlineIndex).Trim('\r', '\n');
                        tempBuffer = tempBuffer.Substring(newlineIndex + 1);

                        messageBuffer.Clear();
                        messageBuffer.Append(tempBuffer);

                        if (!string.IsNullOrWhiteSpace(message))
                        {
                            await ProcessClientMessageAsync(clientSocket, message);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Bỏ qua ngoại lệ đóng socket đột ngột
            }
            finally
            {
                await HandleClientDisconnectAsync(clientSocket);
            }
        }

        private async Task HandleClientDisconnectAsync(Socket clientSocket)
        {
            string username = "";
            int currentRoomId = -1;

            lock (_lock)
            {
                _clientSockets.Remove(clientSocket);
                if (_clientNames.TryGetValue(clientSocket, out var name))
                {
                    username = name;
                    _clientNames.Remove(clientSocket);
                }
                if (_clientRooms.TryGetValue(clientSocket, out var rId))
                {
                    currentRoomId = rId;
                    _clientRooms.Remove(clientSocket);
                }
                if (_clientSemaphores.TryGetValue(clientSocket, out var sem))
                {
                    _clientSemaphores.Remove(clientSocket);
                    sem.Dispose();
                }

                Console.WriteLine($"[-] Connection closed: {clientSocket.RemoteEndPoint} ({username})");
            }

            if (currentRoomId != -1)
            {
                await HandleLeaveRoomAsync(clientSocket, currentRoomId, username);
            }
            else
            {
                await BroadcastToLobbyAsync(MessageProtocol.CreateLobbyChat($"[Hệ thống] {username} đã thoát sảnh chờ."), null);
            }

            clientSocket.Close();
        }

        private async Task ProcessClientMessageAsync(Socket clientSocket, string message)
        {
            ParsedMessage parsed = MessageProtocol.Parse(message);
            int roomId = -1;
            string username = "";

            lock (_lock)
            {
                _clientRooms.TryGetValue(clientSocket, out roomId);
                _clientNames.TryGetValue(clientSocket, out var name);
                username = name ?? "User";
            }

            if (roomId == -1)
            {
                // XỬ LÝ LỆNH Ở SẢNH CHỜ (LOBBY)
                switch (parsed.Type)
                {
                    case MessageType.User:
                        string newName = parsed.Extra.Trim();
                        if (!string.IsNullOrEmpty(newName))
                        {
                            string oldName = "";
                            lock (_lock)
                            {
                                oldName = _clientNames[clientSocket];
                                _clientNames[clientSocket] = newName;
                            }
                            await SendToClientAsync(clientSocket, MessageProtocol.CreateUser(newName));
                            await BroadcastToLobbyAsync(MessageProtocol.CreateLobbyChat($"[Hệ thống] {oldName} đã đổi tên thành {newName}."), null);
                        }
                        break;

                    case MessageType.LobbyChat:
                        string chatMsg = MessageProtocol.CreateLobbyChat($"{username}: {parsed.Message}");
                        await BroadcastToLobbyAsync(chatMsg, null);
                        break;

                    case MessageType.CreateRoom:
                        string roomName = string.IsNullOrWhiteSpace(parsed.Extra) ? $"{username}'s Room" : parsed.Extra;
                        int newRoomId = 0;
                        lock (_lock)
                        {
                            newRoomId = _nextRoomId++;
                            GameRoom room = new GameRoom(newRoomId, roomName);
                            room.Players.Add(clientSocket);
                            room.PlayerRoles[clientSocket] = 1; // P1

                            _activeRooms[newRoomId] = room;
                            _clientRooms[clientSocket] = newRoomId;
                        }

                        await SendToClientAsync(clientSocket, MessageProtocol.CreateRoomInfo(newRoomId, roomName));
                        await SendToClientAsync(clientSocket, MessageProtocol.CreateRole(1));
                        await SendToClientAsync(clientSocket, MessageProtocol.CreateRoomPlayers(username, "Chờ đối thủ..."));
                        await BroadcastToLobbyAsync(MessageProtocol.CreateRoomList(GetRoomListString()), null);
                        await BroadcastToLobbyAsync(MessageProtocol.CreateLobbyChat($"[Hệ thống] Phòng chơi '{roomName}' (ID: {newRoomId}) đã được tạo bởi {username}."), null);
                        break;

                    case MessageType.JoinRoom:
                        if (int.TryParse(parsed.Extra, out int targetRoomId))
                        {
                            GameRoom? room = null;
                            lock (_lock)
                            {
                                _activeRooms.TryGetValue(targetRoomId, out room);
                            }

                            if (room != null)
                            {
                                int assignedRole = 0;
                                lock (_lock)
                                {
                                    room.Players.Add(clientSocket);
                                    _clientRooms[clientSocket] = targetRoomId;

                                    // Gán vai trò
                                    bool hasP1 = room.PlayerRoles.ContainsValue(1);
                                    bool hasP2 = room.PlayerRoles.ContainsValue(2);

                                    if (!hasP1)
                                    {
                                        assignedRole = 1;
                                    }
                                    else if (!hasP2)
                                    {
                                        assignedRole = 2;
                                    }
                                    else
                                    {
                                        assignedRole = 0; // Spectator
                                    }
                                    room.PlayerRoles[clientSocket] = assignedRole;
                                }

                                string roleStr = assignedRole == 1 ? "Player 1 (X)" : (assignedRole == 2 ? "Player 2 (O)" : "Khán giả");

                                await SendToClientAsync(clientSocket, MessageProtocol.CreateRoomInfo(targetRoomId, room.RoomName));
                                await SendToClientAsync(clientSocket, MessageProtocol.CreateRole(assignedRole));
                                await SendToClientAsync(clientSocket, MessageProtocol.CreateMode(room.GameMode));

                                // Gửi danh sách người chơi trong phòng
                                await SyncRoomPlayersAsync(room);

                                if (room.GameMode == "BO3")
                                {
                                    await SendToClientAsync(clientSocket, MessageProtocol.CreateScore(room.Score1, room.Score2));
                                }

                                // Gửi nước đi hiện tại nếu đang trong trận
                                if (room.IsGameStarted)
                                {
                                    await SendToClientAsync(clientSocket, MessageProtocol.CreateStart());
                                    // Đồng bộ lại toàn bộ nước đi trên bàn cờ cho Spectator
                                    for (int r = 0; r < room.BoardSize; r++)
                                    {
                                        for (int c = 0; c < room.BoardSize; c++)
                                        {
                                            if (room.Board[r, c] != 0)
                                            {
                                                // Tạm thời giả lập từng nước đi để client vẽ lại
                                                // Nhưng để đơn giản, chỉ cần gửi nước đi hợp lệ
                                                // Gói tin MOVE gửi đi dạng MOVE|r|c tương đương với nước đi vừa đi
                                                // Nhãn _currentTurn sẽ tự động cập nhật nếu ta đồng bộ đúng thứ tự
                                                // Ở đây ta có thể gửi riêng hoặc client vẽ lại. Tuy nhiên, cách tốt nhất
                                                // là gửi danh sách nước đi. Để đơn giản cho bài tập lớn:
                                                // Khi spectator vào, ta tạm gửi các nước đi hiện tại
                                                // Lưu ý: do _currentTurn đổi liên tục, ta gửi luân phiên
                                                // Để giữ thiết kế đơn giản, ta chỉ gửi nước đi cho người mới vào
                                                await SendToClientAsync(clientSocket, MessageProtocol.CreateMove(r, c));
                                            }
                                        }
                                    }
                                }

                                await BroadcastToRoomAsync(room, MessageProtocol.CreateChat($"[Hệ thống] {username} đã tham gia phòng với vai trò {roleStr}."), null);
                                await BroadcastToLobbyAsync(MessageProtocol.CreateRoomList(GetRoomListString()), null);
                            }
                            else
                            {
                                await SendToClientAsync(clientSocket, MessageProtocol.CreateLobbyChat("[Hệ thống] Phòng chơi này không còn tồn tại."));
                            }
                        }
                        break;

                    case MessageType.Exit:
                        _ = SendToClientAsync(clientSocket, MessageProtocol.CreateExit());
                        break;
                }
            }
            else
            {
                // XỬ LÝ LỆNH TRONG PHÒNG CHƠI (ROOM)
                GameRoom? room = null;
                lock (_lock)
                {
                    _activeRooms.TryGetValue(roomId, out room);
                }

                if (room == null) return;

                switch (parsed.Type)
                {
                    case MessageType.Move:
                        if (!room.IsGameStarted)
                        {
                            await SendToClientAsync(clientSocket, MessageProtocol.CreateChat("[Hệ thống] Trận đấu chưa bắt đầu!"));
                            return;
                        }
                        await ProcessRoomMoveAsync(room, clientSocket, parsed);
                        break;

                    case MessageType.Chat:
                        string roleTag = "";
                        lock (_lock)
                        {
                            if (room.PlayerRoles.TryGetValue(clientSocket, out int r))
                            {
                                roleTag = r == 1 ? "[X]" : (r == 2 ? "[O]" : "[Khán giả]");
                            }
                        }
                        string formattedMsg = MessageProtocol.CreateChat($"{roleTag} {username}: {parsed.Message}");
                        await BroadcastToRoomAsync(room, formattedMsg, null);
                        break;

                    case MessageType.Mode:
                        bool isRoomHost = false;
                        lock (_lock)
                        {
                            isRoomHost = room.PlayerRoles.TryGetValue(clientSocket, out int r) && r == 1 && !room.IsGameStarted;
                        }

                        if (isRoomHost)
                        {
                            lock (_lock)
                            {
                                room.GameMode = parsed.Extra;
                            }
                            await BroadcastToRoomAsync(room, MessageProtocol.CreateMode(room.GameMode), null);
                            await BroadcastToRoomAsync(room, MessageProtocol.CreateChat($"[Hệ thống] Chủ phòng đã đổi chế độ chơi thành: {room.GameMode}"), null);
                            await BroadcastToLobbyAsync(MessageProtocol.CreateRoomList(GetRoomListString()), null);
                        }
                        break;

                    case MessageType.Start:
                        bool isP1 = false;
                        bool canStart = false;
                        lock (_lock)
                        {
                            isP1 = room.PlayerRoles.TryGetValue(clientSocket, out int r) && r == 1;
                            // Kiểm tra xem phòng đã có Player 2 kết nối chưa
                            bool hasP2 = room.PlayerRoles.ContainsValue(2);
                            if (hasP2) canStart = true;
                        }

                        if (isP1)
                        {
                            if (canStart)
                            {
                                lock (_lock)
                                {
                                    room.IsGameStarted = true;
                                    room.Score1 = 0;
                                    room.Score2 = 0;
                                    room.ResetGameBoard();
                                }
                                await BroadcastToRoomAsync(room, MessageProtocol.CreateStart(), null);
                                await BroadcastToRoomAsync(room, MessageProtocol.CreateChat($"[Hệ thống] TRẬN ĐẤU BẮT ĐẦU! Chế độ: {room.GameMode}. Lượt đi: Player 1 (X)."), null);

                                if (room.GameMode == "Timed")
                                {
                                    StartRoomTurnTimer(room, 1);
                                }
                                await BroadcastToLobbyAsync(MessageProtocol.CreateRoomList(GetRoomListString()), null);
                            }
                            else
                            {
                                await SendToClientAsync(clientSocket, MessageProtocol.CreateChat("[Hệ thống] Phòng chưa đủ 2 người chơi chính! Vui lòng chờ đối thủ kết nối."));
                            }
                        }
                        break;

                    case MessageType.Reset:
                        bool isPlayerInRoom = false;
                        lock (_lock)
                        {
                            isPlayerInRoom = room.PlayerRoles.TryGetValue(clientSocket, out int r) && (r == 1 || r == 2);
                        }
                        if (isPlayerInRoom)
                        {
                            lock (_lock)
                            {
                                room.ResetRoomState();
                            }
                            await BroadcastToRoomAsync(room, MessageProtocol.CreateReset(), null);
                            await BroadcastToRoomAsync(room, MessageProtocol.CreateChat("[Hệ thống] Trận đấu đã được hủy và làm mới. Chờ Player 1 Start lại."), null);
                            await BroadcastToLobbyAsync(MessageProtocol.CreateRoomList(GetRoomListString()), null);
                        }
                        break;

                    case MessageType.LeaveRoom:
                        lock (_lock)
                        {
                            _clientRooms[clientSocket] = -1;
                        }
                        await HandleLeaveRoomAsync(clientSocket, roomId, username);
                        
                        // Đưa client về lobby và gửi lại danh sách phòng
                        await SendToClientAsync(clientSocket, MessageProtocol.CreateRoomInfo(-1, "Lobby"));
                        await SendToClientAsync(clientSocket, MessageProtocol.CreateRoomList(GetRoomListString()));
                        await BroadcastToLobbyAsync(MessageProtocol.CreateLobbyChat($"[Hệ thống] {username} đã rời phòng chơi quay lại sảnh chờ."), null);
                        break;
                }
            }
        }

        private async Task ProcessRoomMoveAsync(GameRoom room, Socket clientSocket, ParsedMessage parsed)
        {
            int row = parsed.Row;
            int col = parsed.Col;

            lock (_lock)
            {
                if (!room.PlayerRoles.TryGetValue(clientSocket, out int role) || role == 0) return;

                if (role != room.CurrentTurn)
                {
                    _ = SendToClientAsync(clientSocket, MessageProtocol.CreateChat("[Hệ thống] Chưa đến lượt của bạn!"));
                    return;
                }

                if (row < 0 || row >= room.BoardSize || col < 0 || col >= room.BoardSize)
                {
                    _ = SendToClientAsync(clientSocket, MessageProtocol.CreateChat("[Hệ thống] Nước đi nằm ngoài bàn cờ!"));
                    return;
                }

                if (room.Board[row, col] != 0)
                {
                    _ = SendToClientAsync(clientSocket, MessageProtocol.CreateChat("[Hệ thống] Ô cờ này đã có người đánh!"));
                    return;
                }

                room.Board[row, col] = role;
                room.TimerCts?.Cancel();

                _ = BroadcastToRoomAsync(room, MessageProtocol.CreateMove(row, col), null);

                if (room.CheckWin(row, col, role))
                {
                    _ = HandleRoomGameEndAsync(room, role);
                    return;
                }

                if (room.IsBoardFull())
                {
                    _ = HandleRoomGameEndAsync(room, 0); // 0: Hòa
                    return;
                }

                // Đổi lượt
                room.CurrentTurn = room.CurrentTurn == 1 ? 2 : 1;

                if (room.GameMode == "Timed")
                {
                    StartRoomTurnTimer(room, room.CurrentTurn);
                }
            }
        }

        private void StartRoomTurnTimer(GameRoom room, int turnPlayer)
        {
            room.TimerCts?.Cancel();
            room.TimerCts = new CancellationTokenSource();
            var token = room.TimerCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(30000, token);
                    if (!token.IsCancellationRequested)
                    {
                        await HandleRoomTimeoutAsync(room, turnPlayer);
                    }
                }
                catch (TaskCanceledException)
                {
                }
            }, token);
        }

        private async Task HandleRoomTimeoutAsync(GameRoom room, int timedOutPlayer)
        {
            int winner = timedOutPlayer == 1 ? 2 : 1;
            await BroadcastToRoomAsync(room, MessageProtocol.CreateTimeout(), null);
            await BroadcastToRoomAsync(room, MessageProtocol.CreateChat($"[Hệ thống] Người chơi Player {timedOutPlayer} đã hết thời gian đi quân!"), null);

            await HandleRoomGameEndAsync(room, winner);
        }

        private async Task HandleRoomGameEndAsync(GameRoom room, int winner)
        {
            room.TimerCts?.Cancel();

            if (room.GameMode == "BO3")
            {
                if (winner == 1) room.Score1++;
                else if (winner == 2) room.Score2++;

                await BroadcastToRoomAsync(room, MessageProtocol.CreateScore(room.Score1, room.Score2), null);
                await BroadcastToRoomAsync(room, MessageProtocol.CreateChat($"[Hệ thống] Trận đấu kết thúc! Tỷ số BO3 hiện tại: Player 1 [{room.Score1}] - [{room.Score2}] Player 2."), null);

                if (room.Score1 == 2 || room.Score2 == 2)
                {
                    int matchWinner = room.Score1 == 2 ? 1 : 2;
                    await BroadcastToRoomAsync(room, MessageProtocol.CreateMatchWin(matchWinner), null);
                    await BroadcastToRoomAsync(room, MessageProtocol.CreateChat($"🎉 [Hệ thống] Player {matchWinner} ĐÃ THẮNG CHUNG CUỘC TRẬN ĐẤU BO3! 🎉"), null);

                    lock (_lock)
                    {
                        room.IsGameStarted = false;
                        room.Score1 = 0;
                        room.Score2 = 0;
                        room.ResetGameBoard();
                    }
                }
                else
                {
                    lock (_lock)
                    {
                        room.ResetGameBoard();
                    }
                    await BroadcastToRoomAsync(room, MessageProtocol.CreateReset(), null);
                    await BroadcastToRoomAsync(room, MessageProtocol.CreateChat("[Hệ thống] Bàn cờ làm mới. Chuẩn bị ván đấu tiếp theo..."), null);
                }
            }
            else
            {
                if (winner == 0)
                {
                    await BroadcastToRoomAsync(room, MessageProtocol.CreateDraw(), null);
                }
                else
                {
                    foreach (var socket in room.Players)
                    {
                        if (room.PlayerRoles.TryGetValue(socket, out int role))
                        {
                            if (role == winner)
                            {
                                _ = SendToClientAsync(socket, MessageProtocol.CreateWin());
                            }
                            else if (role == 1 || role == 2)
                            {
                                _ = SendToClientAsync(socket, MessageProtocol.CreateLose());
                            }
                        }
                    }
                }

                lock (_lock)
                {
                    room.IsGameStarted = false;
                    room.ResetGameBoard();
                }
                await BroadcastToRoomAsync(room, MessageProtocol.CreateChat("[Hệ thống] Trận đấu kết thúc. Hãy nhấn Start để chơi lại."), null);
            }

            await BroadcastToLobbyAsync(MessageProtocol.CreateRoomList(GetRoomListString()), null);
        }

        private async Task HandleLeaveRoomAsync(Socket clientSocket, int roomId, string username)
        {
            GameRoom? room = null;
            lock (_lock)
            {
                _activeRooms.TryGetValue(roomId, out room);
            }

            if (room == null) return;

            int leavingRole = 0;
            lock (_lock)
            {
                room.Players.Remove(clientSocket);
                if (room.PlayerRoles.TryGetValue(clientSocket, out int r))
                {
                    leavingRole = r;
                    room.PlayerRoles.Remove(clientSocket);
                }
            }

            // Nếu người chơi chính rời phòng, cần làm mới phòng hoặc đóng phòng
            if (leavingRole == 1 || leavingRole == 2)
            {
                lock (_lock)
                {
                    room.ResetRoomState();
                }
                await BroadcastToRoomAsync(room, MessageProtocol.CreateReset(), null);
                await BroadcastToRoomAsync(room, MessageProtocol.CreateChat($"[Hệ thống] Người chơi {username} đã rời phòng. Ván đấu hiện tại bị hủy."), null);

                lock (_lock)
                {
                    // Đẩy một spectator lên làm Player nếu có thể
                    if (room.Players.Count > 0)
                    {
                        // Sắp xếp lại vai trò
                        var currentRole1 = room.PlayerRoles.FirstOrDefault(x => x.Value == 1).Key;
                        var currentRole2 = room.PlayerRoles.FirstOrDefault(x => x.Value == 2).Key;

                        if (currentRole1 == null && currentRole2 != null)
                        {
                            // Đẩy Player 2 lên làm Player 1
                            room.PlayerRoles[currentRole2] = 1;
                            _ = SendToClientAsync(currentRole2, MessageProtocol.CreateRole(1));
                            _ = SendToClientAsync(currentRole2, MessageProtocol.CreateChat("[Hệ thống] Bạn đã được chuyển thành Player 1 (Chủ phòng)."));
                        }

                        // Nếu thiếu người chơi, lấp spectator vào
                        bool hasP1 = room.PlayerRoles.ContainsValue(1);
                        bool hasP2 = room.PlayerRoles.ContainsValue(2);

                        foreach (var socket in room.Players)
                        {
                            if (!room.PlayerRoles.TryGetValue(socket, out int roleVal) || roleVal == 0)
                            {
                                if (!hasP1)
                                {
                                    room.PlayerRoles[socket] = 1;
                                    hasP1 = true;
                                    _ = SendToClientAsync(socket, MessageProtocol.CreateRole(1));
                                    _ = SendToClientAsync(socket, MessageProtocol.CreateChat("[Hệ thống] Bạn đã được chỉ định làm Player 1 (Chủ phòng)."));
                                }
                                else if (!hasP2)
                                {
                                    room.PlayerRoles[socket] = 2;
                                    hasP2 = true;
                                    _ = SendToClientAsync(socket, MessageProtocol.CreateRole(2));
                                    _ = SendToClientAsync(socket, MessageProtocol.CreateChat("[Hệ thống] Bạn đã được chỉ định làm Player 2."));
                                }
                            }
                        }
                    }
                }
            }

            lock (_lock)
            {
                if (room.Players.Count == 0)
                {
                    _activeRooms.Remove(roomId);
                    Console.WriteLine($"[Room] Room {roomId} deleted because it is empty.");
                }
            }

            // Đồng bộ lại tên người chơi trong phòng cho những ai còn lại
            if (_activeRooms.ContainsKey(roomId))
            {
                await SyncRoomPlayersAsync(room);
            }

            await BroadcastToLobbyAsync(MessageProtocol.CreateRoomList(GetRoomListString()), null);
        }

        private async Task SyncRoomPlayersAsync(GameRoom room)
        {
            Socket? p1Socket = null;
            Socket? p2Socket = null;

            lock (_lock)
            {
                p1Socket = room.PlayerRoles.FirstOrDefault(x => x.Value == 1).Key;
                p2Socket = room.PlayerRoles.FirstOrDefault(x => x.Value == 2).Key;
            }

            string p1Name = "Chờ đối thủ...";
            string p2Name = "Chờ đối thủ...";

            lock (_lock)
            {
                if (p1Socket != null && _clientNames.TryGetValue(p1Socket, out var n1)) p1Name = n1;
                if (p2Socket != null && _clientNames.TryGetValue(p2Socket, out var n2)) p2Name = n2;
            }

            await BroadcastToRoomAsync(room, MessageProtocol.CreateRoomPlayers(p1Name, p2Name), null);
        }

        private string GetRoomListString()
        {
            // Định dạng: roomId,roomName,playerCount,status;...
            List<string> list = new();
            lock (_lock)
            {
                foreach (var kvp in _activeRooms)
                {
                    var room = kvp.Value;
                    string status = room.IsGameStarted ? "Playing" : "Waiting";
                    list.Add($"{room.RoomId},{room.RoomName},{room.Players.Count},{status}");
                }
            }
            return string.Join(";", list);
        }

        private async Task BroadcastToLobbyAsync(string message, Socket? senderSocket)
        {
            List<Socket> lobbySockets = new();
            lock (_lock)
            {
                foreach (var socket in _clientSockets)
                {
                    if (_clientRooms.TryGetValue(socket, out int roomId) && roomId == -1)
                    {
                        lobbySockets.Add(socket);
                    }
                }
            }

            foreach (var socket in lobbySockets)
            {
                if (senderSocket == null || socket != senderSocket)
                {
                    await SendToClientAsync(socket, message);
                }
            }
        }

        private async Task BroadcastToRoomAsync(GameRoom room, string message, Socket? senderSocket)
        {
            List<Socket> roomSockets;
            lock (_lock)
            {
                roomSockets = new List<Socket>(room.Players);
            }

            foreach (var socket in roomSockets)
            {
                if (senderSocket == null || socket != senderSocket)
                {
                    await SendToClientAsync(socket, message);
                }
            }
        }

        private async Task SendToClientAsync(Socket clientSocket, string message)
        {
            SemaphoreSlim? semaphore = null;
            lock (_lock)
            {
                if (_clientSemaphores.TryGetValue(clientSocket, out var sem))
                {
                    semaphore = sem;
                }
            }

            if (semaphore == null) return;

            await semaphore.WaitAsync();
            try
            {
                string framedMessage = message.EndsWith("\n") ? message : message + "\n";
                byte[] data = Encoding.UTF8.GetBytes(framedMessage);
                await clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
            }
            catch (Exception)
            {
                // Bỏ qua lỗi gửi
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            ChatServer server = new ChatServer(8080);
            await server.StartAsync();
        }
    }
}
