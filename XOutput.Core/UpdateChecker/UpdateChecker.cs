using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using XOutput.Logging;

namespace XOutput.UpdateChecker
{
    public sealed class UpdateChecker : IDisposable
    {
        /// <summary>
        /// GitHub API URL of the latest release of this repository.
        /// </summary>
        private const string GithubURL = "https://api.github.com/repos/JCVERSA/XOutput-3.32/releases/latest";

        private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(UpdateChecker));
        private readonly HttpClient client = new HttpClient();

        public UpdateChecker()
        {
            // HttpClient on .NET 9 negotiates TLS 1.2+ by default; the legacy
            // ServicePointManager.SecurityProtocol setting is obsolete and has
            // no effect on HttpClient (SYSLIB0014).
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "XOutput/" + Version.AppVersion);
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        }

        /// <summary>
        /// Gets the latest release version string from the GitHub API response
        /// (the release tag with any leading 'v' removed).
        /// </summary>
        /// <param name="response">GitHub API response body</param>
        /// <returns>Version string</returns>
        private string GetLatestRelease(string response)
        {
            JObject json = JObject.Parse(response);
            string tag = json["tag_name"]?.ToString() ?? "";
            return tag.TrimStart('v');
        }

        /// <summary>
        /// Compares the current version with the latest release.
        /// </summary>
        /// <returns></returns>
        public async Task<VersionCompare> CompareRelease()
        {
            VersionCompare compare;
            HttpResponseMessage response = null;
            try
            {
                await logger.Debug("Getting " + GithubURL);
                response = await client.GetAsync(new Uri(GithubURL));
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();
                string latestRelease = GetLatestRelease(content);
                compare = Version.Compare(Version.AppVersion, latestRelease);
            }
            catch (Exception)
            {
                compare = VersionCompare.Error;
            }
            finally
            {
                response?.Dispose();
            }
            return await Task.Run(() => compare);
        }

        /// <summary>
        /// Releases all resources.
        /// </summary>
        public void Dispose()
        {
            client.Dispose();
        }
    }
}
