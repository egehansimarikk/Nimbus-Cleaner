using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinCleaner
{
    internal static class ContextMenuManager
    {
        private const string FileMenuPath = @"Software\Classes\*\shell\NimbusCleaner";
        private const string DirectoryMenuPath = @"Software\Classes\Directory\shell\NimbusCleaner";
        private const string DisplayName = "Nimbus Cleaner ile Temizle";

        public static bool IsRegistered()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(FileMenuPath))
                return key != null;
        }

        public static void Register()
        {
            try
            {
                RegisterAt(FileMenuPath);
                RegisterAt(DirectoryMenuPath);
            }
            catch
            {
                try { Unregister(); } catch { }
                throw;
            }
        }

        public static void Unregister()
        {
            using (var classes = Registry.CurrentUser.OpenSubKey(@"Software\Classes", true))
            {
                if (classes == null) return;
                classes.DeleteSubKeyTree(@"*\shell\NimbusCleaner", false);
                classes.DeleteSubKeyTree(@"Directory\shell\NimbusCleaner", false);
            }
        }

        private static void RegisterAt(string path)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(path))
            {
                if (key == null) throw new InvalidOperationException("Kayıt defteri anahtarı oluşturulamadı.");
                key.SetValue(null, DisplayName, RegistryValueKind.String);
                key.SetValue("Icon", Application.ExecutablePath, RegistryValueKind.String);

                using (var command = key.CreateSubKey("command"))
                {
                    if (command == null) throw new InvalidOperationException("Komut kayıt defteri anahtarı oluşturulamadı.");
                    command.SetValue(null, BuildCommand(), RegistryValueKind.String);
                }
            }
        }

        private static string BuildCommand()
        {
            return "\"" + Application.ExecutablePath + "\" /cleanpath \"%1\"";
        }
    }
}
