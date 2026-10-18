using System.Windows;

namespace ConversorFicheiros;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.FirstOrDefault() == "--register-context-menu")
        {
            try
            {
                var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Não foi possível localizar o executável.");
                new ExplorerMenuService().Register(executable);
                Environment.ExitCode = 0;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Não foi possível registar o menu: {exception.Message}", "Vira — erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.ExitCode = 1;
            }
            Shutdown();
            return;
        }

        var window = new ConversorFicheiros.MainWindow();
        MainWindow = window;
        window.Show();
    }
}
