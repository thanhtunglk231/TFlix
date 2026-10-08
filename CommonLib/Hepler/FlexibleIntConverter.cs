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
            => objectType == typeof(int) || objectType == typeof(int?) ||
               objectType == typeof(long) || objectType == typeof(long?) ||
               objectType == typeof(decimal) || objectType == typeof(decimal?);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            bool isDecimal = objectType == typeof(decimal) || objectType == typeof(decimal?);
            bool isLong = objectType == typeof(long) || objectType == typeof(long?);

            // null token
            if (reader.TokenType == JsonToken.Null)
            {
                if (isDecimal) return objectType == typeof(decimal) ? 0m : (decimal?)null;
                if (isLong) return objectType == typeof(long) ? 0L : (long?)null;
                return objectType == typeof(int) ? 0 : (int?)null;
            }

            // integer
            if (reader.TokenType == JsonToken.Integer)
            {
                if (isDecimal) return Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture);
                if (isLong) return Convert.ToInt64(reader.Value, CultureInfo.InvariantCulture);
                return Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
            }

            // float
            if (reader.TokenType == JsonToken.Float)
            {
                var d = Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture);
                if (isDecimal) return d;
                if (isLong) return Convert.ToInt64(Math.Truncate(d));
                return Convert.ToInt32(Math.Truncate(d));
            }

            // string
            if (reader.TokenType == JsonToken.String)
            {
                var s = (reader.Value?.ToString() ?? "").Trim();
                if (string.IsNullOrEmpty(s))
                {
                    if (isDecimal) return objectType == typeof(decimal) ? 0m : (decimal?)null;
                    if (isLong) return objectType == typeof(long) ? 0L : (long?)null;
                    return objectType == typeof(int) ? 0 : (int?)null;
                }

                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                {
                    if (isDecimal) return d;
                    if (isLong) return Convert.ToInt64(Math.Truncate(d));
                    return Convert.ToInt32(Math.Truncate(d));
                }
            }

            // fallback an toàn
            if (isDecimal) return objectType == typeof(decimal) ? 0m : (decimal?)null;
            if (isLong) return objectType == typeof(long) ? 0L : (long?)null;
            return objectType == typeof(int) ? 0 : (int?)null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null) { writer.WriteNull(); return; }
            if (value is decimal dec)
            {
                writer.WriteValue(dec);
                return;
            }
            if (value is long longValue)
            {
                writer.WriteValue(longValue);
                return;
            }
            writer.WriteValue(Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }

    }
}
