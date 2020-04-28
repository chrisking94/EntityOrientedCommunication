using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace EntityOrientedCommunication
{
    public static class Serializer
    {
        private static JsonSerializerSettings jsonSerializerSettings;

        static Serializer()
        {
            jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.TypeNameHandling = TypeNameHandling.All;
            jsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            jsonSerializerSettings.PreserveReferencesHandling = PreserveReferencesHandling.All;
            jsonSerializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;
        }

        public static string ToJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
        }

        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings);
        }
    }
}
