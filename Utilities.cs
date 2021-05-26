using System;
using System.Web;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml;

namespace AdvocateAPI
{
    public class Utilities
    {
        /// <summary>
        /// Not Finished
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string ConvertDataTabletoString(DataTable dt)
        {
            //var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            Dictionary<string, object> row;
            foreach (DataRow dr in dt.Rows)
            {
                row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    row.Add(col.ColumnName, dr[col]);
                }
                rows.Add(row);
            }
            return string.Empty;

        }

        public static XmlDocument CSVToXML(string CSV)
        {
            Regex regx = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"); //Regex to split by comma, unless in quotes.

            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = doc.DocumentElement;
            doc.InsertBefore(xmlDeclaration, root);

            XmlElement rootElement = doc.CreateElement(string.Empty, "Report", string.Empty);
            doc.AppendChild(rootElement);

            var lines = Regex.Matches(CSV, @"(?m)^[^""\r\n]*(?:(?:""[^""]*"")+[^""\r\n]*)*")
                .OfType<Match>()
                .Select(m => m.Value)
                .ToArray();

            var headers = regx.Split(lines[0]).Select(m => m.Replace("\"", "")).ToArray();
            var lines_ = new string[lines.Length - 1];


            Array.Copy(lines, 1, lines_, 0, lines.Length - 1); //To remove the header

            foreach (string line in lines_)
            {
                int i = 0;

                XmlElement rowElement = doc.CreateElement(string.Empty, "Row", string.Empty);
                rootElement.AppendChild(rowElement);

                foreach (var item in regx.Split(line).Select(m => m.Replace("\"", "")))
                {
                    
                    XmlElement colElement = doc.CreateElement(string.Empty,  "Column", string.Empty);
                    colElement.SetAttribute("Name", headers[i]);
                    colElement.InnerText = item;
                    rowElement.AppendChild(colElement);
                    i++;
                }
            }

            return doc;
        }

        

        public static List<Dictionary<string, string>> CSVToList(string CSV)
        {
            var data = new List<Dictionary<string, string>>();

            var lines = Regex.Matches(CSV, @"(?m)^[^""\r\n]*(?:(?:""[^""]*"")+[^""\r\n]*)*")
                .OfType<Match>()
                .Select(m => m.Value)
                .ToArray();

            Regex regx = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)"); //Regex to split by comma, unless in quotes.

            var headers = regx.Split(lines[0]).Select(m => m.Replace("\"","")).ToArray(); //Gets the headers [0] for first row
            var lines_ = new string[lines.Length-1];

            Array.Copy(lines, 1, lines_, 0, lines.Length-1); //To remove the header out of lines_

            foreach (string line in lines_)
            {
                var l = new Dictionary<string, string>();
                int i = 0;

                foreach (var item in regx.Split(line).Select(m => m.Replace("\"", "")))
                {
                    l.Add(headers[i], item);
                    i++;
                }

                data.Add(l);
            }
            return data;
        }
    }
}
