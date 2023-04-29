using Newtonsoft.Json;
using System;
using BackupManager.Domain.Hash;

namespace BackupManager.Infra.Serialization
{
    public class FilesHashesHandlerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FilesHashesHandler);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, typeof(FilesHashesHandler));
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value, typeof(FilesHashesHandler));
        }
    }
}