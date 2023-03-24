using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat
{
    internal class Assistant
    {
        private readonly IOpenAIService _ai;
        private readonly List<ChatMessage> _messages = new List<ChatMessage>();
        private static readonly string[] _quitCommands = new[] {
            "quit",
            "bye"
        };

        public Assistant(IOpenAIService ai)
        {
            _ai = ai;
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

                // Instead of doing this, scan a prompt -> action map, with the default action being to pass it to chat-gpt
                if (IsQuitCommand(prompt))
                {
                    break;
                }

                if(prompt is not null)
                {
                    _messages.Add(new ChatMessage(StaticValues.ChatMessageRoles.User, prompt));

                    // @TODO: Instead of pushing entire history up, just push initial prime stuff & all *recent* stuff...
                    var completionResult = _ai.ChatCompletion.CreateCompletionAsStream(new ChatCompletionCreateRequest
                    {
                        Messages = _messages,
                        Model = Models.ChatGpt3_5Turbo
                    });

                    var rs = "";

                    await foreach (var completion in completionResult)
                    {
                        if (completion.Successful)
                        {
                            var content = completion.Choices.First().Message.Content;
                            rs += content;
                            Console.Write(content);
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

                    _messages.Add(new ChatMessage(StaticValues.ChatMessageRoles.Assistant, rs));
                }
            }
        }


        private bool IsQuitCommand(string? cmd)
        {
            return cmd is not null && _quitCommands.Any(c => cmd.Equals(c, StringComparison.OrdinalIgnoreCase));
        }
    }
}
