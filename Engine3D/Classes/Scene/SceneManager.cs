using Newtonsoft.Json;
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
        public static void LoadScene(string path)
        {

        }

        public static void SaveScene(string path, List<Object> objects)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Create))
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

                serializer.Serialize(jsonWriter, objects);
            }
        }
    }
}
