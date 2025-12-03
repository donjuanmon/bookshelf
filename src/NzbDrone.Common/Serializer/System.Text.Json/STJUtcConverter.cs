using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Common.Serializer
{
    public class STJUtcConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();

            // Handle BC dates (before year 1 AD) which DateTime cannot represent
            // Return DateTime.MinValue for ancient dates
            if (!string.IsNullOrWhiteSpace(dateString) && dateString.Contains(" BC", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.MinValue;
            }

            // Try to parse the date, returning MinValue if parsing fails
            if (DateTime.TryParse(dateString, out var result))
            {
                return result.ToUniversalTime();
            }

            return DateTime.MinValue;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssZ"));
        }
    }
}
