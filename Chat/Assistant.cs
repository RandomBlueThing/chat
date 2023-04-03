using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Xml.Linq;

namespace Chat;

internal class Assistant
{
    private readonly IOpenAIService _ai;
    private readonly IConfiguration _cfg;
    private readonly Command[] _commands;
    private readonly TimeSpan _conversationLength;
    private readonly List<ChatMessage> _prime = new List<ChatMessage>();
    private readonly static string _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "chat");
    private List<TimedMessage> _history = new List<TimedMessage>();


    public Assistant(IOpenAIService ai, IConfiguration cfg)
    {
        _ai = ai;
        _cfg = cfg;
        _commands = new Command[] {
            new Command(new[]{"quit", "bye", "exit" }, "Terminate", willTerminate: true),
            new Command("!help", "Show help", Help),
            new Command("!forget", "Forget this conversaion", Forget),
            new Command("!history", "Show conversation history", DumpHistory),
            new Command("!cls", "Clear screen", CLS)
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

            var cmd = _commands.FirstOrDefault(x => x.CanExecute(prompt));
            if(cmd != null)
            {
                await cmd.ExecuteAsync(prompt);
                if (cmd.WillTerminate)
                {
                    break;
                }
            }
            else
            {
                await ProcessChatAsync(prompt);
            }
        }
    }

    private static string ReadPrompt()
    {
        Console.WriteLine();
        Console.Write("> ");
        return Console.ReadLine() ?? "";
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

    private Task DumpHistory(string arg)
    {
        foreach (var h in History.OrderBy(h => h.TimestampUtc))
        {
            Console.WriteLine($"[{h.TimestampUtc:HH:mm:ss}] [{h.Role}] {h.Message.Truncate(60)}");
        }

        return Task.CompletedTask;
    }

    private Task Forget(string arg)
    {
        return Task.CompletedTask;
    }

    private Task CLS(string arg)
    {
        Console.Clear();
        return Task.CompletedTask;
    }

    private Task Help(string arg)
    {
        Console.WriteLine("Commands:");
        foreach(var c in _commands)
        {
            Console.WriteLine($"  {string.Join(",", c.Names)} - {c.Description}");
        }

        Console.WriteLine();

        Console.WriteLine("Information:");
        Console.WriteLine($"  Configuration: {_basePath}");
        Console.WriteLine($"  Memory length (time): {_conversationLength}");
        Console.WriteLine($"  History length: {History.Count()}");

        return Task.CompletedTask;
    }
}
