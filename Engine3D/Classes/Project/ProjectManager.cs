using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using static Engine3D.ProjectManager;

namespace Engine3D
{
    public static class ProjectManager
    {
        public class Project
        {
            public int objectID = 1;
            public List<Object> objects;
            public Character character;

            [JsonIgnore]
            public List<Object> _meshObjects;
            [JsonIgnore]
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
            using (StreamWriter file = File.CreateText(filePath))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    ContractResolver = new AllPropertiesContractResolver(),
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto
                };
                serializer.Converters.Add(new Vector2Converter());
                serializer.Converters.Add(new Vector3Converter());
                serializer.Converters.Add(new QuaternionConverter());
                serializer.Converters.Add(new Matrix4Converter());
                serializer.Converters.Add(new Color4Converter());

                serializer.Serialize(writer, obj);
            }
        }

        public static void Save(string filePath, Project project)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
            using (StreamWriter streamWriter = new StreamWriter(gzipStream))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
            {
                JsonSerializer serializer = new JsonSerializer
                {
                    ContractResolver = new AllPropertiesContractResolver(),
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto
                };

                serializer.Converters.Add(new Vector2Converter());
                serializer.Converters.Add(new Vector3Converter());
                serializer.Converters.Add(new QuaternionConverter());
                serializer.Converters.Add(new Matrix4Converter());
                serializer.Converters.Add(new Color4Converter());
                //serializer.Converters.Add(new ComponentConverter());

                serializer.Serialize(jsonWriter, project);
            }
        }

        public static Project? Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Engine.consoleManager.AddLog("File not found: " + filePath, LogType.Error);
                return null;
            }

            try
            {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                using (StreamReader streamReader = new StreamReader(gzipStream))
                using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
                {
                    JsonSerializer serializer = new JsonSerializer
                    {
                        ContractResolver = new AllPropertiesContractResolver(),
                        Formatting = Formatting.Indented,
                        TypeNameHandling = TypeNameHandling.Auto
                    };

                    serializer.Converters.Add(new Vector2Converter());
                    serializer.Converters.Add(new Vector3Converter());
                    serializer.Converters.Add(new QuaternionConverter());
                    serializer.Converters.Add(new Matrix4Converter());
                    serializer.Converters.Add(new Color4Converter());
                    //serializer.Converters.Add(new ComponentConverter());

                    Project? project = serializer.Deserialize<Project>(jsonReader);

                    if (project != null)
                    {
                        project._meshObjects = new List<Object>();
                        project._instObjects = new List<Object>();
                        foreach(var obj in project.objects)
                        {
                            if(obj.Mesh != null)
                            {
                                obj.Mesh.parentObject = obj;
                                obj.Mesh.RecalculateModelMatrix(new bool[] { true, true, true });
                                obj.Mesh.recalculate = true;
                                if (obj.Mesh is Mesh m)
                                    project._meshObjects.Add(obj);
                                else if(obj.Mesh is InstancedMesh im)
                                    project._instObjects.Add(obj);  
                            }
                        }
                    }

                    return project;
                }
            }
            catch (Exception ex)
            {
                Engine.consoleManager.AddLog("An error occurred while loading the project: " + ex.Message, LogType.Error);
                return null;
            }
        }
    }
}
