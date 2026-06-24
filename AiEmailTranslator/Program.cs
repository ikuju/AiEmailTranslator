namespace AiEmailTranslator;

static class Program
{
    private const string MutexName = "Global\\AiEmailTranslator.SingleInstance";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            ActivateExistingInstance();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }    

    private static void ActivateExistingInstance()
    {
        var currentId = Environment.ProcessId;
        var process = System.Diagnostics.Process
            .GetProcessesByName("AiEmailTranslator")
            .FirstOrDefault(p => p.Id != currentId && p.MainWindowHandle != IntPtr.Zero);

        if (process is null)
        {
            return;
        }

        NativeMethods.ShowWindow(process.MainWindowHandle, 9);
        NativeMethods.SetForegroundWindow(process.MainWindowHandle);
    }
}
