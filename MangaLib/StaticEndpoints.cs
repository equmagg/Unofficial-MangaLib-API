using System;
using System.Collections.Generic;
using System.Text;

namespace MangaLib
{
    public class Endpoints
    {
        public async static Task<bool> CheckAuthorizationTokenAsync(string token)
        {
            var client = new global::MangaLib.Client(token);

            return await client.CheckAuthorizationTokenAsync();
        }

        public async Task<List<MangaTitle>?> GetNewTitlesAsync() 
            => await global::MangaLib.Client.GetNewTitlesAsync(global::MangaLib.Client.Unauthorized);
    }
}
