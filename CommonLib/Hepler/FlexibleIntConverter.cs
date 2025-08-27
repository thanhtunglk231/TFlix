using Newtonsoft.Json;
using System;
using System.Globalization;

namespace CommonLib.Helper
{
    /// <summary>
    /// Parse int/int? từ nhiều dạng: int, float (0.0), string ("0", "0.0", "  12 ").
    /// Null -> null cho int?, 0 cho int.
    /// Float -> TRUNCATE (cắt phần thập phân).
    /// </summary>
    public class FlexibleIntConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(int) || objectType == typeof(int?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // null token
            if (reader.TokenType == JsonToken.Null)
                return objectType == typeof(int) ? 0 : (int?)null;

            // integer
            if (reader.TokenType == JsonToken.Integer)
                return Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);

            // float -> truncate
            if (reader.TokenType == JsonToken.Float)
            {
                var d = Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture);
                return Convert.ToInt32(Math.Truncate(d));
            }

            // string
            if (reader.TokenType == JsonToken.String)
            {
                var s = (reader.Value?.ToString() ?? "").Trim();
                if (string.IsNullOrEmpty(s))
                    return objectType == typeof(int) ? 0 : (int?)null;

                if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    return i;

                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    return Convert.ToInt32(Math.Truncate(d));
            }

            // fallback an toàn
            return objectType == typeof(int) ? 0 : (int?)null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null) { writer.WriteNull(); return; }
            writer.WriteValue(Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

    }
}
