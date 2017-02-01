using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackExchange.Opserver.Data.Dashboard
{
	public class HTTPUnitResult
	{
		public string TestCase;
		public string Protocol;
		public string Ip;
		public string UrlHost;
		public bool Error;
		public bool Connected;
		public bool GotCode;
		public bool GotText;
		public bool GotRegex;
		public bool InvalidCert;
		public float TimeTotal; // Milliseconds
	}
}
