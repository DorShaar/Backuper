﻿namespace Backuper.App.Serialization
{
    public interface IObjectSerializer
    {
        void Serialize<T>(T objectToSerialize, string outputPath);
        T Deserialize<T>(string inputPath);
    }
}