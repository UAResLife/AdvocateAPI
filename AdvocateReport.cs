using RestSharp;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Xml;

namespace AdvocateAPI
{
    public class AdvocateReport
    {
        /// <summary>
        /// Cache Object
        /// </summary>
        private readonly ObjectCache cache = MemoryCache.Default;

        private CacheItemPolicy policy;

        public int CacheExpirationHours
        {
            set { policy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddHours(value)}; }
        }

        /// <summary>
        /// If true, the cache memory cache is not used
        /// </summary>
        public bool BypassCache { get; set; }

        /// <summary>
        /// The username to access Advocate's API
        /// </summary>
        public string APIUserName { get; set; }
        /// <summary>
        /// The password to access Advovate's API
        /// </summary>
        public string APIPassword { get; set; }

        /// <summary>
        /// Base64 Encoded Authorization. The string needs to be in the format {User}:{Password}
        /// </summary>
        public string Authorization { get; set; }

        /// <summary>
        /// The Get Report XML request XML to be sent to the server
        /// </summary>
        public string GetReportXMLRequestBody { get; set; }
        /// <summary>
        /// The Run Report XML request XML to be sent to the server
        /// </summary>
        public string RunReportXMLRequestBody { get; set; }
        /// <summary>
        /// The Check Report Status XML request XML to be sent to the server
        /// </summary>
        public string CheckReportStatusXMLRequestBody { get; set; }


        private readonly RestClient client;

        /// <summary>
        /// The wait time between tries in millisecons. After the report is executed, it takes some time to finish. we need to check if the report finished executing to be able to get the data.
        /// </summary>
        public int sleepBetweenTries { get; set; }
        /// <summary>
        /// The max number of tries. After the report is executed, it takes some time to finish. we need to check if the report finished executing to be able to get the data.
        /// </summary>
        public int maxTries { get; set; }

        /// <summary>
        /// The Server API url, this is set when the AdvocateReports object instance is created.
        /// </summary>
        private Uri _APIUrl;

        /// <summary>
        /// A read only parameter containing the API call client
        /// </summary>
        public RestClient Client
        {
            get { return client; }
        }
        /// <summary>
        /// Gets the report's data from Advocate
        /// </summary>
        /// <param name="reportID">The ID of the report to get the data for</param>
        /// <returns>A string with the content of the report</returns>
        public string GetReportAsText(string reportID)
        {
            return GetReportDataString(reportID);
        }

        /// <summary>
        /// Gets the report's data from Advocate
        /// </summary>
        /// <param name="reportID">The ID of the report to get the data for</param>
        /// <returns>A list object representing the content of the report</returns>
        public List<Dictionary<string,string>> GetReportAsList(string reportID)
        {
            var data = GetReportDataString(reportID);
            return Utilities.CSVToList(data);
        }

        /// <summary>
        /// Gets the report's data from Advocate
        /// </summary>
        /// <param name="reportID">The ID of the report to get the data for</param>
        /// <returns>An XML document representing the content of the report</returns>
        public XmlDocument GetReportAsXml(string reportID)
        {
            var data = GetReportDataString(reportID);
            return Utilities.CSVToXML(data);
        }

        /// <summary>
        /// Gets the report's data from Advocate
        /// </summary>
        /// <param name="reportID">The ID of the report to get the data for</param>
        /// <returns>an XML doc</returns>
        private XmlDocument GetReportXml(string reportID)
        {
            //Check if cache exists
            if (cache.Contains(reportID) && !BypassCache)
            {
                return (XmlDocument)cache.Get(reportID);
            }

            //Initializing variables
            var tries = 0;

            //Running the report
            var RunID = RunReport(reportID).InnerText;

            //Checking if the report finished running
            while (tries <= maxTries)
            {
                //Giving it time to run
                if (tries > 0) Thread.Sleep(sleepBetweenTries);

                //Checking the Status of the report
                var reportStatus = CheckReportStatus(RunID).InnerText;

                if (reportStatus == "complete")
                {
                    var xmlData = GetReportData(RunID);
                    cache.Add(reportID, xmlData, policy);
                    return xmlData;
                }
            }

            //Error if the max tries are reached
            if (tries > maxTries)
            {
                var msg = string.Format("The Advocate report (ID {0}) never was completed", reportID);
                //Log(msg);
                throw new Exception(msg);
            }

            return null;
        }

