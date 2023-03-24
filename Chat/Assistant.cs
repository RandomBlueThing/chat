using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;

namespace Chat;

internal class Assistant
{
    private readonly IOpenAIService _ai;
    private readonly Dictionary<string, Func<string, Task<bool>>> _commands;
    private readonly List<ChatMessage> _messages = new List<ChatMessage>();
    private readonly List<ChatMessage> _prime = new List<ChatMessage>();

    public Assistant(IOpenAIService ai)
    {
        _ai = ai;
        _commands = new Dictionary<string, Func<string, Task<bool>>>(StringComparer.OrdinalIgnoreCase)
        {
            { "quit", p => Task.FromResult(true) },
            { "bye", p => Task.FromResult(true) },
            { "cfg", Cfg },
            { "cls", p => { Console.Clear(); return Task.FromResult(false); } },
            { "", p => Task.FromResult(false) }
        };
    }


    public async Task RunAsync()
    {
        // Prime history
        _prime.AddRange(new[] {
            new ChatMessage(StaticValues.ChatMessageRoles.System, "You are a helpful, slightly sarcastic, assistant with a random name and cat fixation. You talk about cats all the time."),
            new ChatMessage(StaticValues.ChatMessageRoles.System, "You have an imaginary cat whos name changes each time you mention them.")
        });

        while (true)
        {
            var prompt = ReadPrompt();
            if(await ProcessAsync(prompt))
            {
                break;
            }
        }
    }


    private static string ReadPrompt()
    {
        Console.WriteLine();
        //var c = Console.ForegroundColor;
        //Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("> ");

        var prompt = Console.ReadLine();
        //Console.ForegroundColor = c;
        return prompt??"";
    }


    private Task<bool> ProcessAsync(string prompt)
    {
        var x = _commands.ContainsKey(prompt) 
            ? _commands[prompt] 
            : ProcessChatAsync;

        return x(prompt);
    }


    private async Task<bool> ProcessChatAsync(string prompt)
    {
        _messages.Add(new ChatMessage(StaticValues.ChatMessageRoles.User, prompt));

        var messages = _prime.Concat(_messages).ToList();

        // @TODO: Instead of pushing entire history up, just push initial prime stuff & all *recent* stuff...
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
                if(content != null)
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

        _messages.Add(new ChatMessage(StaticValues.ChatMessageRoles.Assistant, rs));

        return false;
    }

    
    private Task<bool> Cfg(string arg)
    {
        Console.WriteLine("TODO: Cfg stuff");
        return Task.FromResult(false);
    }
}
