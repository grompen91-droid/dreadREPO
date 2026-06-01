using System;
using System.Collections.Generic;
using System.Globalization;

namespace Dread.Systems.AudioAssets
{
    [Serializable]
    internal class AudioManifestDto
    {
        public int schema;
        public string modVersion = "";
        public string releaseTag = "";
        public string baseUrl = "";
        public AudioManifestFileDto[] files = Array.Empty<AudioManifestFileDto>();
    }

    [Serializable]
    internal class AudioManifestFileDto
    {
        public string path = "";
        public string assetName = "";
        public int sizeBytes;
        public int priority;
        public string sha256 = "";
    }

    /// <summary>
    /// Manual JSON for audio-manifest (Unity JsonUtility does not deserialize files[]).
    /// </summary>
    internal static class AudioManifestJson
    {
        public static bool TryParse(string json, out AudioManifestDto? dto, out string error)
        {
            dto = null;
            error = "";
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Manifest JSON is empty";
                return false;
            }

            try
            {
                var files = ParseFilesArray(json);
                if (files.Count == 0)
                {
                    error = "Manifest parse produced no files";
                    return false;
                }

                var schema = ReadInt(json, "schema");
                if (schema != 1)
                {
                    error = $"Unsupported manifest schema {schema} (expected 1)";
                    return false;
                }

                dto = new AudioManifestDto
                {
                    schema = schema,
                    modVersion = ReadString(json, "modVersion"),
                    releaseTag = ReadString(json, "releaseTag"),
                    baseUrl = ReadString(json, "baseUrl"),
                    files = files.ToArray(),
                };
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static List<AudioManifestFileDto> ParseFilesArray(string json)
        {
            var keyIndex = json.IndexOf("\"files\"", StringComparison.Ordinal);
            if (keyIndex < 0)
                throw new InvalidOperationException("Manifest missing files array");

            var bracket = json.IndexOf('[', keyIndex);
            if (bracket < 0)
                throw new InvalidOperationException("Manifest files array not found");

            var list = new List<AudioManifestFileDto>();
            var i = bracket + 1;
            while (i < json.Length)
            {
                SkipWhitespace(json, ref i);
                if (i >= json.Length)
                    break;
                if (json[i] == ']')
                    break;
                if (json[i] == ',')
                {
                    i++;
                    continue;
                }

                if (json[i] != '{')
                    throw new InvalidOperationException("Expected file object in files array");

                var end = FindMatchingBrace(json, i);
                var objJson = json.Substring(i, end - i + 1);
                list.Add(ParseFileObject(objJson));
                i = end + 1;
            }

            return list;
        }

        private static AudioManifestFileDto ParseFileObject(string objJson)
        {
            return new AudioManifestFileDto
            {
                path = ReadString(objJson, "path"),
                assetName = ReadString(objJson, "assetName"),
                sizeBytes = ReadInt(objJson, "sizeBytes"),
                priority = ReadInt(objJson, "priority"),
                sha256 = ReadString(objJson, "sha256"),
            };
        }

        private static int ReadInt(string json, string key)
        {
            var raw = ReadRawValue(json, key);
            if (string.IsNullOrEmpty(raw))
                return 0;
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                return v;
            throw new InvalidOperationException($"Invalid integer for {key}");
        }

        private static string ReadString(string json, string key)
        {
            var raw = ReadRawValue(json, key);
            if (raw == null)
                return "";
            if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
                return UnescapeJsonString(raw.Substring(1, raw.Length - 2));
            return raw;
        }

        private static string? ReadRawValue(string json, string key)
        {
            var needle = "\"" + key + "\"";
            var keyIndex = json.IndexOf(needle, StringComparison.Ordinal);
            if (keyIndex < 0)
                return null;

            var i = keyIndex + needle.Length;
            SkipWhitespace(json, ref i);
            if (i >= json.Length || json[i] != ':')
                return null;
            i++;
            SkipWhitespace(json, ref i);
            if (i >= json.Length)
                return null;

            if (json[i] == '"')
            {
                var end = ReadQuotedStringEnd(json, i);
                return json.Substring(i, end - i + 1);
            }

            var start = i;
            while (i < json.Length && ",}\r\n\t ".IndexOf(json[i]) < 0)
                i++;
            return json.Substring(start, i - start);
        }

        private static int ReadQuotedStringEnd(string json, int quoteStart)
        {
            var i = quoteStart + 1;
            while (i < json.Length)
            {
                var c = json[i];
                if (c == '\\')
                {
                    i += 2;
                    continue;
                }

                if (c == '"')
                    return i;
                i++;
            }

            throw new InvalidOperationException("Unterminated JSON string");
        }

        private static string UnescapeJsonString(string s)
        {
            if (s.IndexOf('\\') < 0)
                return s;

            var chars = new List<char>(s.Length);
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c != '\\' || i + 1 >= s.Length)
                {
                    chars.Add(c);
                    continue;
                }

                var esc = s[++i];
                switch (esc)
                {
                    case '"':
                        chars.Add('"');
                        break;
                    case '\\':
                        chars.Add('\\');
                        break;
                    case '/':
                        chars.Add('/');
                        break;
                    case 'b':
                        chars.Add('\b');
                        break;
                    case 'f':
                        chars.Add('\f');
                        break;
                    case 'n':
                        chars.Add('\n');
                        break;
                    case 'r':
                        chars.Add('\r');
                        break;
                    case 't':
                        chars.Add('\t');
                        break;
                    case 'u' when i + 4 < s.Length:
                        var hex = s.Substring(i + 1, 4);
                        if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                        {
                            chars.Add((char)code);
                            i += 4;
                        }
                        else
                            chars.Add(esc);
                        break;
                    default:
                        chars.Add(esc);
                        break;
                }
            }

            return new string(chars.ToArray());
        }

        private static int FindMatchingBrace(string json, int openIndex)
        {
            var depth = 0;
            for (var i = openIndex; i < json.Length; i++)
            {
                var c = json[i];
                if (c == '{')
                    depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                        return i;
                }
                else if (c == '"')
                    i = ReadQuotedStringEnd(json, i);
            }

            throw new InvalidOperationException("Unterminated JSON object");
        }

        private static void SkipWhitespace(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
        }
    }
}
