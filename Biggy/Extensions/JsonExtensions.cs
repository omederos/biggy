using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Collections;

namespace Biggy.Extensions
{
    public static class JsonExtensions
    {


        public static string ToJSON(this object o)
        {

            var serializer = new JavaScriptSerializer();
            var sb = new StringBuilder();
            serializer.Serialize(o, sb);
            return sb.ToString();
        }


        public static T FromJSON<T>(this string json)
        {
            var serializer = new JavaScriptSerializer();
            return serializer.Deserialize<T>(json);
        }

    }
}
