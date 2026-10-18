using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;

namespace Vira.Setup;

internal static class ViraInstaller
{
    private const string PayloadResource = "Vira.Setup.payload.zip";
    private const string ApplicationFileName = "ConversorFicheiros.exe";

    public static string Install()
    {
        var installDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Vira");
        Directory.CreateDirectory(installDirectory);
        var applicationPath = Path.Combine(installDirectory, ApplicationFileName);

        using (var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResource)
            ?? throw new InvalidDataException("The application files are missing from this installer."))
        using (var archive = new ZipArchive(payload, ZipArchiveMode.Read))
        {
            var application = archive.GetEntry(ApplicationFileName)
                ?? throw new InvalidDataException("The application executable is missing from this installer.");
            application.ExtractToFile(applicationPath, overwrite: true);
        }

        CreateShortcut(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Vira", "Vira.lnk"),
            applicationPath,
            installDirectory);
        CreateShortcut(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Vira.lnk"),
            applicationPath,
            installDirectory);

        using var registration = Process.Start(new ProcessStartInfo
        {
            FileName = applicationPath,
            Arguments = "--register-context-menu",
            WorkingDirectory = installDirectory,
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Vira could not register the File Explorer commands.");
        registration.WaitForExit();
        if (registration.ExitCode != 0)
            throw new InvalidOperationException("Vira could not register the File Explorer commands.");

        return installDirectory;
    }

    private static void CreateShortcut(string shortcutPath, string applicationPath, string workingDirectory)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is not available to create shortcuts.");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows could not create the shortcut service.");
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = applicationPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = "Vira file converter and PDF signer";
        shortcut.Save();
        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
        System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
    }
}
