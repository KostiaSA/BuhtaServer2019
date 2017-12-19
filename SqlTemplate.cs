using HandlebarsDotNet;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuhtaServer
{
    public class SqlTemplate
    {
        static ConcurrentDictionary<string, Func<object, string>> CompiledTemplates = new ConcurrentDictionary<string, Func<object, string>>();

        static JToken escapeSql(string dialect, JToken obj)
        {
            switch (obj.Type)
            {
                case JTokenType.Object:
                    foreach (var kvPair in obj as JObject)
                    {
                        string name = kvPair.Key;
                        JToken value = kvPair.Value;
                        obj[kvPair.Key] = escapeSql(dialect, kvPair.Value);
                    }
                    return obj;

                case JTokenType.Array:
                    for (int i = 0; i < obj.ToArray().Length; i++)
                    {
                        obj[i] = escapeSql(dialect, obj[i]);
                    }
                    return obj;

                case JTokenType.Integer:
                    return obj.ToString().Replace(",", ".");

                case JTokenType.Float:
                    return obj.ToString().Replace(",", ".");

                case JTokenType.String:
                    var str = (string)obj;
                    if (str.StartsWith("<"))
                    {
                        if (str.StartsWith("<Date>"))
                        {
                            var dateStr = str.Substring("<Date>".Length);
                            var date = DateTime.Parse(dateStr);
                            if (dialect == "mssql")
                                return "CONVERT(DATE,'" + date.ToString("yyyyMMdd") + "')";
                            else
                            if (dialect == "postgres")
                                return "TIMESTAMP(3)'" + date.ToString("yyyy-MM-dd") + "'";
                            else
                            if (dialect == "mysql")
                                // timezone не воспринимает
                                return "STR_TO_DATE('" + date.ToString("yyyy-MM-dd") + "','%Y-%c-%d')";
                            else
                                throw new Exception("invalid dialect: " + dialect);

                        }
                        else
                        if (str.StartsWith("<DateTime>"))
                        {
                            var dateStr = str.Substring("<DateTime>".Length);
                            var date = DateTime.Parse(dateStr);
                            if (dialect == "mssql")
                                return "CONVERT(DATETIME2,'" + date.ToString("yyyyMMdd HH:mm:ss.FFF") + "')";
                            else
                            if (dialect == "postgres")
                                return "TIMESTAMP(3)'" + date.ToString("yyyy-MM-dd HH:mm:ss.FFF") + "'";
                            else
                            if (dialect == "mysql")
                                // timezone не воспринимает
                                return "STR_TO_DATE('" + date.ToString("yyyy-MM-dd HH:mm:ss.FFF") + "','%Y-%c-%d %k:%i:%s.%f')";
                            else
                                throw new Exception("invalid dialect: " + dialect);

                        }
                        else // 
                        if (str.StartsWith("<Guid>"))
                        {
                            var guidStr = str.Substring("<Guid>".Length);
                            var guid = Guid.Parse(guidStr);
                            if (dialect == "mssql")
                                return "CONVERT(UNIQUEIDENTIFIER,'" + guid.ToString() + "')";
                            else
                            if (dialect == "postgres")
                                return "UUID '" + guid.ToString() + "'";
                            else
                            if (dialect == "mysql")
                                return "convert(0x" + BitConverter.ToString(guid.ToByteArray()).Replace("-", "") + ",binary(16))";
                            else
                                throw new Exception("invalid dialect: " + dialect);
                        }
                        else
                        if (str.StartsWith("<Uint8Array>") || str.StartsWith("<ArrayBuffer>"))
                        {
                            string base64;
                            if (str.StartsWith("<Uint8Array>"))
                                base64 = str.Substring("<Uint8Array>".Length);
                            else
                                base64 = str.Substring("<ArrayBuffer>".Length);
                            var hexStr = BitConverter.ToString(Convert.FromBase64String(base64)).Replace("-", "");

                            if (dialect == "mssql")
                                return "0x" + hexStr;
                            else if (dialect == "postgres")
                                return "E'\\\\x" + hexStr + "'";
                            else if (dialect == "mysql")
                                return "convert(X'" + hexStr + "',binary)";
                            else
                                throw new Exception("invalid dialect: " + dialect);
                        }
                        else
                        {
                            str = str.Substring(1);
                        }
                    }

                    if (dialect == "mssql")
                        return "'" + str.Replace("'", "''") + "'";
                    else
                    if (dialect == "postgres")
                        return "'" + str.Replace("\0", "").Replace("'", "''") + "'";
                    else
                    if (dialect == "mysql")
                        return "'" + MySql.Data.MySqlClient.MySqlHelper.EscapeString(str) + "'";
                    else
                        throw new Exception("invalid dialect: " + dialect);

                case JTokenType.Boolean:
                    if (dialect == "mssql")
                        return (bool)obj ? "1" : "0";
                    else
                    if (dialect == "postgres")
                        return (bool)obj ? "TRUE" : "FALSE";
                    else
                    if (dialect == "mysql")
                        return (bool)obj ? "1" : "0";
                    else
                        throw new Exception("invalid dialect: " + dialect);

                case JTokenType.Null:
                    return "NULL";

                case JTokenType.None:
                    throw new Exception("invalid token: JTokenType.None");
                case JTokenType.Constructor:
                    throw new Exception("JTokenType.Constructor: internal error");
                case JTokenType.Property:
                    throw new Exception("JTokenType.Property: internal error");
                case JTokenType.Comment:
                    throw new Exception("JTokenType.Comment: internal error");
                case JTokenType.Undefined:
                    throw new Exception("JTokenType.Undefined: internal error");
                case JTokenType.Date:
                    throw new Exception("JTokenType.Date: internal error");
                case JTokenType.Raw:
                    throw new Exception("JTokenType.Raw: internal error");
                case JTokenType.Bytes:
                    throw new Exception("JTokenType.Bytes: internal error");
                case JTokenType.Guid:
                    throw new Exception("JTokenType.Guid: internal error");
                case JTokenType.Uri:
                    throw new Exception("JTokenType.Uri: internal error");
                case JTokenType.TimeSpan:
                    throw new Exception("JTokenType.TimeSpan: internal error");
                default:
                    throw new Exception("unknown token");
            }
            //}
        }

        public static string[] emitSqlBatchFromTemplatePath(string dialect, string templatePath, JToken param)
        {
//            JToken param = JObject.Parse(p);

            if (!templatePath.EndsWith(".sql"))
                templatePath += ".sql";

            param = escapeSql(dialect, param);

            Func<object, string> compiledFunc;

            if (!CompiledTemplates.TryGetValue(templatePath, out compiledFunc))
            {
                var fullPath = App.GetWebRoot() + "/" + templatePath;
                var sqlTemplateText = File.ReadAllText(fullPath);
                compiledFunc = Handlebars.Compile(sqlTemplateText);
                CompiledTemplates.AddOrUpdate(templatePath, compiledFunc);
            }

            var sqlText = compiledFunc(param);

            if (dialect == "mssql")
            {
                sqlText = sqlText.Replace("[[", "[").Replace("]]", "]");
            }
            else
            if (dialect == "mysql")
            {
                sqlText = sqlText.Replace("[[", "\u0001").Replace("]]", "\u0002");
                sqlText = sqlText.Replace("[", "`").Replace("]", "`");
                sqlText = sqlText.Replace("\u0001", "[").Replace("\u0002", "]");
            }
            else
            if (dialect == "postgres")
            {
                sqlText = sqlText.Replace("[[", "\u0001").Replace("]]", "\u0002");
                sqlText = sqlText.Replace("[", @"""").Replace("]", @"""");
                sqlText = sqlText.Replace("\u0001", "[").Replace("\u0002", "]");
            }
            else
                throw new Exception("invalid sql dialect " + dialect);

            string[] lines = sqlText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var commands = new List<string>();

            StringBuilder command = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed == "GO" || trimmed == "Go" || trimmed == "gO" || trimmed == "go")
                {
                    commands.Add(command.ToString());
                    command.Clear();
                }
                else
                    command.AppendLine(line);
            }
            if (command.Length > 0)
                commands.Add(command.ToString());

            return commands.ToArray();
        }

        public static string[] emitSqlBatchFromTemplateText(string dialect, string sqlTemplateText, JToken param)
        {
            //            JToken param = JObject.Parse(p);

            param = escapeSql(dialect, param);

            //Func<object, string> compiledFunc;

            //if (!CompiledTemplates.TryGetValue(templatePath, out compiledFunc))
            //{
            //    var fullPath = App.GetWebRoot() + "/" + templatePath;
            //    var sqlTemplateText = File.ReadAllText(fullPath);
            //    compiledFunc = Handlebars.Compile(sqlTemplateText);
            //    CompiledTemplates.AddOrUpdate(templatePath, compiledFunc);
            //}

            var sqlText = Handlebars.Compile(sqlTemplateText)(param);

            if (dialect == "mssql")
            {
                sqlText = sqlText.Replace("[[", "[").Replace("]]", "]");
            }
            else
            if (dialect == "mysql")
            {
                sqlText = sqlText.Replace("[[", "\u0001").Replace("]]", "\u0002");
                sqlText = sqlText.Replace("[", "`").Replace("]", "`");
                sqlText = sqlText.Replace("\u0001", "[").Replace("\u0002", "]");
            }
            else
            if (dialect == "postgres")
            {
                sqlText = sqlText.Replace("[[", "\u0001").Replace("]]", "\u0002");
                sqlText = sqlText.Replace("[", @"""").Replace("]", @"""");
                sqlText = sqlText.Replace("\u0001", "[").Replace("\u0002", "]");
            }
            else
                throw new Exception("invalid sql dialect " + dialect);

            string[] lines = sqlText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            var commands = new List<string>();

            StringBuilder command = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed == "GO" || trimmed == "Go" || trimmed == "gO" || trimmed == "go")
                {
                    commands.Add(command.ToString());
                    command.Clear();
                }
                else
                    command.AppendLine(line);
            }
            if (command.Length > 0)
                commands.Add(command.ToString());

            return commands.ToArray();
        }
    }
}
