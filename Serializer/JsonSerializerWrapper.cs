using FileHashes;
using Newtonsoft.Json;
using Serializer.Interface;
using System;
using System.IO;

namespace Serializer
{
    public class JsonSerializerWrapper : IObjectSerializer
    {
        public void Serialize<T>(T objectToSerialize, string databasePath)
        {
            string jsonText = JsonConvert.SerializeObject(objectToSerialize, Formatting.Indented);
            File.WriteAllText(databasePath, jsonText);
        }

        public T Deserialize<T>(string databasePath)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new FilesHashesHandlerConverter());
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(databasePath), settings);
        }

        private class FilesHashesHandlerConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(FilesHashesHandler);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return serializer.Deserialize(reader, typeof(FilesHashesHandler));
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, value, typeof(FilesHashesHandler));
            }
        }
    }
}