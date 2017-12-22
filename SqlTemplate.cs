using HandlebarsDotNet;
using Microsoft.AspNetCore.Http;
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
        public static ConcurrentDictionary<string, Func<object, string>> CompiledTemplates = new ConcurrentDictionary<string, Func<object, string>>();

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

        static FileSystemWatcher watcher;

        static void InitSqlTemplates()
        {

        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            var templatePath = e.FullPath.Replace(App.GetWebRoot(), "").Substring(1).Replace("\\", "/");
            CompiledTemplates.TryRemove(templatePath, out _);
            //Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            var templatePath = e.FullPath.Replace(App.GetWebRoot(), "").Substring(1).Replace("\\", "/");
            CompiledTemplates.TryRemove(templatePath, out _);
            //Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }

        public static void InitSqlFilesWatcher()
        {
            watcher = new FileSystemWatcher();
            watcher.IncludeSubdirectories = true;
            watcher.Path = App.GetWebRoot();

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.Filter = "*.sql";


            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            watcher.EnableRaisingEvents = true;
        }

        // для боевого запуска SQL
        public static string[] emitSqlBatchFromTemplatePath(string dialect, string templatePath, JObject param, HttpContext httpContext, HttpRequest httpRequest)
        {


            if (!templatePath.EndsWith(".sql"))
                templatePath += ".sql";

            addServerTokens(dialect, param, httpContext, httpRequest);

            var _param = escapeSql(dialect, param);

            Func<object, string> compiledFunc;

            if (!CompiledTemplates.TryGetValue(dialect + "*" + templatePath, out compiledFunc))
            {
                var fullPath = App.GetWebRoot() + "/" + templatePath;
                var sqlTemplateText = File.ReadAllText(fullPath);

                var config = new HandlebarsConfiguration
                {
                    ThrowOnUnresolvedBindingExpression = true
                };
                var handlebars = Handlebars.Create(config);
                registerHelpers(handlebars, dialect);
                compiledFunc = handlebars.Compile(sqlTemplateText);

                CompiledTemplates.AddOrUpdate(dialect + "*" + templatePath, compiledFunc);
            }

            var sqlText = compiledFunc(_param);

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

        private static void registerHelpers(IHandlebars handlebars, string dialect)
        {
            handlebars.RegisterHelper("_ServerCurrentTime_", (writer, context, args) =>
            {
                if (dialect == "mssql")
                    writer.Write("getdate()");
                else
                if (dialect == "mysql")
                    writer.Write("now()");
                else
                if (dialect == "postgres")
                    writer.Write("current_time");
                else
                {
                    throw new Exception("BuhtaServer.registerHelpers(): неверный SQL-диалект '" + dialect + "'");
                }
            });

        }

        // для подсказок
        private static Object[] getServerTokensList()
        {
            return new Object[]
            {
                new { token="_UserName_",note="Имя пользователя (Строка)"},
                new { token="_UserId_",note="Id пользователя (Guid)"},
                new { token="_UserIP_",note="IP-адрес пользователя (Строка) в формате '192.168.0.104'"},
                new { token="_ServerCurrentTime_",note="Текущее время сервера (ДатаВремя)"},
            };
        }


        private static void addServerTokens(string dialect, JObject param, HttpContext httpContext, HttpRequest httpRequest)
        {
            if (!param.TryAdd("_UserName_", "Иванов"))
                throw new Exception("BuhtaServer.addServerTokens(): недопустимый параметр sql '_UserName_'");

            if (!param.TryAdd("_UserId_", "<Guid>bf261725-b1f4-4e84-95fe-fd892d7493e4"))
                throw new Exception("BuhtaServer.addServerTokens(): недопустимый параметр sql '_UserId_'");

            if (!param.TryAdd("_UserIP_", httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString()))
                throw new Exception("BuhtaServer.addServerTokens(): недопустимый параметр sql '_UserIP_'");

            if (dialect == "mssql")
            {
            }
            else
            if (dialect == "mysql")
            {
            }
            else
            if (dialect == "postgres")
            {
            }
            else
            {
                throw new Exception("BuhtaServer.registerHelpers(): неверный SQL-диалект '" + dialect + "'");
            }

        }



        // только для показа SQL в режиме редактирования запроса
        public static string[] emitSqlBatchFromTemplateText(string dialect, string sqlTemplateText, JObject param, HttpContext httpContext, HttpRequest httpRequest)
        {
            //            JToken param = JObject.Parse(p);


            addServerTokens(dialect, param, httpContext, httpRequest);

            var _param = escapeSql(dialect, param);

            var config = new HandlebarsConfiguration
            {
                ThrowOnUnresolvedBindingExpression = false
            };


            var handlebars = Handlebars.Create(config);
            registerHelpers(handlebars, dialect);
            var sqlText = handlebars.Compile(sqlTemplateText)(_param);


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
