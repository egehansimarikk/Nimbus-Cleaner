using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinCleaner
{
    [DataContract]
    internal sealed class GitHubRelease
    {
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }

        [DataMember(Name = "assets")]
        public List<GitHubReleaseAsset> Assets { get; set; }
    }

    [DataContract]
    internal sealed class GitHubReleaseAsset
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }

    internal sealed class UpdateCheckResult
    {
        public string CurrentVersionText { get; set; }
        public string RemoteVersionText { get; set; }
        public Version CurrentVersion { get; set; }
        public Version RemoteVersion { get; set; }
        public string InstallerUrl { get; set; }
        public string ErrorMessage { get; set; }

        public bool HasUpdate => CurrentVersion != null && RemoteVersion != null && RemoteVersion > CurrentVersion;
    }

    internal static class UpdateChecker
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/egehansimarikk/Nimbus-Cleaner/releases/latest";
        private const string InstallerFileName = "NimbusCleaner_Setup.exe";
        private static readonly HttpClient Client = CreateClient();

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("NimbusCleanerApp");
            return client;
        }

        public static async Task<UpdateCheckResult> CheckForUpdateAsync(string currentProductVersion)
        {
            SetSecurityProtocol();

            var currentVersionText = NormalizeVersion(currentProductVersion);
            Debug.WriteLine("Current App Version: " + (currentVersionText ?? "(missing)"));

            string json;
            using (var response = await Client.GetAsync(LatestReleaseUrl).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException("GitHub API returned HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase + ".");

                json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            GitHubRelease release;
            var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                release = serializer.ReadObject(stream) as GitHubRelease;

            if (release == null || string.IsNullOrEmpty(release.TagName))
                return CreateErrorResult(currentVersionText, "(missing)", "The GitHub response does not contain a tag_name.");

            var remoteVersionText = NormalizeVersion(release.TagName);
            Debug.WriteLine("GitHub Remote Version: " + (remoteVersionText ?? "(missing)"));

            Version currentVersion;
            if (!Version.TryParse(currentVersionText, out currentVersion))
                return CreateErrorResult(currentVersionText, remoteVersionText,
                    "Current app version could not be parsed: " + (currentVersionText ?? "(missing)"));

            Version remoteVersion;
            if (!Version.TryParse(remoteVersionText, out remoteVersion))
                return CreateErrorResult(currentVersionText, remoteVersionText,
                    "GitHub release version could not be parsed: " + (remoteVersionText ?? "(missing)"));

            var installerUrl = FindInstallerUrl(release);
            Debug.WriteLine("Matched Asset Download URL: " + (installerUrl ?? "(none)"));

            return new UpdateCheckResult
            {
                CurrentVersionText = currentVersionText,
                RemoteVersionText = remoteVersionText,
                CurrentVersion = currentVersion,
                RemoteVersion = remoteVersion,
                InstallerUrl = installerUrl
            };
        }

        public static async Task CheckAndUpdateAsync(IWin32Window owner, string currentProductVersion)
        {
            try
            {
                var result = await CheckForUpdateAsync(currentProductVersion);

                if (result.ErrorMessage != null)
                {
                    Debug.WriteLine("Update check skipped: " + result.ErrorMessage);
                    return;
                }

                if (!result.HasUpdate)
                {
                    Debug.WriteLine("No update available. Current: " + result.CurrentVersionText +
                        ", Remote: " + result.RemoteVersionText);
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.InstallerUrl))
                {
                    Debug.WriteLine("Update available, but no valid .exe asset was found. Current: " +
                        result.CurrentVersionText + ", Remote: " + result.RemoteVersionText);
                    return;
                }

                var answer = MessageBox.Show(owner,
                    "A new version (v" + result.RemoteVersionText + ") is available. Would you like to update now?",
                    "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (answer != DialogResult.Yes) return;

                var installerPath = await DownloadInstallerAsync(result.InstallerUrl);
                var installerProcess = Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
                if (installerProcess == null)
                    throw new InvalidOperationException("Windows did not start the downloaded installer.");

                Application.Exit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Update check or installation failed: " + ex);
            }
        }

        private static async Task<string> DownloadInstallerAsync(string installerUrl)
        {
            SetSecurityProtocol();

            Uri downloadUri;
            if (!Uri.TryCreate(installerUrl, UriKind.Absolute, out downloadUri) ||
                downloadUri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidDataException("The installer URL is not a valid HTTPS URL.");

            var filePath = Path.Combine(Path.GetTempPath(), InstallerFileName);
            try
            {
                using (var response = await Client.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException("Installer download returned HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase + ".");

                    using (var input = await response.Content.ReadAsStreamAsync())
                    using (var output = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                        await input.CopyToAsync(output);
                }

                return filePath;
            }
            catch
            {
                try { if (File.Exists(filePath)) File.Delete(filePath); } catch { }
                throw;
            }
        }

        private static UpdateCheckResult CreateErrorResult(string currentVersionText, string remoteVersionText, string errorMessage)
        {
            Debug.WriteLine("Update check error: " + errorMessage);
            return new UpdateCheckResult
            {
                CurrentVersionText = currentVersionText,
                RemoteVersionText = remoteVersionText,
                ErrorMessage = errorMessage
            };
        }

        private static string FindInstallerUrl(GitHubRelease release)
        {
            if (release == null || release.Assets == null) return null;

            var executableAssets = release.Assets
                .Where(asset => asset != null &&
                    !string.IsNullOrEmpty(asset.BrowserDownloadUrl) &&
                    !string.IsNullOrEmpty(asset.Name) &&
                    asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                    asset.BrowserDownloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                    Uri.TryCreate(asset.BrowserDownloadUrl, UriKind.Absolute, out var downloadUri) &&
                    downloadUri.Scheme == Uri.UriSchemeHttps)
                .Select(asset => new { asset.Name, Url = asset.BrowserDownloadUrl })
                .Where(asset => asset != null)
                .ToList();

            var preferredAsset = executableAssets.FirstOrDefault(asset =>
                asset.Name.IndexOf("setup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                asset.Name.IndexOf("installer", StringComparison.OrdinalIgnoreCase) >= 0);
            var selectedAsset = preferredAsset ?? executableAssets.FirstOrDefault();
            return selectedAsset == null ? null : selectedAsset.Url;
        }

        private static void SetSecurityProtocol()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
        }

        private static string NormalizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version)) return version;

            var normalizedVersion = version.Trim().TrimStart('v', 'V').Trim();
            var suffixIndex = normalizedVersion.IndexOfAny(new[] { '-', '+', '(' });
            if (suffixIndex >= 0)
                normalizedVersion = normalizedVersion.Substring(0, suffixIndex).Trim();

            return normalizedVersion;
        }
    }
}
