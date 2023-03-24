using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat;

internal class Assistant
{
    private readonly IOpenAIService _ai;
    private readonly Dictionary<string, Func<string, Task<bool>>> _commands;
    private readonly List<ChatMessage> _messages = new List<ChatMessage>();

    public Assistant(IOpenAIService ai)
    {
        _ai = ai;
        _commands = new Dictionary<string, Func<string, Task<bool>>>()
        {
            { "quit", Quit },
            { "bye", Quit },
            { "cfg", Cfg }
        };
    }


    public async Task RunAsync()
    {
        // Prime history
        _messages.AddRange(new[] {
            new ChatMessage(StaticValues.ChatMessageRoles.System, "You are a helpful, slightly sarcastic, assistant called bob with a cat fixation. You talk about cats all the time.")
        });

        while (true)
        {
            Console.WriteLine();
            Console.Write(">");

            var prompt = Console.ReadLine();

            if (prompt is not null && await ProcessAsync(prompt))
            {
                break;
            }
        }
    }



    private Task<bool> ProcessAsync(string prompt)
    {
        var x = _commands.ContainsKey(prompt) 
            ? _commands[prompt] 
            : ProcessChatAsync;

        return x(prompt);
    }


    private Task<bool> Quit(string arg)
    {
        return Task.FromResult(true);
    }

    private Task<bool> Cfg(string arg)
    {
        Console.WriteLine("TODO: Cfg stuff");
        return Task.FromResult(false);
    }


    private async Task<bool> ProcessChatAsync(string prompt)
    {
        _messages.Add(new ChatMessage(StaticValues.ChatMessageRoles.User, prompt));

        // @TODO: Instead of pushing entire history up, just push initial prime stuff & all *recent* stuff...
        var completionResult = _ai.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
        {
            Messages = _messages,
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
}
