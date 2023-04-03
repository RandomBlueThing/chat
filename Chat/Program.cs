using Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.GPT3.Extensions;

try
{
    var assistent = CreateAssistant();
    if(assistent != null)
    {
        await assistent.RunAsync();
    }
}
catch (Exception ex)
{
    Console.WriteLine("Ooops, bust it: " + ex.Message);
    Console.WriteLine("-------------------------------------------------------------------");
    Console.WriteLine("Maybe email the below to PJ, whatevs.");
    Console.WriteLine();
    Console.WriteLine(ex);
    Console.WriteLine("-------------------------------------------------------------------");
    Console.WriteLine("key");
    Console.ReadKey(true);
}
finally
{
    Console.WriteLine("Byeee!");
}


static Assistant? CreateAssistant()
{
    var cfg = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "chat", "appsettings.json"), optional: true, reloadOnChange: true)
        .Build();

    var services = new ServiceCollection();

    services.AddSingleton<IConfiguration>(cfg);
    services.AddOpenAIService(settings => {
        settings.ApiKey = cfg["apikey"];
    });
    services.AddTransient<Assistant>();

    return services
        .BuildServiceProvider(true)
        .GetService<Assistant>();
}
