using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Engine3D.IComponentListConverter;

namespace Engine3D
{

    public class Project
    {
        public int engineId;
        public List<Object> objects;

        public Project(int engineId, List<Object> objects)
        {
            this.engineId = engineId;
            this.objects = objects;
        }
    }

    public static class SceneManager
    {
        public static Project? LoadScene(string saveFile, bool compress=true)
        {
            if (!File.Exists(saveFile))
            {
                Engine.consoleManager.AddLog("File not found: " + saveFile, LogType.Error);
                return null;
            }

            if (compress)
            {
                using (FileStream fileStream = new FileStream(saveFile, FileMode.Open))
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
                    serializer.Converters.Add(new Vector4Converter());
                    serializer.Converters.Add(new QuaternionConverter());
                    serializer.Converters.Add(new Matrix4Converter());
                    serializer.Converters.Add(new Color4Converter());
                    serializer.Converters.Add(new IComponentListConverter());
                    serializer.Converters.Add(new Vector3DConverter());
                    serializer.Converters.Add(new Color4DConverter());
                    serializer.Converters.Add(new MeshConverter());

                    Project? project = serializer.Deserialize<Project>(jsonReader);

                    if (project == null)
                        return null;

                    return project;
                }
            }
            else
            {
                using (StreamReader streamReader = new StreamReader(saveFile))
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
                    serializer.Converters.Add(new Vector4Converter());
                    serializer.Converters.Add(new QuaternionConverter());
                    serializer.Converters.Add(new Matrix4Converter());
                    serializer.Converters.Add(new Color4Converter());
                    serializer.Converters.Add(new IComponentListConverter());
                    serializer.Converters.Add(new Vector3DConverter());
                    serializer.Converters.Add(new Color4DConverter());
                    serializer.Converters.Add(new MeshConverter());

                    Project? project = serializer.Deserialize<Project>(jsonReader);

                    if (project == null)
                        return null;

                    return project;
                }
            }
        }

        public static void SaveScene(string saveFile, Project project, bool compress=true)
        {
            if (compress)
            {
                using (FileStream fileStream = new FileStream(saveFile, FileMode.Create))
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
                    serializer.Converters.Add(new Vector4Converter());
                    serializer.Converters.Add(new QuaternionConverter());
                    serializer.Converters.Add(new Matrix4Converter());
                    serializer.Converters.Add(new Color4Converter());
                    serializer.Converters.Add(new IComponentListConverter());
                    serializer.Converters.Add(new Vector3DConverter());
                    serializer.Converters.Add(new Color4DConverter());
                    serializer.Converters.Add(new MeshConverter());

                    serializer.Serialize(jsonWriter, project);
                }
            }
            else
            {
                using (StreamWriter file = File.CreateText(saveFile))
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
                    serializer.Converters.Add(new Vector4Converter());
                    serializer.Converters.Add(new QuaternionConverter());
                    serializer.Converters.Add(new Matrix4Converter());
                    serializer.Converters.Add(new Color4Converter());
                    serializer.Converters.Add(new IComponentListConverter());
                    serializer.Converters.Add(new Vector3DConverter());
                    serializer.Converters.Add(new Color4DConverter());
                    serializer.Converters.Add(new MeshConverter());

                    serializer.Serialize(writer, project);
                }
            }
        }
    }
}
