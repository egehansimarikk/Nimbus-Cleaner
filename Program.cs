using System;
using System.Threading;
using System.Windows.Forms;

namespace WinCleaner
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (_, e) =>
                MessageBox.Show(e.Exception.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => { };

            string cleanPath = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "/cleanpath", StringComparison.OrdinalIgnoreCase))
                {
                    cleanPath = i + 1 < args.Length ? args[i + 1] : string.Empty;
                    break;
                }
            }

            Application.Run(new MainForm(cleanPath));
        }
    }
}
