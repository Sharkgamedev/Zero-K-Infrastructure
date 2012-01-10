﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace ZeroKWeb
{
    public class JsonRequest
    {
        public static string MakeRequest(string url, object data) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            var ser = new JavaScriptSerializer();
            StreamWriter writer = new StreamWriter(request.GetRequestStream());
            var serialized = ser.Serialize(data);
            writer.Write(serialized);
            writer.Close();
            var ms = new MemoryStream();
            request.GetResponse().GetResponseStream().CopyTo(ms);
            return serialized;
        }

    }
}