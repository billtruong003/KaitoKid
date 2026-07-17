#nullable enable
// Editor-only HMAC-SHA256 token signer. Mirrors arena-server/src/auth/tokens.ts
// verbatim per arena-unity/server-extract-2026-05-15.md §D.
//
// Lives under Assets/.../Editor/ — Unity compiles Editor folders into
// Assembly-CSharp-Editor, which is excluded from player builds automatically.
// So the HMAC secret + this code NEVER ship in WebGL.
//
// Spec contract (must match server byte-for-byte):
//   payload = JSON({ session_id, discord_id, expires_at })   field-ordered
//   payloadB64 = base64url(payload)                          +/-/_/strip=
//   sig = HMAC-SHA256(payloadB64-bytes, secret-bytes) → hex lowercase
//   token = `${payloadB64}.${sigHex}`

using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RadiantArena.Editor
{
    public static class DevTokenSigner
    {
        /// <summary>
        /// Mint a signed token. Throws if secret is empty.
        /// </summary>
        public static string SignToken(string sessionId, string discordId, long expiresAtUnixMs, string secret)
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("secret must be non-empty", nameof(secret));

            // JObject with explicit add order locks field order to match Node's JSON.stringify
            // for { session_id, discord_id, expires_at }.
            var payload = new JObject();
            payload["session_id"] = sessionId ?? "";
            payload["discord_id"] = discordId ?? "";
            payload["expires_at"] = expiresAtUnixMs;
            var json = payload.ToString(Formatting.None);

            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var payloadB64 = Base64UrlEncode(jsonBytes);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var sigBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
            var sigHex = ToHexLower(sigBytes);

            return $"{payloadB64}.{sigHex}";
        }

        static string Base64UrlEncode(byte[] bytes)
        {
            var s = Convert.ToBase64String(bytes);
            return s.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        static string ToHexLower(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
