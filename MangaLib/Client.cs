using System.Net;
using System.Text.RegularExpressions;

namespace MangaLib
{
    public partial class Client
    {
        /// <summary> Unauthorized insatance of MangaLib client. </summary>
        public static Client Unauthorized = new global::MangaLib.Client();

        /// <summary> Base url of the Mangalib API. By default <c>https://api.cdnlibs.org/</c> </summary>
        /// <remarks> 
        /// MangaLib changes its API time to time, so you might want to override it yourself if it was changed. Overwise leave it as is. 
        /// </remarks>
        public static string MangaLibApiBaseAddress
        {
            get
            {
                return field;
            }
            set
            {
                // invalid url will throw anyway, catch early
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(nameof(value), "ApiBase can not be empty.");
                }
                if (!value.StartsWith("https://") || value[^1] != '/' || value.Any(c => char.IsWhiteSpace(c))) // https://(.*)/ form
                {
                    throw new ArgumentException(nameof(value), "ApiBase url is invalid.");
                }
                field = value;
            }
        } = "https://api.cdnlibs.org/";
        public Client(AuthorizationToken authorizationToken, WebProxy? proxy = null) : this(authorizationToken.TokenString, proxy) { }
        public Client(string? authorizationToken = null, WebProxy? proxy = null)
        {
            HttpClient = CreateHttpClient(authorizationToken);
            Proxy = proxy;
        }

        private readonly CookieContainer CdnLibsCookies = new();
        public readonly HttpClient HttpClient;
        public readonly WebProxy? Proxy = null;

        public AuthorizationToken _authorizationToken = AuthorizationToken.NullInstance;
        /// <summary> Authorization token instance. </summary>
        public AuthorizationToken AuthorizationToken => _authorizationToken; 
        /// <summary> Token with Bearer prefix included. </summary>
        /// <exception cref="ArgumentException"></exception>
        public string? AuthorizationTokenString
        { 
            get 
            { 
                return _authorizationToken.TokenString; 
            }
            set
            {
                if(value == null) 
                {
                    _authorizationToken = AuthorizationToken.NullInstance;
                    return;
                }
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException(nameof(value), "AuthorizationToken was empty.");
                }
                if (!value.Contains("Bearer "))
                {
                    throw new ArgumentException(nameof(value), "AuthorizationToken must contain Bearer prefix.");
                }
                _authorizationToken = AuthorizationToken.CreateWithoutBearer(value.Replace("Bearer ", "").Trim());
            }
        }
        /// <summary> Raw token representation without bearer. </summary>
        /// <exception cref="ArgumentException"></exception>
        public string? AuthorizationTokenStringWithoutBearer
        {
            get
            {
                return _authorizationToken.TokenRaw;
            }
            set
            {
                if (value != null && value.Contains("Bearer"))
                {
                    throw new ArgumentException(nameof(value), "Raw token set should not include Bearer.");
                }
                _authorizationToken = global::MangaLib.AuthorizationToken.CreateWithoutBearer(value);
            }
        }
        /// <summary> checks if token is not null and not empty. </summary>
        public bool AuthrizationTokenExists => _authorizationToken.Exists;

        private HttpClient CreateHttpClient(string? authorizationToken)
        {
            var handler = new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                UseCookies = true,
                CookieContainer = CdnLibsCookies,
                AllowAutoRedirect = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),

                UseProxy = false,
            };

            var http = new HttpClient(handler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(20),
                BaseAddress = new Uri(MangaLibApiBaseAddress)
            };

            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36");

            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru,en-US;q=0.9,en;q=0.8");

            http.DefaultRequestHeaders.Referrer = new Uri("https://ranobelib.me/");
            http.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://ranobelib.me");
            http.DefaultRequestHeaders.TryAddWithoutValidation("site-id", "3");
            http.DefaultRequestHeaders.TryAddWithoutValidation("client-time-zone", "Europe/Berlin");
            http.DefaultRequestHeaders.TryAddWithoutValidation("DNT", "1");

            if(!string.IsNullOrEmpty(authorizationToken))
                http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationToken);

            return http;
        }

        public static string GetSlugFromUrl(string url)
        {
            string slug = string.Empty;
            if (string.IsNullOrEmpty(url)) 
                return slug;

            string patternWithDigitsLong = @"^https://ranobelib.me/ru/book/(\d+--[^/?#]+)";
            string patternWithoutDigitsLong = @"^https://ranobelib.me/ru/book/([^/?#]+)";
            string patternWithDigits = @"^https://ranobelib.me/(?!ru/)(?!.*people/)(?!.*team/)(\d+--[^/?#]+)";
            string patternWithoutDigits = @"^https://ranobelib.me/(?!ru/)(?!.*people/)(?!.*team/)([^/?#]+)";
            string patternWithDigitsManga = @"^https://mangalib.me/(\d+--[^/?#]+)";
            string patternWithoutDigitsManga = @"^https://mangalib.me/([^/?#]+)";


            Match match = Regex.Match(url, patternWithDigitsLong);
            if (!match.Success)
                match = Regex.Match(url, patternWithoutDigitsLong);
            if (!match.Success)
                match = Regex.Match(url, patternWithDigits);
            if (!match.Success)
                match = Regex.Match(url, patternWithoutDigits);
            if (!match.Success)
                match = Regex.Match(url, patternWithDigitsManga);
            if (!match.Success)
                match = Regex.Match(url, patternWithoutDigitsManga);

            if (match.Success)
            {
                slug = match.Groups[1].Value;
            }

            return slug;
        }
    }
}
