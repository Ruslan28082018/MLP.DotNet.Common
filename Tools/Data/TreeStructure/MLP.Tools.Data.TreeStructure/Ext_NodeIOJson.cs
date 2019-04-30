using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
//using System.Web.Helpers;

namespace MLP.Tools
{
    public static class Ext_NodeIOJson
    {

        public static Node<T> Json2Node<T>(this string the)
            => JsonConvert.DeserializeObject<Node<T>>(the);

        public static string ToJson<T>(this Node<T> the)
            => JsonConvert.SerializeObject(the, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });
    }
}
