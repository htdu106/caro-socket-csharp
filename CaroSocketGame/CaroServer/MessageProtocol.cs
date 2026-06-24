using System;

namespace CaroServer
{
    public enum MessageType
    {
        Move,
        Chat,
        Reset,
        Exit,
        Timeout,
        Win,
        Lose,
        Draw,
        Mode,
        Score,
        MatchWin,
        Start,
        Role,
        User,
        LobbyChat,
        CreateRoom,
        JoinRoom,
        LeaveRoom,
        RoomList,
        RoomInfo,
        RoomPlayers,
        Unknown
    }

    public class ParsedMessage
    {
        public MessageType Type { get; set; } = MessageType.Unknown;
        public int Row { get; set; } = -1;
        public int Col { get; set; } = -1;
        public string Message { get; set; } = string.Empty;
        public string Extra { get; set; } = string.Empty; // used for Mode, MatchWin, Role, User, CreateRoom, JoinRoom, RoomList, etc.
        public int Score1 { get; set; } = 0;
        public int Score2 { get; set; } = 0;
    }

    public static class MessageProtocol
    {
        public static string CreateMove(int row, int col) => $"MOVE|{row}|{col}";
        public static string CreateChat(string message) => $"CHAT|{message.Replace("|", "")}";
        public static string CreateReset() => "RESET";
        public static string CreateExit() => "EXIT";
        public static string CreateTimeout() => "TIMEOUT";
        public static string CreateWin() => "WIN";
        public static string CreateLose() => "LOSE";
        public static string CreateDraw() => "DRAW";
        public static string CreateMode(string modeType) => $"MODE|{modeType}";
        public static string CreateScore(int score1, int score2) => $"SCORE|{score1}|{score2}";
        public static string CreateMatchWin(int winner) => $"MATCH_WIN|{winner}";
        public static string CreateStart() => "START";
        public static string CreateRole(int role) => $"ROLE|{role}";
        public static string CreateUser(string username) => $"USER|{username.Replace("|", "")}";
        public static string CreateLobbyChat(string message) => $"LOBBY_CHAT|{message.Replace("|", "")}";
        public static string CreateCreateRoom(string roomName) => $"CREATE_ROOM|{roomName.Replace("|", "")}";
        public static string CreateJoinRoom(int roomId) => $"JOIN_ROOM|{roomId}";
        public static string CreateLeaveRoom() => "LEAVE_ROOM";
        public static string CreateRoomList(string roomData) => $"ROOM_LIST|{roomData}";
        public static string CreateRoomInfo(int roomId, string roomName) => $"ROOM_INFO|{roomId}|{roomName.Replace("|", "")}";
        public static string CreateRoomPlayers(string p1Name, string p2Name) => $"ROOM_PLAYERS|{p1Name.Replace("|", "")}|{p2Name.Replace("|", "")}";

        public static ParsedMessage Parse(string msg)
        {
            var result = new ParsedMessage();
            if (string.IsNullOrWhiteSpace(msg)) return result;

            string[] parts = msg.Split('|');
            if (parts.Length == 0) return result;

            string header = parts[0].ToUpper();
            switch (header)
            {
                case "MOVE":
                    result.Type = MessageType.Move;
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int r) && int.TryParse(parts[2], out int c))
                    {
                        result.Row = r;
                        result.Col = c;
                    }
                    break;
                case "CHAT":
                    result.Type = MessageType.Chat;
                    if (msg.Length > 5) result.Message = msg.Substring(5);
                    break;
                case "RESET":
                    result.Type = MessageType.Reset;
                    break;
                case "EXIT":
                    result.Type = MessageType.Exit;
                    break;
                case "TIMEOUT":
                    result.Type = MessageType.Timeout;
                    break;
                case "WIN":
                    result.Type = MessageType.Win;
                    break;
                case "LOSE":
                    result.Type = MessageType.Lose;
                    break;
                case "DRAW":
                    result.Type = MessageType.Draw;
                    break;
                case "MODE":
                    result.Type = MessageType.Mode;
                    if (parts.Length >= 2) result.Extra = parts[1];
                    break;
                case "SCORE":
                    result.Type = MessageType.Score;
                    if (parts.Length >= 3 && int.TryParse(parts[1], out int s1) && int.TryParse(parts[2], out int s2))
                    {
                        result.Score1 = s1;
                        result.Score2 = s2;
                    }
                    break;
                case "MATCH_WIN":
                    result.Type = MessageType.MatchWin;
                    if (parts.Length >= 2) result.Extra = parts[1];
                    break;
                case "START":
                    result.Type = MessageType.Start;
                    break;
                case "ROLE":
                    result.Type = MessageType.Role;
                    if (parts.Length >= 2) result.Extra = parts[1];
                    break;
                case "USER":
                    result.Type = MessageType.User;
                    if (parts.Length >= 2) result.Extra = parts[1];
                    break;
                case "LOBBY_CHAT":
                    result.Type = MessageType.LobbyChat;
                    if (msg.Length > 11) result.Message = msg.Substring(11);
                    break;
                case "CREATE_ROOM":
                    result.Type = MessageType.CreateRoom;
                    if (parts.Length >= 2) result.Extra = parts[1];
                    break;
                case "JOIN_ROOM":
                    result.Type = MessageType.JoinRoom;
                    if (parts.Length >= 2) result.Extra = parts[1];
                    break;
                case "LEAVE_ROOM":
                    result.Type = MessageType.LeaveRoom;
                    break;
                case "ROOM_LIST":
                    result.Type = MessageType.RoomList;
                    if (msg.Length > 10) result.Extra = msg.Substring(10);
                    break;
                case "ROOM_INFO":
                    result.Type = MessageType.RoomInfo;
                    if (parts.Length >= 3)
                    {
                        result.Extra = parts[1];
                        result.Message = parts[2];
                    }
                    break;
                case "ROOM_PLAYERS":
                    result.Type = MessageType.RoomPlayers;
                    if (parts.Length >= 2) result.Extra = parts[1];
                    if (parts.Length >= 3) result.Message = parts[2];
                    break;
                default:
                    result.Type = MessageType.Unknown;
                    break;
            }
            return result;
        }
    }
}
