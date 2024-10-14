using Newtonsoft.Json;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine3D
{

    public static class SceneManager
    {
        public static List<Object>? LoadScene(string path, bool compress=true)
        {
            string saveFile = path + "\\save.sav";

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
                    serializer.Converters.Add(new QuaternionConverter());
                    serializer.Converters.Add(new Matrix4Converter());
                    serializer.Converters.Add(new Color4Converter());
                    serializer.Converters.Add(new IComponentListConverter());

                    List<Object>? objects = serializer.Deserialize<List<Object>>(jsonReader);

                    if (objects == null)
                        return null;

                    return objects;
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
                    serializer.Converters.Add(new QuaternionConverter());
                    serializer.Converters.Add(new Matrix4Converter());
                    serializer.Converters.Add(new Color4Converter());
                    serializer.Converters.Add(new IComponentListConverter());

                    List<Object>? objects = serializer.Deserialize<List<Object>>(jsonReader);

                    if (objects == null)
                        return null;

                    return objects;
                }
            }
        }

        public static void SaveScene(string path, List<Object> objects, bool compress=true)
        {
            string saveFile = path + "\\save.sav";
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
                    serializer.Converters.Add(new QuaternionConverter());
                    serializer.Converters.Add(new Matrix4Converter());
                    serializer.Converters.Add(new Color4Converter());
                    serializer.Converters.Add(new IComponentListConverter());

                    serializer.Serialize(jsonWriter, objects);
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
                    serializer.Converters.Add(new QuaternionConverter());
                    serializer.Converters.Add(new Matrix4Converter());
                    serializer.Converters.Add(new Color4Converter());
                    serializer.Converters.Add(new IComponentListConverter());

                    serializer.Serialize(writer, objects);
                }
            }
        }
    }
}
