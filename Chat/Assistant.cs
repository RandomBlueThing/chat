using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using System.Text.Json;

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

internal class Assistant
{
    private readonly IOpenAIService _ai;
    private readonly IConfiguration _cfg;
    private readonly Dictionary<string, Func<string, Task<bool>>> _commands;
    private readonly TimeSpan _conversationLength;
    private List<TimedMessage> _history = new List<TimedMessage>();
    private readonly List<ChatMessage> _prime = new List<ChatMessage>();
    private static string _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "chat");


    public Assistant(IOpenAIService ai, IConfiguration cfg)
    {
        _ai = ai;
        _cfg = cfg;
        _commands = new Dictionary<string, Func<string, Task<bool>>>(StringComparer.OrdinalIgnoreCase)
        {
            { "quit", p => Task.FromResult(true) },
            { "bye", p => Task.FromResult(true) },
            { "exit", p => Task.FromResult(true) },
            { "!help", Help },
            { "!", Help },
            { "!cls", p => { Console.Clear(); return Task.FromResult(false); } },
            { "!forget", p => { Console.WriteLine("Dropping history"); _history.Clear(); return Task.FromResult(false); } },
            { "", p => Task.FromResult(false) }
        };
        _conversationLength = TimeSpan.Parse(_cfg["conversation-length"] ?? "00:15:00");
    }


    public async Task RunAsync()
    {
        // Prime history
        _prime.AddRange(new[] {
            new ChatMessage(StaticValues.ChatMessageRoles.System, "You are a helpful, slightly sarcastic, assistant with a cat fixation. You talk about cats all the time."),
            new ChatMessage(StaticValues.ChatMessageRoles.System, "You have an imaginary cat who's name changes each time you mention them."),
            new ChatMessage(StaticValues.ChatMessageRoles.System, "You also can't stop talking about Panda's. Occasionally you'll write a short poem about them")
        });

        _history = LoadHistory();

        while (true)
        {
            var prompt = ReadPrompt();
            if (await ProcessAsync(prompt))
            {
                break;
            }
        }
    }

    private static string ReadPrompt()
    {
        Console.WriteLine();
        Console.Write("> ");
        return Console.ReadLine() ?? "";
    }


    private Task<bool> ProcessAsync(string prompt)
    {
        var x = _commands.ContainsKey(prompt)
            ? _commands[prompt]
            : ProcessChatAsync;

        return x(prompt);
    }

    private IEnumerable<TimedMessage> History => _history.Where(m => m.TimestampUtc > DateTime.UtcNow.Subtract(_conversationLength));

    private async Task<bool> ProcessChatAsync(string prompt)
    {
        _history.Add(new TimedMessage(StaticValues.ChatMessageRoles.User, prompt));

        var messages = _prime.Concat(History.Select(m => m.ToChatMessage())).ToList();

        var completionResult = _ai.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
        {
            Messages = messages,
            Model = Models.ChatGpt3_5Turbo
        });

        var rs = "";
        var formatter = new Formatter();

        await foreach (var completion in completionResult)
        {
            if (completion.Successful)
            {
                var content = completion.Choices.First().Message.Content;
                if (content != null)
                {
                    rs += content;
                    formatter.Append(content);
                }
            }
            else
            {
                if (completion.Error == null)
                {
                    throw new Exception("Unknown Error");
                }

                Console.WriteLine($"{completion.Error.Code}: {completion.Error.Message}");
            }
        }

        formatter.Finish();

        _history.Add(new TimedMessage(StaticValues.ChatMessageRoles.Assistant, rs));

        await SaveHistoryAsync();

        return false;
    }


    private List<TimedMessage> LoadHistory()
    {
        var history = new List<TimedMessage>();
        var path = Path.Combine(_basePath, "history.json");
        if (File.Exists(path))
        {
            history = JsonSerializer.Deserialize<List<TimedMessage>>(File.ReadAllText(path)) ?? history;
        }

        // Reset chat history
        //history.ForEach(h => h.TimestampUtc = DateTime.UtcNow);

        return history;
    }

    private async Task SaveHistoryAsync()
    {
        var path = Path.Combine(_basePath, "history.json");
        var json = JsonSerializer.Serialize(History.ToArray());
        await File.WriteAllTextAsync(path, json);
    }


    private Task<bool> Help(string arg)
    {
        Console.WriteLine("Commands:");
        Console.WriteLine("  !help - Show this help");
        Console.WriteLine("  !cls - Clear screen");
        Console.WriteLine("  !forget - Forget history");
        Console.WriteLine("  quit/exit/bye - Quit");

        Console.WriteLine("Information:");
        Console.WriteLine("  Configuration: " + _basePath);
        Console.WriteLine($"  Conversation Length: {_conversationLength}");
        Console.WriteLine($"  History length: {History.Count()}");

        return Task.FromResult(false);
    }
}
