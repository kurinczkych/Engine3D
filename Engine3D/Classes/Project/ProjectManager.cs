using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenTK.Mathematics;

namespace Engine3D
{
    public static class ProjectManager
    {
        public class Project
        {
            [JsonProperty("object_id")]
            public int objectID = 1;
            [JsonProperty("objects")]
            public List<Object> objects;
            [JsonProperty("_meshObjects")]
            public List<Object> _meshObjects;
            [JsonProperty("_instObjects")]
            public List<Object> _instObjects;
            //[JsonProperty("character")]
            //public Character character;
            //[JsonProperty("particleSystems")]
            //public List<ParticleSystem> particleSystems;
        }

        public static void CheckMatrix4ConvertersInNamespace(string namespaceToCheck)
        {
            // Get all types in the currently loaded assemblies that are in the specified namespace
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(assembly => assembly.GetTypes())
                            .Where(type => type.Namespace == namespaceToCheck)
                            .ToList();

            List<string> errors = new List<string>();
            foreach (var type in allTypes)
            {
                //Engine.consoleManager.AddLog($"\nChecking type: {type.FullName}", LogType.Error);

                // Check fields
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(Matrix4))
                    {
                        bool hasJsonConverter = field.GetCustomAttributes(typeof(JsonConverterAttribute), false).Any();
                        bool hasJsonIgnore = field.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any();

                        if(!hasJsonConverter && !hasJsonIgnore)
                            errors.Add($"Field: {field.Name} | Has JsonConverter: {hasJsonConverter} | Has JsonIgnore: {hasJsonIgnore}");
                            //Engine.consoleManager.AddLog(, LogType.Error);
                    }
                }

                // Check properties
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var property in properties)
                {
                    if (property.PropertyType == typeof(Matrix4))
                    {
                        bool hasJsonConverter = property.GetCustomAttributes(typeof(JsonConverterAttribute), false).Any();
                        bool hasJsonIgnore = property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any();

                        if (!hasJsonConverter && !hasJsonIgnore)
                            errors.Add($"Property: {property.Name} | Has JsonConverter: {hasJsonConverter} | Has JsonIgnore: {hasJsonIgnore}");
                    }
                }
            }

            Engine.consoleManager.AddLog("Errors: ", LogType.Error);
            foreach(string error in errors)
            {
                Engine.consoleManager.AddLog(error, LogType.Error);
            }
        }

        public static void SaveObj(string filePath, object obj)
        {
            string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public static void Save(string filePath, Project project)
        {
            //CheckMatrix4ConvertersInNamespace("Engine3D");
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.Indented;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            using (StreamWriter file = File.CreateText("large_project.json"))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    // Optional: Configure formatting and other settings
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                };

                // Serialize the object directly to the file stream
                serializer.Serialize(writer, project);
            }

            //string json = JsonConvert.SerializeObject(project, settings);
            //byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            //byte[] compressedData = Compress(jsonBytes);

            //File.WriteAllBytes("save.bin", compressedData);
            //File.WriteAllText("save.sav", json);
        }

        public static Project? Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                byte[] compressedData = File.ReadAllBytes(filePath);
                byte[] decompressedData = Decompress(compressedData);
                string decompressedJson = Encoding.UTF8.GetString(decompressedData);
                Project? project = JsonConvert.DeserializeObject<Project>(decompressedJson);

                return project;
            }
            else
                return null;
        }

        public static byte[] Compress(byte[] data)
        {
            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(compressedStream, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return compressedStream.ToArray();
            }
        }

        // Method to decompress data using GZip
        public static byte[] Decompress(byte[] data)
        {
            using (MemoryStream compressedStream = new MemoryStream(data))
            {
                using (GZipStream gzip = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    using (MemoryStream resultStream = new MemoryStream())
                    {
                        gzip.CopyTo(resultStream);
                        return resultStream.ToArray();
                    }
                }
            }
        }
    }
}
