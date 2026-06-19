using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        Unknown // Dùng khi gói tin không hợp lệ
    }

    // 2. Class chứa dữ liệu sau khi phân tích gói tin
    public class ParsedMessage
    {
        public MessageType Type { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public string Message { get; set; }

        public ParsedMessage()
        {
            Type = MessageType.Unknown;
            Row = -1;
            Col = -1;
            Message = string.Empty;
        }
    }

    // 3. Class xử lý Giao thức (Tạo và Phân tích)
    public static class MessageProtocol
    {
        // ==========================================
        // CÁC HÀM TẠO GÓI TIN (CREATE)
        // ==========================================

        public static string CreateMove(int row, int col)
        {
            return $"MOVE|{row}|{col}";
        }

        public static string CreateChat(string message)
        {
            // Trong trường hợp an toàn, có thể replace ký tự '|' trong message để tránh lỗi parse
            string safeMessage = message.Replace("|", "");
            return $"CHAT|{safeMessage}";
        }

        public static string CreateReset() => "RESET";
        public static string CreateExit() => "EXIT";
        public static string CreateTimeout() => "TIMEOUT";
        public static string CreateWin() => "WIN";
        public static string CreateLose() => "LOSE";
        public static string CreateDraw() => "DRAW";

        // ==========================================
        // HÀM PHÂN TÍCH GÓI TIN (PARSE)
        // ==========================================

        public static ParsedMessage Parse(string msg)
        {
            var result = new ParsedMessage();

            if (string.IsNullOrWhiteSpace(msg))
                return result;

            // Tách chuỗi dựa trên ký tự '|'
            // Đối với CHAT, ta chỉ tách làm 2 phần để giữ nguyên nội dung tin nhắn nếu có khoảng trắng
            string[] parts = msg.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

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
                    // Lấy phần còn lại của chuỗi sau chữ "CHAT|"
                    if (msg.Length > 5)
                    {
                        result.Message = msg.Substring(5);
                    }
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

                default:
                    result.Type = MessageType.Unknown;
                    break;
            }

            return result;
        }
    }
}
