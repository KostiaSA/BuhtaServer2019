using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Data.Common;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Npgsql;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;

namespace BuhtaServer.Controllers
{


    [Produces("application/json")]
    [Route("api/admin/adminExecuteSql")]
    public class AdminExecuteSqlController : BaseAdminController
    {
        class ResponseObject
        {
            public string json;
            public byte[] compressed;
            public string error;
        }

        public static string Utf16ToUtf8(string utf16String)
        {
            // Get UTF16 bytes and convert UTF16 bytes to UTF8 bytes
            byte[] utf16Bytes = Encoding.Unicode.GetBytes(utf16String);
            byte[] utf8Bytes = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, utf16Bytes);

            // Return UTF8 bytes as ANSI string
            return Encoding.Default.GetString(utf8Bytes);
        }

        [HttpPost]
        public object Post([FromBody]dynamic req)
        {
            try
            {
                var request = Utils.parseXJSON(JObject.Parse(req.xjson.ToString()));

                if (!AuthOk((Guid)request["sessionId"], (String)request["authToken"]))
                    return NoAuthResponse();

                var database = Program.BuhtaConfig.GetDatabase(request["database"].ToString());
                if (database == null)
                {
                    return new ResponseObject() { error = $"invalid database '{request["database"]}'" };
                }

                DbConnection conn;

                if (database.Dialect == "mssql")
                    conn = new SqlConnection(database.ConnectionString);
                else
                if (database.Dialect == "mysql")
                    conn = new MySqlConnection(database.ConnectionString);
                else
                if (database.Dialect == "postgres")
                    conn = new NpgsqlConnection(database.ConnectionString);
                else
                {
                    return new ResponseObject() { error = $"invalid database sql dialect '{database.Dialect}'" };
                }


                try
                {

                    using (conn)
                    {
                        conn.Open();

                        StringBuilder sb = new StringBuilder();
                        StringWriter sw = new StringWriter(sb);

                        using (JsonWriter jsonWriter = new JsonTextWriter(sw))
                        {

                            //ctx.WriteString(`{ "rowsets":[`);
                            jsonWriter.WriteStartObject();
                            jsonWriter.WritePropertyName("rowsets");
                            jsonWriter.WriteStartArray();

                            foreach (string sql in request["sql"])
                            {
                                using (var cmd = conn.CreateCommand())
                                {
                                    cmd.CommandText = sql;
                                    var reader = cmd.ExecuteReader();
                                    if (reader.FieldCount == 0)
                                    {
                                        reader.Dispose();
                                        cmd.Dispose();
                                        continue;
                                    }

                                    jsonWriter.WriteStartObject();
                                    jsonWriter.WritePropertyName("columns");
                                    jsonWriter.WriteStartArray();
                                    for (int col = 0; col < reader.FieldCount; col++)
                                    {
                                        jsonWriter.WriteStartObject();
                                        jsonWriter.WritePropertyName("name");

                                        jsonWriter.WriteValue(reader.GetName(col));

                                        jsonWriter.WritePropertyName("type");
                                        jsonWriter.WriteValue(reader.GetDataTypeName(col));
                                        jsonWriter.WriteEndObject();
                                    }
                                    jsonWriter.WriteEnd();
                                    jsonWriter.WritePropertyName("rows");
                                    jsonWriter.WriteStartArray();
                                    while (true)
                                    {
                                        //jsonWriter.WriteStartArray();
                                        while (reader.Read())
                                        {

                                            jsonWriter.WriteStartArray();
                                            #region for
                                            for (int colIndex = 0; colIndex < reader.FieldCount; colIndex++)
                                            {
                                                var value = reader[colIndex];
                                                //reader.GetDataTypeName(37)
                                                if (value is DBNull)
                                                {
                                                    jsonWriter.WriteStartObject();
                                                    jsonWriter.WritePropertyName("t");
                                                    jsonWriter.WriteValue("N");
                                                    jsonWriter.WriteEndObject();
                                                }
                                                else
                                                if (value is Array && (value as Array).Length == 16 && database.Dialect == "mysql")  // это guid в mysql
                                                {
                                                    var guid = new Guid(value as byte[]);
                                                    //jsonWriter.WriteValue("<Guid>" + Convert.ToBase64String(guid.ToByteArray()));
                                                    jsonWriter.WriteValue("<Guid>" + guid);
                                                }
                                                else
                                                if (reader.GetDataTypeName(colIndex) == "BIT" && database.Dialect == "mysql")  // это boolean в mysql
                                                {
                                                    jsonWriter.WriteValue((UInt64)value != 0);
                                                }
                                                else
                                                if (value is Array)  // это BLOB
                                                {
                                                    jsonWriter.WriteValue("<ArrayBuffer>" + Convert.ToBase64String((byte[])value));
                                                }
                                                else
                                                if (value is Guid)  
                                                {
                                                    //jsonWriter.WriteValue("<Guid>" + Convert.ToBase64String(((Guid)value).ToByteArray()));
                                                    jsonWriter.WriteValue("<Guid>" + value);

                                                }
                                                else
                                                if (value is DateTime)
                                                {
                                                    var date = (DateTime)value;
                                                    //jsonWriter.WriteStartObject();
                                                    //jsonWriter.WritePropertyName("t");
                                                    //jsonWriter.WriteValue("D");
                                                    //jsonWriter.WritePropertyName("v");
                                                    if (date.TimeOfDay == TimeSpan.Zero)
                                                        jsonWriter.WriteValue("<Date>" + (date).ToString("yyyy-MM-dd"));
                                                    else
                                                    if (date.Year == 0 && date.Month == 1 && date.Day == 1)
                                                        jsonWriter.WriteValue("<Time>" + (date).ToString("HH:mm:ss.fff"));
                                                    else
                                                        jsonWriter.WriteValue("<Date>" + (date).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                                                    //jsonWriter.WriteEndObject();
                                                }
                                                else
                                                if (value is TimeSpan)
                                                {
                                                    jsonWriter.WriteStartObject();
                                                    jsonWriter.WritePropertyName("t");
                                                    jsonWriter.WriteValue("T");

                                                    jsonWriter.WritePropertyName("h");
                                                    jsonWriter.WriteValue(((TimeSpan)value).Hours);
                                                    jsonWriter.WritePropertyName("m");
                                                    jsonWriter.WriteValue(((TimeSpan)value).Minutes);
                                                    jsonWriter.WritePropertyName("s");
                                                    jsonWriter.WriteValue(((TimeSpan)value).Seconds);
                                                    jsonWriter.WritePropertyName("ms");
                                                    jsonWriter.WriteValue(((TimeSpan)value).Milliseconds);

                                                    jsonWriter.WriteEndObject();
                                                }
                                                else
                                                    jsonWriter.WriteValue(value);

                                                //Console.WriteLine(String.Format("{0}", reader[0]));
                                            }
                                            #endregion
                                            jsonWriter.WriteEnd();
                                        }
                                        //jsonWriter.WriteEnd();
                                        //if (database.Dialect != "mssql" || !reader.NextResult())
                                        if (!reader.NextResult())
                                            break;
                                        else
                                        {
                                            jsonWriter.WriteEnd();
                                            jsonWriter.WriteEndObject();

                                            jsonWriter.WriteStartObject();
                                            jsonWriter.WritePropertyName("columns");
                                            jsonWriter.WriteStartArray();
                                            for (int col = 0; col < reader.FieldCount; col++)
                                            {
                                                jsonWriter.WriteStartObject();
                                                jsonWriter.WritePropertyName("name");
                                                jsonWriter.WriteValue(reader.GetColumnSchema()[col].ColumnName);
                                                jsonWriter.WritePropertyName("type");
                                                jsonWriter.WriteValue(reader.GetColumnSchema()[col].DataTypeName);
                                                jsonWriter.WriteEndObject();
                                            }
                                            jsonWriter.WriteEnd();
                                            jsonWriter.WritePropertyName("rows");
                                            jsonWriter.WriteStartArray();

                                        }
                                        //Console.WriteLine("----- NEXT ------");
                                    }
                                    jsonWriter.WriteEnd();
                                    jsonWriter.WriteEndObject();
                                    reader.Dispose();
                                    cmd.Dispose();
                                }
                            }
                            jsonWriter.WriteEnd();
                            jsonWriter.WriteEndObject();

                        }
                        //Console.WriteLine("req: " + json);
                        //Console.WriteLine("abs: " + sb.ToString());

                        return new ResponseObject() { compressed = Utils.CompressToByteArray(sb.ToString()) };
                        //return new ResponseObject() { json = sb.ToString() };
                    }
                }
                catch (Exception e)
                {
                    return new ResponseObject() { error = Utf16ToUtf8(e.Message) };
                }
            }
            catch (Exception e)
            {
                return new { error = e.Message };
            }

        }

    }
}
