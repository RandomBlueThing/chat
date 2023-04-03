using OpenAI.GPT3.ObjectModels.RequestModels;

namespace Chat;

internal struct TimedMessage
{
    public TimedMessage(string role, string message)
    {
        TimestampUtc = DateTime.UtcNow;
        Role = role;
        Message = message;
    }

    public DateTime TimestampUtc { get; set; }
    public string Role { get; set; }
    public string Message { get; set; }

    public ChatMessage ToChatMessage()
    {
        return new ChatMessage(Role, Message);
    }
}
