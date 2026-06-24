using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaroServer;

namespace CaroClient
{
    public partial class Form1 : Form
    {
        private int[,] boardState = new int[15, 15];
        private int _myRole = -1; // -1: Chưa kết nối, 1: Player 1, 2: Player 2, 0: Khán giả
        private int _currentTurn = 1; // 1: Player 1, 2: Player 2
        private string _currentGameMode = "Classic"; // Classic, Timed, BO3
        private bool _isGameStarted = false;
        private bool _isConnected = false;

        private int _roomId = -1; // -1: Sảnh chờ (Lobby), >=1: Phòng chơi
        private string _roomName = "Lobby";
        private string _username = "";
        private string _player1Name = "Chờ đối thủ...";
        private string _player2Name = "Chờ đối thủ...";

        private Socket? _clientSocket;
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);

        private System.Windows.Forms.Timer? _clientTimer;
        private int _countdownSeconds = 30;

        private int hoverRow = -1;
        private int hoverCol = -1;

        private readonly List<int> _roomIdsList = new();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ResetLocalBoard();
            UpdateRoleUI();
            UpdateTurnUI();
            TogglePanels();

            pnlBoard.MouseLeave += (s, ev) =>
            {
                hoverRow = -1;
                hoverCol = -1;
                pnlBoard.Invalidate();
            };
        }

        private void TogglePanels()
        {
            if (!_isConnected)
            {
                pnlLobby.Visible = false;
                pnlBoard.Visible = false;
                grpSettings.Visible = false;
                grpChat.Visible = false;
            }
            else if (_roomId == -1)
            {
                // Sảnh chờ
                pnlLobby.Visible = true;
                pnlBoard.Visible = false;
                grpSettings.Visible = false;
                grpChat.Visible = false;
            }
            else
            {
                // Trong phòng chơi
                pnlLobby.Visible = false;
                pnlBoard.Visible = true;
                grpSettings.Visible = true;
                grpChat.Visible = true;
            }
        }

        private async Task SendPacketAsync(string packet)
        {
            if (!_isConnected || _clientSocket == null || !_clientSocket.Connected) return;

            await _sendSemaphore.WaitAsync();
            try
            {
                string framedPacket = packet.EndsWith("\n") ? packet : packet + "\n";
                byte[] data = Encoding.UTF8.GetBytes(framedPacket);
                await _clientSocket.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
            }
            catch (Exception ex)
            {
                AppendLobbyChat($"[Lỗi gửi] Không thể gửi: {ex.Message}");
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            byte[] buffer = new byte[1024];
            StringBuilder messageBuffer = new StringBuilder();

            try
            {
                while (_isConnected && _clientSocket != null)
                {
                    int receivedBytes = await _clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (receivedBytes == 0)
                    {
                        this.BeginInvoke(new Action(() => OnDisconnect("[Hệ thống] Máy chủ đóng kết nối.")));
                        break;
                    }

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
                            this.BeginInvoke(new Action(() => ProcessServerMessage(message)));
                        }
                    }
                }
            }
            catch (Exception)
            {
                this.BeginInvoke(new Action(() => OnDisconnect("[Hệ thống] Mất kết nối tới máy chủ.")));
            }
        }

        private void ProcessServerMessage(string message)
        {
            ParsedMessage parsed = MessageProtocol.Parse(message);

            switch (parsed.Type)
            {
                case MessageType.User:
                    _username = parsed.Extra;
                    txtUsername.Text = _username;
                    AppendLobbyChat($"[Hệ thống] Biệt danh của bạn đã đồng bộ là: {_username}");
                    break;

                case MessageType.RoomList:
                    UpdateRoomListUI(parsed.Extra);
                    break;

                case MessageType.RoomInfo:
                    if (int.TryParse(parsed.Extra, out int rId))
                    {
                        _roomId = rId;
                        _roomName = parsed.Message;
                        if (_roomId != -1)
                        {
                            rtbChat.Clear();
                            AppendChat($"[Phòng] Đã vào phòng '{_roomName}' (ID: {_roomId})");
                        }
                        TogglePanels();
                    }
                    break;

                case MessageType.RoomPlayers:
                    _player1Name = parsed.Extra;
                    _player2Name = parsed.Message;
                    AppendChat($"[Hệ thống] Đối đầu: {_player1Name} (X) VS {_player2Name} (O)");
                    UpdateTurnUI();
                    break;

                case MessageType.Role:
                    if (int.TryParse(parsed.Extra, out int roleVal))
                    {
                        _myRole = roleVal;
                        UpdateRoleUI();
                        UpdateTurnUI();
                    }
                    break;

                case MessageType.Mode:
                    _currentGameMode = parsed.Extra;
                    cmbMode.SelectedItem = _currentGameMode;
                    lblScore.Visible = (_currentGameMode == "BO3");
                    break;

                case MessageType.Start:
                    _isGameStarted = true;
                    ResetLocalBoard();
                    AppendChat("[Hệ thống] Trận đấu bắt đầu!");
                    UpdateTurnUI();
                    StartClientTimer();
                    
                    if (_myRole == 1)
                    {
                        cmbMode.Enabled = false;
                        btnStart.Enabled = false;
                    }
                    break;

                case MessageType.Move:
                    boardState[parsed.Row, parsed.Col] = _currentTurn;
                    _currentTurn = _currentTurn == 1 ? 2 : 1;
                    pnlBoard.Invalidate();
                    UpdateTurnUI();
                    StartClientTimer();
                    break;

                case MessageType.Chat:
                    AppendChat(parsed.Message);
                    break;

                case MessageType.LobbyChat:
                    AppendLobbyChat(parsed.Message);
                    break;

                case MessageType.Reset:
                    _isGameStarted = false;
                    ResetLocalBoard();
                    StopClientTimer();
                    UpdateRoleUI();
                    UpdateTurnUI();
                    lblScore.Visible = (_currentGameMode == "BO3");
                    AppendChat("[Hệ thống] Ván đấu được làm mới. Chờ chủ phòng bắt đầu ván mới.");
                    break;

                case MessageType.Timeout:
                    StopClientTimer();
                    AppendChat("[Hệ thống] Đã hết giờ lượt đi!");
                    break;

                case MessageType.Win:
                    StopClientTimer();
                    MessageBox.Show("Chúc mừng! Bạn đã giành chiến thắng! 🎉", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _isGameStarted = false;
                    UpdateRoleUI();
                    break;

                case MessageType.Lose:
                    StopClientTimer();
                    MessageBox.Show("Rất tiếc! Bạn đã thua cuộc ván đấu này. 💀", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _isGameStarted = false;
                    UpdateRoleUI();
                    break;

                case MessageType.Draw:
                    StopClientTimer();
                    MessageBox.Show("Trận đấu kết thúc với kết quả Hòa cờ! 🤝", "Kết quả", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _isGameStarted = false;
                    UpdateRoleUI();
                    break;

                case MessageType.Score:
                    lblScore.Text = $"Tỷ số: {parsed.Score1} - {parsed.Score2}";
                    break;

                case MessageType.MatchWin:
                    StopClientTimer();
                    string winner = parsed.Extra == "1" ? _player1Name : _player2Name;
                    MessageBox.Show($"Chúc mừng! {winner} đã thắng chung cuộc trận đấu BO3! 🏆", "Kết quả chung cuộc BO3", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    _isGameStarted = false;
                    UpdateRoleUI();
                    break;

                case MessageType.Exit:
                    OnDisconnect("[Hệ thống] Máy chủ yêu cầu ngắt kết nối.");
                    break;
            }
        }

        private void UpdateRoomListUI(string roomData)
        {
            lstRooms.Items.Clear();
            _roomIdsList.Clear();

            if (string.IsNullOrWhiteSpace(roomData)) return;

            // Dữ liệu: ID,TênPhòng,SốNgười,TrạngThái;...
            string[] rooms = roomData.Split(';');
            foreach (var room in rooms)
            {
                if (string.IsNullOrWhiteSpace(room)) continue;
                string[] fields = room.Split(',');
                if (fields.Length >= 4)
                {
                    if (int.TryParse(fields[0], out int id))
                    {
                        string name = fields[1];
                        string count = fields[2];
                        string status = fields[3] == "Playing" ? "Đang chơi" : "Đang chờ";

                        lstRooms.Items.Add($"ID: {id} | Phòng: {name} ({count} người) [{status}]");
                        _roomIdsList.Add(id);
                    }
                }
            }
        }

        private void StartClientTimer()
        {
            StopClientTimer();

            if (_currentGameMode != "Timed" || !_isGameStarted)
            {
                pbCountdown.Visible = false;
                lblTimer.Visible = false;
                return;
            }

            pbCountdown.Visible = true;
            lblTimer.Visible = true;
            _countdownSeconds = 30;
            pbCountdown.Value = 30;
            lblTimer.Text = $"Thời gian: 30s";

            _clientTimer = new System.Windows.Forms.Timer();
            _clientTimer.Interval = 1000;
            _clientTimer.Tick += (s, e) =>
            {
                _countdownSeconds--;
                if (_countdownSeconds < 0) _countdownSeconds = 0;

                pbCountdown.Value = _countdownSeconds;
                lblTimer.Text = $"Thời gian: {_countdownSeconds}s";

                if (_countdownSeconds == 0)
                {
                    _clientTimer.Stop();
                }
            };
            _clientTimer.Start();
        }

        private void StopClientTimer()
        {
            if (_clientTimer != null)
            {
                _clientTimer.Stop();
                _clientTimer.Dispose();
                _clientTimer = null;
            }
            pbCountdown.Visible = false;
            lblTimer.Visible = false;
        }

        private void UpdateRoleUI()
        {
            if (_myRole == 1)
            {
                lblRole.Text = $"Vai trò: Player 1 (X) - {(_isGameStarted ? "Đang đấu" : "Chủ phòng")}";
                lblRole.ForeColor = Color.Cyan;
                cmbMode.Enabled = !_isGameStarted;
                btnStart.Enabled = !_isGameStarted;
            }
            else if (_myRole == 2)
            {
                lblRole.Text = "Vai trò: Player 2 (O)";
                lblRole.ForeColor = Color.Coral;
                cmbMode.Enabled = false;
                btnStart.Enabled = false;
            }
            else if (_myRole == 0)
            {
                lblRole.Text = "Vai trò: Khán giả (Spectator)";
                lblRole.ForeColor = Color.Gray;
                cmbMode.Enabled = false;
                btnStart.Enabled = false;
            }
            else
            {
                lblRole.Text = "Vai trò: Chưa kết nối";
                lblRole.ForeColor = Color.Gold;
                cmbMode.Enabled = false;
                btnStart.Enabled = false;
            }
        }

        private void UpdateTurnUI()
        {
            if (!_isGameStarted)
            {
                lblTurn.Text = "Lượt đi: Chờ bắt đầu...";
                lblTurn.ForeColor = Color.Yellow;
                return;
            }

            string currentName = _currentTurn == 1 ? _player1Name : _player2Name;

            if (_currentTurn == _myRole)
            {
                lblTurn.Text = $"Lượt đi: LƯỢT CỦA BẠN ({currentName})";
                lblTurn.ForeColor = Color.Lime;
            }
            else
            {
                lblTurn.Text = $"Lượt đi: {currentName} ({(_currentTurn == 1 ? "X" : "O")})";
                lblTurn.ForeColor = Color.Tomato;
            }
        }

        private void ResetLocalBoard()
        {
            boardState = new int[15, 15];
            _currentTurn = 1;
            pnlBoard.Invalidate();
        }

        private void AppendChat(string message)
        {
            rtbChat.AppendText(message + Environment.NewLine);
            rtbChat.ScrollToCaret();
        }

        private void AppendLobbyChat(string message)
        {
            rtbLobbyChat.AppendText(message + Environment.NewLine);
            rtbLobbyChat.ScrollToCaret();
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                string ip = txtIP.Text.Trim();
                if (!int.TryParse(txtPort.Text.Trim(), out int port))
                {
                    MessageBox.Show("Port phải là số nguyên hợp lệ!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    btnConnect.Enabled = false;
                    AppendLobbyChat("[Hệ thống] Đang kết nối tới server...");
                    await _clientSocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port));
                    
                    _isConnected = true;
                    btnConnect.Text = "Ngắt";
                    btnConnect.ForeColor = Color.Red;
                    btnConnect.Enabled = true;
                    
                    txtIP.Enabled = false;
                    txtPort.Enabled = false;

                    _roomId = -1;
                    _roomName = "Lobby";
                    TogglePanels();

                    _ = ReceiveMessagesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kết nối tới Server thất bại: {ex.Message}", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnConnect.Enabled = true;
                    OnDisconnect(null);
                }
            }
            else
            {
                await SendPacketAsync(MessageProtocol.CreateExit());
                OnDisconnect("[Hệ thống] Đã ngắt kết nối.");
            }
        }

        private void OnDisconnect(string? logMessage)
        {
            _isConnected = false;
            _isGameStarted = false;
            _myRole = -1;
            _currentTurn = 1;
            _roomId = -1;
            _roomName = "Lobby";

            ResetLocalBoard();
            StopClientTimer();

            if (_clientSocket != null)
            {
                try { _clientSocket.Close(); } catch { }
                _clientSocket = null;
            }

            btnConnect.Text = "Kết nối";
            btnConnect.ForeColor = Color.Cyan;
            txtIP.Enabled = true;
            txtPort.Enabled = true;
            lblScore.Visible = false;

            rtbLobbyChat.Clear();
            rtbChat.Clear();
            lstRooms.Items.Clear();
            _roomIdsList.Clear();

            UpdateRoleUI();
            UpdateTurnUI();
            TogglePanels();

            if (logMessage != null)
            {
                MessageBox.Show(logMessage, "Thông báo hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (_isConnected && _myRole == 1)
            {
                await SendPacketAsync(MessageProtocol.CreateStart());
            }
        }

        private async void cmbMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isConnected && _myRole == 1 && !_isGameStarted)
            {
                string? mode = cmbMode.SelectedItem?.ToString();
                if (mode != null)
                {
                    await SendPacketAsync(MessageProtocol.CreateMode(mode));
                }
            }
        }

        private async void btnSendChat_Click(object sender, EventArgs e)
        {
            await SendChatText();
        }

        private async void txtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendChatText();
            }
        }

        private async Task SendChatText()
        {
            string txt = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(txt)) return;

            if (_isConnected)
            {
                await SendPacketAsync(MessageProtocol.CreateChat(txt));
                txtChatInput.Clear();
            }
        }

        private async void btnSetUsername_Click(object sender, EventArgs e)
        {
            string newName = txtUsername.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("Biệt danh không được trống!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isConnected)
            {
                await SendPacketAsync(MessageProtocol.CreateUser(newName));
            }
        }

        private async void btnCreateRoom_Click(object sender, EventArgs e)
        {
            string roomNameInput = txtRoomName.Text.Trim();
            if (_isConnected)
            {
                await SendPacketAsync(MessageProtocol.CreateCreateRoom(roomNameInput));
                txtRoomName.Clear();
            }
        }

        private async void btnJoinRoom_Click(object sender, EventArgs e)
        {
            int index = lstRooms.SelectedIndex;
            if (index >= 0 && index < _roomIdsList.Count)
            {
                int targetId = _roomIdsList[index];
                if (_isConnected)
                {
                    await SendPacketAsync(MessageProtocol.CreateJoinRoom(targetId));
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một phòng trong danh sách để vào!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void btnLeaveRoom_Click(object sender, EventArgs e)
        {
            if (_isConnected && _roomId != -1)
            {
                await SendPacketAsync(MessageProtocol.CreateLeaveRoom());
            }
        }

        private async void btnSendLobbyChat_Click(object sender, EventArgs e)
        {
            await SendLobbyChatText();
        }

        private async void txtLobbyChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SendLobbyChatText();
            }
        }

        private async Task SendLobbyChatText()
        {
            string txt = txtLobbyChatInput.Text.Trim();
            if (string.IsNullOrEmpty(txt)) return;

            if (_isConnected)
            {
                await SendPacketAsync(MessageProtocol.CreateLobbyChat(txt));
                txtLobbyChatInput.Clear();
            }
        }

        private void pnlBoard_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int cellSize = 30;

            // 1. Vẽ lưới bàn cờ 15x15
            using (Pen gridPen = new Pen(Color.FromArgb(50, 50, 60), 1))
            {
                for (int i = 0; i <= 15; i++)
                {
                    g.DrawLine(gridPen, 0, i * cellSize, 450, i * cellSize);
                    g.DrawLine(gridPen, i * cellSize, 0, i * cellSize, 450);
                }
            }

            // 2. Vẽ Highlight ô cờ đang di chuột qua (Hover Effect)
            if (hoverRow >= 0 && hoverRow < 15 && hoverCol >= 0 && hoverCol < 15)
            {
                if (boardState[hoverRow, hoverCol] == 0 && _isConnected && _isGameStarted && _currentTurn == _myRole)
                {
                    using (SolidBrush hoverBrush = new SolidBrush(Color.FromArgb(40, 0, 255, 255)))
                    {
                        g.FillRectangle(hoverBrush, hoverCol * cellSize + 1, hoverRow * cellSize + 1, cellSize - 1, cellSize - 1);
                    }
                }
            }

            // 3. Vẽ các quân cờ X và O sắc nét
            using (Pen penX = new Pen(Color.FromArgb(0, 191, 255), 3)) // X - Neon Blue
            using (Pen penO = new Pen(Color.FromArgb(255, 99, 71), 3))  // O - Neon Coral
            {
                for (int r = 0; r < 15; r++)
                {
                    for (int c = 0; c < 15; c++)
                    {
                        int state = boardState[r, c];
                        int padding = 6;
                        if (state == 1) // Player 1 - X
                        {
                            g.DrawLine(penX, c * cellSize + padding, r * cellSize + padding, (c + 1) * cellSize - padding, (r + 1) * cellSize - padding);
                            g.DrawLine(penX, (c + 1) * cellSize - padding, r * cellSize + padding, c * cellSize + padding, (r + 1) * cellSize - padding);
                        }
                        else if (state == 2) // Player 2 - O
                        {
                            g.DrawEllipse(penO, c * cellSize + padding, r * cellSize + padding, cellSize - padding * 2, cellSize - padding * 2);
                        }
                    }
                }
            }
        }

        private void pnlBoard_MouseMove(object sender, MouseEventArgs e)
        {
            int cellSize = 30;
            int col = e.X / cellSize;
            int row = e.Y / cellSize;

            if (row >= 0 && row < 15 && col >= 0 && col < 15)
            {
                if (row != hoverRow || col != hoverCol)
                {
                    hoverRow = row;
                    hoverCol = col;
                    pnlBoard.Invalidate();
                }
            }
            else
            {
                if (hoverRow != -1 || hoverCol != -1)
                {
                    hoverRow = -1;
                    hoverCol = -1;
                    pnlBoard.Invalidate();
                }
            }
        }

        private async void pnlBoard_MouseClick(object sender, MouseEventArgs e)
        {
            if (!_isConnected || !_isGameStarted) return;
            if (_myRole != 1 && _myRole != 2) return;
            if (_currentTurn != _myRole) return;

            int cellSize = 30;
            int col = e.X / cellSize;
            int row = e.Y / cellSize;

            if (row >= 0 && row < 15 && col >= 0 && col < 15)
            {
                if (boardState[row, col] == 0)
                {
                    await SendPacketAsync(MessageProtocol.CreateMove(row, col));
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopClientTimer();
            if (_isConnected)
            {
                _ = SendPacketAsync(MessageProtocol.CreateExit());
            }
        }
    }
}
