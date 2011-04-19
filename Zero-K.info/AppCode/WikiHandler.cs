﻿using System;
using System.Net;
using System.Web;
using System.Web.Caching;

namespace ZeroKWeb
{
	public class WikiHandler
	{
		public static string LoadWiki(string node)
		{
			var entry = HttpContext.Current.Cache.Get("wiki_" + node) as string;
			if (entry != null) return entry;

			var wc = new WebClient();
			if (String.IsNullOrEmpty(node)) node = "Manual";

			var ret = wc.DownloadString("http://code.google.com/p/zero-k/wiki/" + node);

			var idx = ret.IndexOf("<div id=\"wikicontent\""); 
			var idx2 = ret.LastIndexOf("</td>");

			if (idx > -1 && idx2 > -1) ret = ret.Substring(idx, idx2 - idx);

			ret = ret.Replace("href=\"/p/zero-k/wiki/", "href =\"/Wiki/");
			ret = ret.Replace("href=\"/", "href=\"http://code.google.com/");

			HttpContext.Current.Cache.Insert("wiki_" + node, ret, null, DateTime.UtcNow.AddMinutes(15), Cache.NoSlidingExpiration);
			return ret;
		}
	}
}