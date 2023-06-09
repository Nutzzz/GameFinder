using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;

#if NET5_0_OR_GREATER
using System.Net.Http.Json;
#endif

namespace GameFinder.Deprecated
{
    internal static class Utils
    {
        // ReSharper disable once MemberCanBePrivate.Global
        internal static readonly JsonSerializerOptions DefaultSerializerOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
        };
        
        internal static bool ContainsCaseInsensitive(this string s, string value)
        {
            return s.Contains(value, StringComparison.OrdinalIgnoreCase);
        }

        internal static DateTime ToDateTime(this long timeStamp)
        {
            var offset = DateTimeOffset.FromUnixTimeSeconds(timeStamp);
            return offset.UtcDateTime;
        }

        internal static string RemoveQuotes(this string s)
        {
            var pos1 = s.IndexOf('"');
            var pos2 = s.LastIndexOf('"');

            if (pos1 == -1 || pos2 == -1) return s;

            var newString = s.Substring(pos1+1, pos2-1);
            return newString;
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
        internal static async Task<T?> FromJsonAsync<T>(string url) where T : class
        {
            using var client = new HttpClient();
#if NET5_0_OR_GREATER
            var res = await client.GetFromJsonAsync<T>(url, DefaultSerializerOptions).ConfigureAwait(false);
            return res;
#endif
#if NETSTANDARD2_1
            var text = await client.GetStringAsync(url);
            return FromJsonString<T>(text);
#endif
        }
        
        internal static T? FromJson<T>(string file) where T : class
        {
            if (!File.Exists(file))
                throw new FileNotFoundException(null, file);

            var jsonText = File.ReadAllText(file, Encoding.UTF8);
            return FromJsonString<T>(jsonText);
        }

        [UnconditionalSuppressMessage(
            "Trimming",
            "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
        internal static T? FromJsonString<T>(string jsonText) where T : class
        {
                var value = JsonSerializer.Deserialize<T>(jsonText, DefaultSerializerOptions);
                return value;
        }
        
        internal static void DoNoThrow(Action a)
        {
            try
            {
                a();
            }
            catch (Exception)
            {
                //ignored
            }
        }
    }
}
