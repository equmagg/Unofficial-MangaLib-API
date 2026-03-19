using System;
using System.Collections.Generic;
using System.Text;

namespace MangaLib
{
    internal class Endpoints
    {
        internal async static Task<bool> CheckAuthorizationTokenAsync(string token)
        {
            var client = new global::MangaLib.Client(token);

            return await client.CheckAuthorizationTokenAsync();
        }
    }
}
