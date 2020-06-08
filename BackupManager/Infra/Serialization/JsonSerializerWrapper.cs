using Backuper.App.Serialization;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Backuper.Infra.Serialization
{
    public class JsonSerializerWrapper : IObjectSerializer
    {
        public void Serialize<T>(T objectToSerialize, string databasePath)
        {
            string jsonText = JsonConvert.SerializeObject(objectToSerialize, Formatting.Indented);
            File.WriteAllText(databasePath, jsonText);
            Console.WriteLine($"Serialized object {typeof(T)} into {databasePath}");
        }

        public T Deserialize<T>(string databasePath)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new FilesHashesHandlerConverter());

            T deserializedObject = JsonConvert.DeserializeObject<T>(File.ReadAllText(databasePath), settings);
            Console.WriteLine($"Deserialized object {typeof(T)} from {databasePath}");

            return deserializedObject;
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