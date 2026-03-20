using System;
using System.Collections.Generic;
using System.Text;

namespace MangaLib
{
    public readonly struct AuthorizationToken : IEquatable<AuthorizationToken>
    {
        /// <summary> raw representation without Bearer prefix. </summary>
        private readonly string? _authorizationToken = null;
        private AuthorizationToken(string? authorizationToken)
        {
            _authorizationToken = authorizationToken;
        }
        public static AuthorizationToken CreateWithoutBearer(string? authorizationToken)
            => new AuthorizationToken(authorizationToken);
        public readonly static AuthorizationToken NullInstance = new AuthorizationToken(null);
        /// <summary> checks if token is not null and not empty. </summary>
        public bool Exists => !string.IsNullOrEmpty(_authorizationToken);
        /// <summary> raw value as a string without Bearer prefix. </summary>
        public string? TokenString => this.ToString();
        /// <summary> Internal raw value of an inner _authorizationToken field. </summary>
        internal string? TokenRaw => _authorizationToken;


        public override string? ToString()
        {
            return this.Exists ? "Bearer" + _authorizationToken!.Trim() : null;
        }

        public bool Equals(AuthorizationToken other)
        {
            return string.Equals(_authorizationToken, other._authorizationToken, StringComparison.Ordinal);
        }
        public override bool Equals(object? obj)
        {
            return obj is AuthorizationToken other && Equals(other);
        }
        public override int GetHashCode()
        {
            return _authorizationToken?.GetHashCode() ?? 0;
        }
        public static bool operator ==(AuthorizationToken left, AuthorizationToken right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(AuthorizationToken left, AuthorizationToken right) => !(left == right);
    }
}
