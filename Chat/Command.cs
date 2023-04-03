namespace Chat;

public class Command
{
    private readonly Func<string, Task>? _function;
    public string[] Names { get; private set; }
    public string Description { get; private set; }
    public bool WillTerminate { get; private set; }


    public Command(string[] names, string description, Func<string, Task>? function = null, bool willTerminate = false)
    {
        _function = function;

        Names = names;
        Description = description;
        WillTerminate = willTerminate;
    }

    public Command(string name, string description, Func<string, Task>? function = null, bool willTerminate = false)
        : this(new[] { name}, description, function, willTerminate)
    {
    }

    public async virtual Task ExecuteAsync(string args)
    {
        if(_function != null)
        {
            await _function.Invoke(args);
        }
    }

    public virtual bool CanExecute(string prompt)
    {
        return Names.Contains(prompt, StringComparer.InvariantCultureIgnoreCase);
    }
}
