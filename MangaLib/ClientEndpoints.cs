using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MangaLib
{
    public partial class Client
    {
        /// <summary> Checks if authorization token is authorized by ManglaLib. </summary>
        /// <remarks> They do not return errors on invalid tokens, so we utilize auth/me and search for account-specific info. </remarks>
        public async Task<bool> CheckAuthorizationTokenAsync()
        {
            if(!this.AuthorizationToken.Exists)
                return false;

            string url = global::MangaLib.Client.MangaLibApiBaseAddress + "api/auth/me";

            var client = this;

            try
            {
                HttpResponseMessage response = await client.HttpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    if (responseBody.Contains("{\"data\":{\"id\":") && !responseBody.Contains("{\"data\":{\"popular\":[{\""))
                        return true;
                    else
                        return false;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary> returns parsed title (manga or book) information by url. </summary>
        public async Task<MangaTitle?> GetTitleByUrl(string url) => await GetTitle(Client.GetSlugFromUrl(url));

        /// <summary> returns parsed title (manga or book) information by slug. </summary>
        public async Task<MangaTitle?> GetTitle(string slug)
        {
            try
            {
                string response = await this.GetTitleRawAsync(slug);
                var title = MangaLib.MangaParsers.ParseTitle(response);
                if (title is null) throw new JsonException("Cannot deserialize Mangalib title");
                return title;
            }
            catch (Exception) 
            { 
                return null; 
            }
        }
        /// <summary> returns raw json string of the selected Manga or Book title. </summary> 
        /// <exception cref="HttpRequestException"></exception>
        public async Task<string> GetTitleRawAsync(string slug)
        {
            var path = $"/api/manga/{slug}";
            var query =
                "fields[]=background&fields[]=eng_name&fields[]=otherNames&fields[]=summary" +
                "&fields[]=releaseDate&fields[]=type_id&fields[]=caution&fields[]=views&fields[]=close_view" +
                "&fields[]=rate_avg&fields[]=rate&fields[]=genres&fields[]=tags&fields[]=teams&fields[]=user" +
                "&fields[]=franchise&fields[]=authors&fields[]=publisher&fields[]=userRating&fields[]=moderated" +
                "&fields[]=metadata&fields[]=metadata.count&fields[]=metadata.close_comments&fields[]=manga_status_id" +
                "&fields[]=chap_count&fields[]=status_id&fields[]=artists&fields[]=format";

            var uri = new Uri($"{path}?{query}", UriKind.Relative);

            using var req = new HttpRequestMessage(HttpMethod.Get, uri);

            using var resp = await this.HttpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var sb = new StringBuilder();
                sb.AppendLine($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
                sb.AppendLine("Request URI: " + resp.RequestMessage!.RequestUri);
                sb.AppendLine("Response headers:");
                foreach (var h in resp.Headers) sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");
                foreach (var h in resp.Content.Headers) sb.AppendLine($"{h.Key}: {string.Join(", ", h.Value)}");
                sb.AppendLine();
                sb.AppendLine(body);
                throw new HttpRequestException(sb.ToString());
            }

            return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}
