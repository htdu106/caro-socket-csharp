namespace CaroClient
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            pnlBoard = new DoubleBufferedPanel();
            grpConnection = new GroupBox();
            lblRole = new Label();
            btnConnect = new Button();
            txtPort = new TextBox();
            lblPort = new Label();
            txtIP = new TextBox();
            lblIP = new Label();
            grpSettings = new GroupBox();
            btnLeaveRoom = new Button();
            lblScore = new Label();
            lblTimer = new Label();
            pbCountdown = new ProgressBar();
            lblTurn = new Label();
            btnStart = new Button();
            cmbMode = new ComboBox();
            lblMode = new Label();
            grpChat = new GroupBox();
            btnSendChat = new Button();
            txtChatInput = new TextBox();
            rtbChat = new RichTextBox();
            pnlLobby = new Panel();
            btnSendLobbyChat = new Button();
            txtLobbyChatInput = new TextBox();
            rtbLobbyChat = new RichTextBox();
            lblLobbyChatTitle = new Label();
            btnJoinRoom = new Button();
            btnCreateRoom = new Button();
            txtRoomName = new TextBox();
            lblCreateRoomTitle = new Label();
            lstRooms = new ListBox();
            lblRoomListTitle = new Label();
            btnSetUsername = new Button();
            txtUsername = new TextBox();
            lblUserTitle = new Label();
            grpConnection.SuspendLayout();
            grpSettings.SuspendLayout();
            grpChat.SuspendLayout();
            pnlLobby.SuspendLayout();
            SuspendLayout();
            // 
            // pnlBoard
            // 
            pnlBoard.BackColor = Color.FromArgb(30, 30, 36);
            pnlBoard.BorderStyle = BorderStyle.FixedSingle;
            pnlBoard.Location = new Point(12, 12);
            pnlBoard.Name = "pnlBoard";
            pnlBoard.Size = new Size(450, 450);
            pnlBoard.TabIndex = 0;
            pnlBoard.Visible = false;
            pnlBoard.Paint += pnlBoard_Paint;
            pnlBoard.MouseClick += pnlBoard_MouseClick;
            pnlBoard.MouseMove += pnlBoard_MouseMove;
            // 
            // grpConnection
            // 
            grpConnection.Controls.Add(lblRole);
            grpConnection.Controls.Add(btnConnect);
            grpConnection.Controls.Add(txtPort);
            grpConnection.Controls.Add(lblPort);
            grpConnection.Controls.Add(txtIP);
            grpConnection.Controls.Add(lblIP);
            grpConnection.ForeColor = Color.White;
            grpConnection.Location = new Point(475, 12);
            grpConnection.Name = "grpConnection";
            grpConnection.Size = new Size(317, 105);
            grpConnection.TabIndex = 1;
            grpConnection.TabStop = false;
            grpConnection.Text = "Kết nối mạng";
            // 
            // lblRole
            // 
            lblRole.AutoSize = true;
            lblRole.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblRole.ForeColor = Color.Gold;
            lblRole.Location = new Point(16, 75);
            lblRole.Name = "lblRole";
            lblRole.Size = new Size(130, 17);
            lblRole.TabIndex = 5;
            lblRole.Text = "Vai trò: Chưa kết nối";
            // 
            // btnConnect
            // 
            btnConnect.BackColor = Color.FromArgb(50, 50, 60);
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.ForeColor = Color.Cyan;
            btnConnect.Location = new Point(207, 23);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(95, 45);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "Kết nối";
            btnConnect.UseVisualStyleBackColor = false;
            btnConnect.Click += btnConnect_Click;
            // 
            // txtPort
            // 
            txtPort.BackColor = Color.FromArgb(40, 40, 48);
            txtPort.BorderStyle = BorderStyle.FixedSingle;
            txtPort.ForeColor = Color.White;
            txtPort.Location = new Point(148, 23);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(50, 23);
            txtPort.TabIndex = 3;
            txtPort.Text = "8080";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(116, 26);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(32, 15);
            lblPort.TabIndex = 2;
            lblPort.Text = "Port:";
            // 
            // txtIP
            // 
            txtIP.BackColor = Color.FromArgb(40, 40, 48);
            txtIP.BorderStyle = BorderStyle.FixedSingle;
            txtIP.ForeColor = Color.White;
            txtIP.Location = new Point(36, 23);
            txtIP.Name = "txtIP";
            txtIP.Size = new Size(74, 23);
            txtIP.TabIndex = 1;
            txtIP.Text = "127.0.0.1";
            // 
            // lblIP
            // 
            lblIP.AutoSize = true;
            lblIP.Location = new Point(14, 26);
            lblIP.Name = "lblIP";
            lblIP.Size = new Size(20, 15);
            lblIP.TabIndex = 0;
            lblIP.Text = "IP:";
            // 
            // grpSettings
            // 
            grpSettings.Controls.Add(btnLeaveRoom);
            grpSettings.Controls.Add(lblScore);
            grpSettings.Controls.Add(lblTimer);
            grpSettings.Controls.Add(pbCountdown);
            grpSettings.Controls.Add(lblTurn);
            grpSettings.Controls.Add(btnStart);
            grpSettings.Controls.Add(cmbMode);
            grpSettings.Controls.Add(lblMode);
            grpSettings.ForeColor = Color.White;
            grpSettings.Location = new Point(475, 123);
            grpSettings.Name = "grpSettings";
            grpSettings.Size = new Size(317, 160);
            grpSettings.TabIndex = 2;
            grpSettings.TabStop = false;
            grpSettings.Text = "Trận đấu";
            grpSettings.Visible = false;
            // 
            // btnLeaveRoom
            // 
            btnLeaveRoom.BackColor = Color.FromArgb(60, 40, 40);
            btnLeaveRoom.FlatStyle = FlatStyle.Flat;
            btnLeaveRoom.ForeColor = Color.Salmon;
            btnLeaveRoom.Location = new Point(207, 123);
            btnLeaveRoom.Name = "btnLeaveRoom";
            btnLeaveRoom.Size = new Size(95, 27);
            btnLeaveRoom.TabIndex = 7;
            btnLeaveRoom.Text = "Thoát phòng";
            btnLeaveRoom.UseVisualStyleBackColor = false;
            btnLeaveRoom.Click += btnLeaveRoom_Click;
            // 
            // lblScore
            // 
            lblScore.AutoSize = true;
            lblScore.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblScore.ForeColor = Color.Yellow;
            lblScore.Location = new Point(18, 126);
            lblScore.Name = "lblScore";
            lblScore.Size = new Size(88, 20);
            lblScore.TabIndex = 6;
            lblScore.Text = "Tỷ số: 0 - 0";
            lblScore.Visible = false;
            // 
            // lblTimer
            // 
            lblTimer.AutoSize = true;
            lblTimer.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTimer.ForeColor = Color.Tomato;
            lblTimer.Location = new Point(207, 96);
            lblTimer.Name = "lblTimer";
            lblTimer.Size = new Size(95, 17);
            lblTimer.TabIndex = 5;
            lblTimer.Text = "Thời gian: 30s";
            lblTimer.Visible = false;
            // 
            // pbCountdown
            // 
            pbCountdown.Location = new Point(18, 98);
            pbCountdown.Maximum = 30;
            pbCountdown.Name = "pbCountdown";
            pbCountdown.Size = new Size(180, 15);
            pbCountdown.Style = ProgressBarStyle.Continuous;
            pbCountdown.TabIndex = 4;
            pbCountdown.Value = 30;
            pbCountdown.Visible = false;
            // 
            // lblTurn
            // 
            lblTurn.AutoSize = true;
            lblTurn.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTurn.ForeColor = Color.LimeGreen;
            lblTurn.Location = new Point(16, 68);
            lblTurn.Name = "lblTurn";
            lblTurn.Size = new Size(71, 17);
            lblTurn.TabIndex = 3;
            lblTurn.Text = "Lượt đi: --";
            // 
            // btnStart
            // 
            btnStart.BackColor = Color.FromArgb(50, 50, 60);
            btnStart.Enabled = false;
            btnStart.FlatStyle = FlatStyle.Flat;
            btnStart.ForeColor = Color.Lime;
            btnStart.Location = new Point(207, 22);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(95, 27);
            btnStart.TabIndex = 2;
            btnStart.Text = "Bắt đầu";
            btnStart.UseVisualStyleBackColor = false;
            btnStart.Click += btnStart_Click;
            // 
            // cmbMode
            // 
            cmbMode.BackColor = Color.FromArgb(40, 40, 48);
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.FlatStyle = FlatStyle.Flat;
            cmbMode.ForeColor = Color.White;
            cmbMode.FormattingEnabled = true;
            cmbMode.Items.AddRange(new object[] { "Classic", "Timed", "BO3" });
            cmbMode.Location = new Point(78, 23);
            cmbMode.Name = "cmbMode";
            cmbMode.Size = new Size(118, 23);
            cmbMode.TabIndex = 1;
            cmbMode.SelectedIndexChanged += cmbMode_SelectedIndexChanged;
            // 
            // lblMode
            // 
            lblMode.AutoSize = true;
            lblMode.Location = new Point(16, 26);
            lblMode.Name = "lblMode";
            lblMode.Size = new Size(54, 15);
            lblMode.TabIndex = 0;
            lblMode.Text = "Chế độ: ";
            // 
            // grpChat
            // 
            grpChat.Controls.Add(btnSendChat);
            grpChat.Controls.Add(txtChatInput);
            grpChat.Controls.Add(rtbChat);
            grpChat.ForeColor = Color.White;
            grpChat.Location = new Point(475, 289);
            grpChat.Name = "grpChat";
            grpChat.Size = new Size(317, 173);
            grpChat.TabIndex = 3;
            grpChat.TabStop = false;
            grpChat.Text = "Phòng chat";
            grpChat.Visible = false;
            // 
            // btnSendChat
            // 
            btnSendChat.BackColor = Color.FromArgb(50, 50, 60);
            btnSendChat.FlatStyle = FlatStyle.Flat;
            btnSendChat.ForeColor = Color.LightSkyBlue;
            btnSendChat.Location = new Point(245, 137);
            btnSendChat.Name = "btnSendChat";
            btnSendChat.Size = new Size(57, 23);
            btnSendChat.TabIndex = 2;
            btnSendChat.Text = "Gửi";
            btnSendChat.UseVisualStyleBackColor = false;
            btnSendChat.Click += btnSendChat_Click;
            // 
            // txtChatInput
            // 
            txtChatInput.BackColor = Color.FromArgb(40, 40, 48);
            txtChatInput.BorderStyle = BorderStyle.FixedSingle;
            txtChatInput.ForeColor = Color.White;
            txtChatInput.Location = new Point(14, 137);
            txtChatInput.Name = "txtChatInput";
            txtChatInput.Size = new Size(225, 23);
            txtChatInput.TabIndex = 1;
            txtChatInput.KeyDown += txtChatInput_KeyDown;
            // 
            // rtbChat
            // 
            rtbChat.BackColor = Color.FromArgb(24, 24, 30);
            rtbChat.BorderStyle = BorderStyle.None;
            rtbChat.ForeColor = Color.LightGray;
            rtbChat.Location = new Point(14, 22);
            rtbChat.Name = "rtbChat";
            rtbChat.ReadOnly = true;
            rtbChat.Size = new Size(288, 105);
            rtbChat.TabIndex = 0;
            rtbChat.Text = "";
            // 
            // pnlLobby
            // 
            pnlLobby.Controls.Add(btnSendLobbyChat);
            pnlLobby.Controls.Add(txtLobbyChatInput);
            pnlLobby.Controls.Add(rtbLobbyChat);
            pnlLobby.Controls.Add(lblLobbyChatTitle);
            pnlLobby.Controls.Add(btnJoinRoom);
            pnlLobby.Controls.Add(btnCreateRoom);
            pnlLobby.Controls.Add(txtRoomName);
            pnlLobby.Controls.Add(lblCreateRoomTitle);
            pnlLobby.Controls.Add(lstRooms);
            pnlLobby.Controls.Add(lblRoomListTitle);
            pnlLobby.Controls.Add(btnSetUsername);
            pnlLobby.Controls.Add(txtUsername);
            pnlLobby.Controls.Add(lblUserTitle);
            pnlLobby.Location = new Point(12, 12);
            pnlLobby.Name = "pnlLobby";
            pnlLobby.Size = new Size(450, 450);
            pnlLobby.TabIndex = 4;
            pnlLobby.Visible = false;
            // 
            // btnSendLobbyChat
            // 
            btnSendLobbyChat.BackColor = Color.FromArgb(50, 50, 60);
            btnSendLobbyChat.FlatStyle = FlatStyle.Flat;
            btnSendLobbyChat.ForeColor = Color.LightSkyBlue;
            btnSendLobbyChat.Location = new Point(386, 417);
            btnSendLobbyChat.Name = "btnSendLobbyChat";
            btnSendLobbyChat.Size = new Size(54, 23);
            btnSendLobbyChat.TabIndex = 12;
            btnSendLobbyChat.Text = "Gửi";
            btnSendLobbyChat.UseVisualStyleBackColor = false;
            btnSendLobbyChat.Click += btnSendLobbyChat_Click;
            // 
            // txtLobbyChatInput
            // 
            txtLobbyChatInput.BackColor = Color.FromArgb(40, 40, 48);
            txtLobbyChatInput.BorderStyle = BorderStyle.FixedSingle;
            txtLobbyChatInput.ForeColor = Color.White;
            txtLobbyChatInput.Location = new Point(207, 417);
            txtLobbyChatInput.Name = "txtLobbyChatInput";
            txtLobbyChatInput.Size = new Size(173, 23);
            txtLobbyChatInput.TabIndex = 11;
            txtLobbyChatInput.KeyDown += txtLobbyChatInput_KeyDown;
            // 
            // rtbLobbyChat
            // 
            rtbLobbyChat.BackColor = Color.FromArgb(24, 24, 30);
            rtbLobbyChat.BorderStyle = BorderStyle.None;
            rtbLobbyChat.ForeColor = Color.LightGray;
            rtbLobbyChat.Location = new Point(207, 231);
            rtbLobbyChat.Name = "rtbLobbyChat";
            rtbLobbyChat.ReadOnly = true;
            rtbLobbyChat.Size = new Size(233, 178);
            rtbLobbyChat.TabIndex = 10;
            rtbLobbyChat.Text = "";
            // 
            // lblLobbyChatTitle
            // 
            lblLobbyChatTitle.AutoSize = true;
            lblLobbyChatTitle.ForeColor = Color.White;
            lblLobbyChatTitle.Location = new Point(207, 211);
            lblLobbyChatTitle.Name = "lblLobbyChatTitle";
            lblLobbyChatTitle.Size = new Size(106, 15);
            lblLobbyChatTitle.TabIndex = 9;
            lblLobbyChatTitle.Text = "Chat Sảnh chờ";
            // 
            // btnJoinRoom
            // 
            btnJoinRoom.BackColor = Color.FromArgb(50, 50, 60);
            btnJoinRoom.FlatStyle = FlatStyle.Flat;
            btnJoinRoom.ForeColor = Color.Yellow;
            btnJoinRoom.Location = new Point(14, 413);
            btnJoinRoom.Name = "btnJoinRoom";
            btnJoinRoom.Size = new Size(177, 27);
            btnJoinRoom.TabIndex = 8;
            btnJoinRoom.Text = "Vào phòng đã chọn";
            btnJoinRoom.UseVisualStyleBackColor = false;
            btnJoinRoom.Click += btnJoinRoom_Click;
            // 
            // btnCreateRoom
            // 
            btnCreateRoom.BackColor = Color.FromArgb(50, 50, 60);
            btnCreateRoom.FlatStyle = FlatStyle.Flat;
            btnCreateRoom.ForeColor = Color.Lime;
            btnCreateRoom.Location = new Point(365, 126);
            btnCreateRoom.Name = "btnCreateRoom";
            btnCreateRoom.Size = new Size(75, 25);
            btnCreateRoom.TabIndex = 7;
            btnCreateRoom.Text = "Tạo";
            btnCreateRoom.UseVisualStyleBackColor = false;
            btnCreateRoom.Click += btnCreateRoom_Click;
            // 
            // txtRoomName
            // 
            txtRoomName.BackColor = Color.FromArgb(40, 40, 48);
            txtRoomName.BorderStyle = BorderStyle.FixedSingle;
            txtRoomName.ForeColor = Color.White;
            txtRoomName.Location = new Point(207, 127);
            txtRoomName.Name = "txtRoomName";
            txtRoomName.Size = new Size(152, 23);
            txtRoomName.TabIndex = 6;
            // 
            // lblCreateRoomTitle
            // 
            lblCreateRoomTitle.AutoSize = true;
            lblCreateRoomTitle.ForeColor = Color.White;
            lblCreateRoomTitle.Location = new Point(207, 104);
            lblCreateRoomTitle.Name = "lblCreateRoomTitle";
            lblCreateRoomTitle.Size = new Size(81, 15);
            lblCreateRoomTitle.TabIndex = 5;
            lblCreateRoomTitle.Text = "Tạo phòng mới";
            // 
            // lstRooms
            // 
            lstRooms.BackColor = Color.FromArgb(30, 30, 36);
            lstRooms.BorderStyle = BorderStyle.FixedSingle;
            lstRooms.ForeColor = Color.White;
            lstRooms.FormattingEnabled = true;
            lstRooms.ItemHeight = 15;
            lstRooms.Location = new Point(14, 127);
            lstRooms.Name = "lstRooms";
            lstRooms.Size = new Size(177, 272);
            lstRooms.TabIndex = 4;
            // 
            // lblRoomListTitle
            // 
            lblRoomListTitle.AutoSize = true;
            lblRoomListTitle.ForeColor = Color.White;
            lblRoomListTitle.Location = new Point(14, 104);
            lblRoomListTitle.Name = "lblRoomListTitle";
            lblRoomListTitle.Size = new Size(111, 15);
            lblRoomListTitle.TabIndex = 3;
            lblRoomListTitle.Text = "Danh sách phòng";
            // 
            // btnSetUsername
            // 
            btnSetUsername.BackColor = Color.FromArgb(50, 50, 60);
            btnSetUsername.FlatStyle = FlatStyle.Flat;
            btnSetUsername.ForeColor = Color.Cyan;
            btnSetUsername.Location = new Point(310, 33);
            btnSetUsername.Name = "btnSetUsername";
            btnSetUsername.Size = new Size(130, 27);
            btnSetUsername.TabIndex = 2;
            btnSetUsername.Text = "Lưu biệt danh";
            btnSetUsername.UseVisualStyleBackColor = false;
            btnSetUsername.Click += btnSetUsername_Click;
            // 
            // txtUsername
            // 
            txtUsername.BackColor = Color.FromArgb(40, 40, 48);
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.ForeColor = Color.White;
            txtUsername.Location = new Point(14, 35);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(280, 23);
            txtUsername.TabIndex = 1;
            // 
            // lblUserTitle
            // 
            lblUserTitle.AutoSize = true;
            lblUserTitle.ForeColor = Color.White;
            lblUserTitle.Location = new Point(14, 12);
            lblUserTitle.Name = "lblUserTitle";
            lblUserTitle.Size = new Size(116, 15);
            lblUserTitle.TabIndex = 0;
            lblUserTitle.Text = "Biệt danh của bạn";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(24, 24, 30);
            ClientSize = new Size(808, 477);
            Controls.Add(pnlLobby);
            Controls.Add(grpChat);
            Controls.Add(grpSettings);
            Controls.Add(grpConnection);
            Controls.Add(pnlBoard);
            DoubleBuffered = true;
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Caro Socket Game - Lobby Edition";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            grpConnection.ResumeLayout(false);
            grpConnection.PerformLayout();
            grpSettings.ResumeLayout(false);
            grpSettings.PerformLayout();
            grpChat.ResumeLayout(false);
            grpChat.PerformLayout();
            pnlLobby.ResumeLayout(false);
            pnlLobby.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private DoubleBufferedPanel pnlBoard;
        private GroupBox grpConnection;
        private Label lblIP;
        private Label lblPort;
        private TextBox txtIP;
        private TextBox txtPort;
        private Button btnConnect;
        private Label lblRole;
        private GroupBox grpSettings;
        private ComboBox cmbMode;
        private Label lblMode;
        private Button btnStart;
        private Label lblTurn;
        private ProgressBar pbCountdown;
        private Label lblTimer;
        private Label lblScore;
        private Button btnLeaveRoom;
        private GroupBox grpChat;
        private RichTextBox rtbChat;
        private TextBox txtChatInput;
        private Button btnSendChat;
        private Panel pnlLobby;
        private TextBox txtUsername;
        private Label lblUserTitle;
        private Button btnSetUsername;
        private Label lblRoomListTitle;
        private ListBox lstRooms;
        private TextBox txtRoomName;
        private Label lblCreateRoomTitle;
        private Button btnCreateRoom;
        private Button btnJoinRoom;
        private Label lblLobbyChatTitle;
        private RichTextBox rtbLobbyChat;
        private TextBox txtLobbyChatInput;
        private Button btnSendLobbyChat;
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }
    }
}
