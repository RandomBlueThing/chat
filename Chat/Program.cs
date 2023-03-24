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
    Console.WriteLine(ex.Message);
}
finally
{
    Console.WriteLine("Byeee!");
}


static Assistant? CreateAssistant()
{
    var cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "chat", "appsettings.json");
    if (!File.Exists(cfgPath))
    {
        throw new FileNotFoundException($"Config file not found: {cfgPath}");
    }
    Console.Write($"Found cfg: {cfgPath}");

    var cfg = new ConfigurationBuilder()
        .AddJsonFile(cfgPath, optional: false, reloadOnChange: true)
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
