using System.Diagnostics;
using System.Drawing;

namespace Vira.Setup;

internal sealed class SetupForm : Form
{
    private readonly Label _status;
    private readonly Button _installButton;
    private readonly ProgressBar _progress;

    public SetupForm()
    {
        Text = "Vira Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(510, 285);
        BackColor = Color.FromArgb(246, 248, 252);
        Font = new Font("Segoe UI", 10);

        var title = new Label
        {
            Text = "Install Vira",
            AutoSize = false,
            Location = new Point(28, 24),
            Size = new Size(450, 40),
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.FromArgb(23, 32, 51)
        };
        var description = new Label
        {
            Text = "Install the latest Vira version for your Windows account. No administrator access or separate .NET installation is required.",
            AutoSize = false,
            Location = new Point(30, 75),
            Size = new Size(450, 55),
            ForeColor = Color.FromArgb(82, 94, 112)
        };
        _status = new Label
        {
            Text = "The installer will add Start Menu and Desktop shortcuts and enable File Explorer conversion actions.",
            AutoSize = false,
            Location = new Point(30, 142),
            Size = new Size(450, 42),
            ForeColor = Color.FromArgb(82, 94, 112)
        };
        _progress = new ProgressBar
        {
            Location = new Point(30, 195),
            Size = new Size(450, 8),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 25,
            Visible = false
        };
        _installButton = new Button
        {
            Text = "Install",
            Location = new Point(350, 224),
            Size = new Size(130, 38),
            BackColor = Color.FromArgb(49, 95, 234),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _installButton.FlatAppearance.BorderSize = 0;
        _installButton.Click += InstallButton_Click;

        Controls.AddRange([title, description, _status, _progress, _installButton]);
        AcceptButton = _installButton;
    }

    private async void InstallButton_Click(object? sender, EventArgs e)
    {
        _installButton.Enabled = false;
        _progress.Visible = true;
        _status.Text = "Installing Vira…";

        try
        {
            var installPath = await Task.Run(ViraInstaller.Install);
            _progress.Visible = false;
            _status.Text = $"Vira was installed successfully. You can find it in the Start Menu or on your Desktop.\n{installPath}";
            _installButton.Text = "Close";
            _installButton.Enabled = true;
            _installButton.Click -= InstallButton_Click;
            _installButton.Click += (_, _) => Close();
            AcceptButton = _installButton;
        }
        catch (Exception exception)
        {
            _progress.Visible = false;
            _status.Text = "Installation failed.";
            _installButton.Enabled = true;
            MessageBox.Show(this, exception.Message, "Vira Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
