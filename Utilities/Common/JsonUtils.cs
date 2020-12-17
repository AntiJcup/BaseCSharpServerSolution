using System.IO;
using Newtonsoft.Json;

namespace BaseApi.Utilities.Common
{
    public class JsonUtils
    {
        public static void Serialize(object value, Stream s)
        {
            StreamWriter writer = new StreamWriter(s);
            JsonTextWriter jsonWriter = new JsonTextWriter(writer);

            JsonSerializer ser = new JsonSerializer();
            ser.Serialize(jsonWriter, value);
            jsonWriter.Flush();
        }

        public static T Deserialize<T>(Stream s)
        {
            StreamReader reader = new StreamReader(s);
            using (JsonTextReader jsonReader = new JsonTextReader(reader))
            {
                JsonSerializer ser = new JsonSerializer();
                return ser.Deserialize<T>(jsonReader);
            }
        }
    }
}