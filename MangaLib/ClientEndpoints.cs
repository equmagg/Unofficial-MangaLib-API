using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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

            string url = Client.MangaLibApiBaseAddress + "api/auth/me";

            var client = this;

            client.LogMessage(nameof(CheckAuthorizationTokenAsync), url);

            try
            {
                HttpResponseMessage response = await client.HttpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();

                    client.LogMessage(nameof(CheckAuthorizationTokenAsync), responseBody);

                    if (responseBody.Contains("{\"data\":{\"id\":") && !responseBody.Contains("{\"data\":{\"popular\":[{\""))
                        return true;
                    else
                        return false;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    client.LogWarning(nameof(CheckAuthorizationTokenAsync), errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                client.LogWarning(nameof(CheckAuthorizationTokenAsync), ex.Message);
                return false;
            }
        }
        public async Task<ulong> GetUserIdOrDefault(string token)
            => (await GetUserBaseDataAsync(token))?.Id ?? 0;
        private async Task<Me?> GetUserBaseDataAsync(string token) => await GetUserBaseDataAsync(this, token);
        private static async Task<Me?> GetUserBaseDataAsync(Client client, string token)
        {
            try
            {
                string url = Client.MangaLibApiBaseAddress + "api/auth/me";
                HttpResponseMessage response = await client.HttpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    client.LogMessage(nameof(GetUserBaseDataAsync), responseBody);
                    return System.Text.Json.JsonSerializer.Deserialize<ApiEnvelope<Me>>(responseBody, JsonCfg.Options)?.Data;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    client.LogWarning(nameof(GetUserBaseDataAsync), "Request not successfull. Responce body: " + errorContent);
                    return null;
                }
            }
            catch (Exception ex) 
            {
                client.LogError(nameof(GetUserBaseDataAsync), ex.ToString());
                return null; 
            }
        }
        public async Task<MangaLib.User?> GetUserAsync(ulong userId) => await GetUserAsync(this, userId);
        public static async Task<MangaLib.User?> GetUserAsync(Client client, ulong userId)
        {
            string url = Client.MangaLibApiBaseAddress + $"api/user/{userId}?" +
                $"fields[]=background&fields[]=roles&fields[]=points&fields[]=ban_info&fields[]=gender" +
                $"&fields[]=created_at&fields[]=about&fields[]=teams&fields[]=premium_background_id";
            client.LogMessage(nameof(GetUserAsync), "Id: " + userId);
            try
            {
                HttpResponseMessage response = await client.HttpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    client.LogMessage(nameof(GetUserAsync), responseBody);
                    return System.Text.Json.JsonSerializer.Deserialize<ApiEnvelope<MangaLib.User>>(responseBody, JsonCfg.Options)?.Data;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    client.LogWarning(nameof(GetUserAsync), "Responce returned an error:" + errorContent);
                    return null;
                }
            }
            catch (Exception ex) 
            { 
                client.LogError(nameof(GetUserAsync), ex.ToString());
                return null; 
            }
        }
        public async Task<uint> GetChapterIdOrDefaultAsync(string slug, uint chapter, uint volume = 1) 
            => (uint?)((await GetChapterAsync(slug, chapter, volume))?.Id) ?? 0u;
        public async Task<Chapter?> GetChapterAsync(string slug, uint chapter, uint volume = 1) => await GetChapterAsync(this, slug, chapter, volume);
        public static async Task<Chapter?> GetChapterAsync(Client client, string slug, uint chapter, uint volume = 1)
        {
            var url = Client.MangaLibApiBaseAddress + $"api/manga/{slug}/chapter?number={chapter}&volume={volume}";

            try
            {
                HttpResponseMessage response = await client.HttpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    client.LogMessage(nameof(GetUserAsync), responseBody);
                    return System.Text.Json.JsonSerializer.Deserialize<ApiEnvelope<Chapter>>(responseBody, JsonCfg.Options)?.Data;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    client.LogWarning(nameof(GetChapterAsync), "Responce returned an error:" + errorContent);
                    return null;
                }
            }
            catch (Exception ex) 
            {
                client.LogError(nameof(GetChapterAsync), ex.ToString());
                return null; 
            }
        }
        /// <summary> returns parsed title (manga or book) information by url. </summary>
        public async Task<MangaTitle?> GetTitleByUrlAsync(string url) => await GetTitleAsync(Client.GetSlugFromUrl(url));

        /// <summary> returns parsed title (manga or book) information by slug. </summary>
        public async Task<MangaTitle?> GetTitleAsync(string slug)
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
        public async Task<List<MangaTitle>?> GetNewTitlesAsync() => await GetNewTitlesAsync(this);
        public async static Task<List<MangaTitle>?> GetNewTitlesAsync(Client client)
        {
            string url = Client.MangaLibApiBaseAddress + $"api/manga?fields[]=rate&fields[]=rate_avg&fields[]=userBookmark" +
                $"&page=1&seed=5aa5238cf8bc4a759760ced27bc22b76&site_id[]=3&sort_by=created_at&types[]=14";

            try
            {
                using HttpResponseMessage response = await client.HttpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    client.LogError(nameof(GetNewTitlesAsync), "Request error. Status code: " + response.StatusCode);
                    StringBuilder sb = new();
                    foreach (var header in response.Headers)
                        sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
                    client.LogMessage(nameof(GetNewTitlesAsync), "Responce headers: \n" + sb);

                    string errorContent = await response.Content.ReadAsStringAsync();
                    client.LogMessage(nameof(GetNewTitlesAsync), "Responce body: \n" + errorContent);
                    return null;
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                var res = JsonSerializer
                    .Deserialize<ApiEnvelope<List<MangaTitle>>>(responseBody, JsonCfg.Options)
                    ?.Data;

                if (res == null)
                {
                    client.LogError(nameof(GetNewTitlesAsync), "Unable to deserialize:\n{responseBody}");
                }

                return res;
            }
            catch (JsonException ex)
            {
                client.LogError(nameof(GetNewTitlesAsync), "JSON error: " + ex);
                return null;
            }
            catch (Exception ex)
            {
                client.LogError(nameof(GetNewTitlesAsync), ex.ToString());
                return null;
            }
        }
        public async Task AddBookmarkAsync(string slug, string token, uint bookmark = 1) => await AddBookmarkAsync(this, slug, token, bookmark);
        public async static Task AddBookmarkAsync(Client client, string slug, string token, uint bookmark = 1)
        {
            string url = Client.MangaLibApiBaseAddress + "api/bookmarks"; 
            var data = new
            {
                bookmark = new { status = bookmark.ToString() },
                media_slug = $"{slug}",
                media_type = "manga",
                meta = new { }
            };


            try
            {
                HttpResponseMessage response = await client.HttpClient.PostAsJsonAsync(url, data);

                if (response.IsSuccessStatusCode)
                {
                    client.LogMessage(nameof(AddBookmarkAsync), $"{slug} add successfully");
                    string responseBody = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    client.LogWarning(nameof(AddBookmarkAsync), 
                        $"{slug} Error: " + response.StatusCode + "   " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                client.LogError(nameof(AddBookmarkAsync), "Request error: " + ex.Message);
            }

        }
        public async Task AddVoteAsync(string slug, string token, uint vote = 10) => await AddVoteAsync(this, slug, token, vote);
        public async static Task AddVoteAsync(Client client, string slug, string token, uint vote = 10)
        {
            var match = Regex.Match(slug, @"^(\d+)--");
            string url = Client.MangaLibApiBaseAddress + "api/manga/rate";

            if (!match.Success)
            {
                client.LogError(nameof(AddVoteAsync), $"ID not found for slug {slug}");
            }
            string id = match.Groups[1].Value;
            var data = new
            {
                rateable_id = id,
                score = vote,
                rateable_type = "manga"
            };
            client.LogMessage(nameof(AddVoteAsync), $"{slug}  ID: " + id);


            try
            {
                HttpResponseMessage response = await client.HttpClient.PostAsJsonAsync(url, data);

                if (response.IsSuccessStatusCode)
                {
                    client.LogMessage(nameof(AddVoteAsync), "voted successfully");
                    string responseBody = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    client.LogWarning(nameof(AddVoteAsync), "Error: " + response.StatusCode + "   " + await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                client.LogError(nameof(AddVoteAsync), "Request error: " + ex.Message);
            }


        }
    }
}