        /// <summary>
        /// Gets the report's data from Advocate
        /// </summary>
        /// <param name="reportID">The ID of the report to get the data for</param>
        /// <returns>a string with the content of the report</returns>
        private string GetReportDataString(string reportID)
        {
            return GetReportXml(reportID).InnerText;
        }

        public AdvocateReport(Uri APIUrl)
        {
            _APIUrl = APIUrl;
            client = new RestClient(APIUrl);
        }

        /// <summary>
        /// Executes a new version of the report on advocate
        /// </summary>
        /// <param name="ReportID"></param>
        /// <returns>The ID of the instance of the report on Advocate's server</returns>
        public XmlDocument RunReport(string ReportID)
        {
            var body = RunReportXMLRequestBody.Replace("{ReportID}", ReportID) ;
            var request = PrepareRequest(Method.POST, body);
            var response = client.Execute(request).Content;
            return StringToXML(response);
        }

        /// <summary>
        /// Returns the report status from the server
        /// </summary>
        /// <param name="RunID">The if of the report</param>
        /// <returns>a string containing the status of the running report</returns>
        public XmlDocument CheckReportStatus(string RunID)
        {
            //Prepare Request
            var body = CheckReportStatusXMLRequestBody.Replace("{RunID}", RunID);
            var request = PrepareRequest(Method.POST, body);
            
            //Getting data from the server
            var response = client.Execute(request).Content;

            //Returning the response
            return StringToXML(response);
        }

        /// <summary>
        /// Gets the report's data from Advocate
        /// </summary>
        /// <param name="RunID">The ID returned by the RunReport function</param>
        /// <returns>an XML document containing the data returned from the server</returns>
        public XmlDocument GetReportData(string RunID)
        {
            //Prepare Request
            var body = GetReportXMLRequestBody.Replace("{RunID}", RunID);
            var request = PrepareRequest(Method.POST, body);

            //Getting data from the server
            var response = client.Execute(request).Content;

            //Returning the response
            return StringToXML(response);
        }

        private XmlDocument StringToXML(string str)
        {
            var xml = new XmlDocument();
            try
            {
                xml.LoadXml(str);
            }
            catch
            {
                throw new Exception(str);
            }
            
            return xml;
        }

        /// <summary>
        /// Prepares the necessary headers for the API request
        /// </summary>
        /// <param name="method">Specifies the method used for the API call</param>
        /// <param name="requestBody">the body content to be sent with the request</param>
        /// <returns></returns>
        public RestRequest PrepareRequest(Method method, string requestBody)
        {

            

            var EncodedAuthorization = string.Empty;
            
            if(string.IsNullOrEmpty(Authorization)) EncodedAuthorization = 
                    Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes($"{APIUserName}:{APIPassword}"));

            var request = new RestRequest(method);
            var encoding = new ASCIIEncoding();
            var bodyBytes = encoding.GetBytes(requestBody);
            request.AddHeader("Content-Length", bodyBytes.Length.ToString());
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("Connection", "keep-alive");
            request.AddHeader("Accept-Encoding", "gzip, deflate");
            request.AddHeader("Cache-Control", "no-cache");
            request.AddHeader("Authorization", $"Basic {EncodedAuthorization}");
            request.AddHeader("Content-Type", "text/xml");
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Host", _APIUrl.Host);
            request.AddParameter("undefined", requestBody , ParameterType.RequestBody);
            return request;
        }

    }

}
