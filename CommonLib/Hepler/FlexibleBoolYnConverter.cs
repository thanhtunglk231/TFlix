using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLib.Hepler
{
    public class FlexibleBoolYnConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(bool) || t == typeof(bool?);

        public override object ReadJson(JsonReader r, Type t, object v, JsonSerializer s)
        {
            if (r.TokenType == JsonToken.Null) return t == typeof(bool) ? false : (bool?)null;
            if (r.TokenType == JsonToken.Boolean) return (bool)r.Value;
            if (r.TokenType == JsonToken.Integer) return Convert.ToInt32(r.Value) != 0;

            if (r.TokenType == JsonToken.String)
            {
                var sVal = (r.Value?.ToString() ?? "").Trim().ToLowerInvariant();
                if (sVal is "y" or "yes" or "true" or "1") return true;
                if (sVal is "n" or "no" or "false" or "0" or "") return false;
            }
            return t == typeof(bool) ? false : (bool?)null;
        }

        public override void WriteJson(JsonWriter w, object value, JsonSerializer s)
        { if (value == null) w.WriteNull(); else w.WriteValue((bool)value); }
    }
}
