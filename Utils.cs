using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Snappy.Sharp;
using System.Text;
using System.Data.Common;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace BuhtaServer
{
    public static class Utils
    {

        public static bool IsPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }


        public static string CompressToBase64Str(string str)
        {
            var data = Encoding.Default.GetBytes(str);
            var snappy = new SnappyCompressor();

            int compressedSize = snappy.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            int result = snappy.Compress(data, 0, data.Length, compressed);

            return Convert.ToBase64String(compressed.Take(result).ToArray());

        }

        public static Byte[] CompressToByteArray(string str)
        {
            var data = Encoding.Default.GetBytes(str);
            var snappy = new SnappyCompressor();

            int compressedSize = snappy.MaxCompressedLength(data.Length);
            var compressed = new byte[compressedSize];

            int result = snappy.Compress(data, 0, data.Length, compressed);

            return compressed.Take(result).ToArray();

        }


        public static DataSet GetLoadedDataSet(string dbName, string SQL)
        {
            try
            {
                var database = Program.BuhtaConfig.GetDatabase(dbName);
                if (database == null)
                    throw new Exception($"invalid database '{dbName}'");

                DbConnection conn;
                DbDataAdapter adapter;

                if (database.Dialect == "mssql")
                {
                    conn = new SqlConnection(database.ConnectionString);
                    adapter = new SqlDataAdapter(SQL, conn as SqlConnection);
                }
                else
                if (database.Dialect == "mysql")
                {
                    conn = new MySqlConnection(database.ConnectionString);
                    adapter = new MySqlDataAdapter(SQL, conn as MySqlConnection);
                }
                else
                if (database.Dialect == "postgres")
                {
                    conn = new NpgsqlConnection(database.ConnectionString);
                    adapter = new NpgsqlDataAdapter(SQL, conn as NpgsqlConnection);
                }
                else
                {
                    throw new Exception($"invalid database sql dialect '{database.Dialect}'");
                }


                //   SqlDataAdapter adapter = new SqlDataAdapter(SQL, new SqlConnection(database.ConnectionString));
                conn.Open();
                adapter.SelectCommand.CommandTimeout = 0;
                DataSet retValue = new DataSet();
                adapter.Fill(retValue);
                conn.Close();
                return retValue;
            }
            catch (Exception ee)
            {
                throw new Exception(ee.Message + "\n\n" + SQL);
            }
        }

        public static void ExecuteSql(string dbName, string sql)
        {
            var database = Program.BuhtaConfig.GetDatabase(dbName);
            if (database == null)
                throw new Exception($"invalid database '{dbName}'");

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
                throw new Exception($"invalid database sql dialect '{database.Dialect}'");
            }

            using (conn)
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }
            }

        }

        public static void ExecuteSql(string dbName, IEnumerable<string> sqlBatch)
        {
            var database = Program.BuhtaConfig.GetDatabase(dbName);
            if (database == null)
                throw new Exception($"invalid database '{dbName}'");

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
                throw new Exception($"invalid database sql dialect '{database.Dialect}'");
            }

            if (database.Dialect == "mssql")
            {
                using (conn)
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = String.Join("\n", sqlBatch);
                        cmd.ExecuteNonQuery();
                        cmd.Dispose();
                    }
                }
            }
            else
            {
                using (conn)
                {
                    conn.Open();
                    foreach (var sql in sqlBatch)
                    {
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            cmd.ExecuteNonQuery();
                            cmd.Dispose();
                        }
                    }
                }
            }
        }

        public static string StringAsSql(string str, string dialect)
        {
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

        }

        public static string BoolAsSql(Boolean value, string dialect)
        {
            if (dialect == "mssql")
                return value ? "1" : "0";
            else
            if (dialect == "postgres")
                return value ? "TRUE" : "FALSE";
            else
            if (dialect == "mysql")
                return value ? "1" : "0";
            else
                throw new Exception("invalid dialect: " + dialect);

        }

        public static string DateAsSql(DateTime value, string dialect)
        {
            if (dialect == "mssql")
                return "CONVERT(DATE,'" + value.ToString("yyyyMMdd") + "')";
            else
            if (dialect == "postgres")
                return "TIMESTAMP(3)'" + value.ToString("yyyy-MM-dd") + "'";
            else
            if (dialect == "mysql")
                // timezone не воспринимает
                return "STR_TO_DATE('" + value.ToString("yyyy-MM-dd") + "','%Y-%c-%d')";
            else
                throw new Exception("invalid dialect: " + dialect);

        }

        public static string DateTimeAsSql(DateTime value, string dialect)
        {
            if (dialect == "mssql")
                return "CONVERT(DATETIME2,'" + value.ToString("yyyyMMdd HH:mm:ss.FFF") + "')";
            else
            if (dialect == "postgres")
                return "TIMESTAMP(3)'" + value.ToString("yyyy-MM-dd HH:mm:ss.FFF") + "'";
            else
            if (dialect == "mysql")
                // timezone не воспринимает
                return "STR_TO_DATE('" + value.ToString("yyyy-MM-dd HH:mm:ss.FFF") + "','%Y-%c-%d %k:%i:%s.%f')";
            else
                throw new Exception("invalid dialect: " + dialect);

        }

        public static string GuidAsSql(Guid value, string dialect)
        {
            if (dialect == "mssql")
                return "CONVERT(UNIQUEIDENTIFIER,'" + value.ToString() + "')";
            else
            if (dialect == "postgres")
                return "UUID '" + value.ToString() + "'";
            else
            if (dialect == "mysql")
                return "convert(0x" + BitConverter.ToString(value.ToByteArray()).Replace("-", "") + ",binary(16))";
            else
                throw new Exception("invalid dialect: " + dialect);

        }

        public static string NullAsSql(string dialect)
        {
            return "NULL";
        }


        public static JToken parseXJSON(JToken obj)
        {
            switch (obj.Type)
            {
                case JTokenType.Object:
                    foreach (var kvPair in obj as JObject)
                    {
                        string name = kvPair.Key;
                        JToken value = kvPair.Value;
                        obj[kvPair.Key] = parseXJSON(kvPair.Value);
                    }
                    return obj;

                case JTokenType.Array:
                    for (int i = 0; i < obj.ToArray().Length; i++)
                    {
                        obj[i] = parseXJSON(obj[i]);
                    }
                    return obj;

                case JTokenType.Integer:
                    return obj;

                case JTokenType.Float:
                    return obj;

                case JTokenType.Date:
                    return obj;

                case JTokenType.String:
                    var str = (string)obj;
                    if (str.StartsWith("<"))
                    {
                        if (str.StartsWith("<Date>"))
                        {
                            var dateStr = str.Substring("<Date>".Length);
                            var date = DateTime.Parse(dateStr);
                            return (JToken)date;
                        }
                        else
                        if (str.StartsWith("<DateTime>"))
                        {
                            var dateStr = str.Substring("<DateTime>".Length);
                            var date = DateTime.Parse(dateStr);
                            return (JToken)date;
                        }
                        else // 
                        if (str.StartsWith("<Guid>"))
                        {
                            var guidStr = str.Substring("<Guid>".Length);
                            var guid = Guid.Parse(guidStr);
                            //var guid = new Guid(Convert.FromBase64String(guidStr));

                            return (JToken)guid;
                        }
                        else
                        if (str.StartsWith("<Uint8Array>") || str.StartsWith("<ArrayBuffer>"))
                        {
                            string base64;
                            if (str.StartsWith("<Uint8Array>"))
                                base64 = str.Substring("<Uint8Array>".Length);
                            else
                                base64 = str.Substring("<ArrayBuffer>".Length);

                            return (JToken)Convert.FromBase64String(base64);
                        }
                        else
                        {
                            str = str.Substring(1);
                        }
                    }

                    return str;

                case JTokenType.Boolean:
                    return (JToken)((bool)obj);

                case JTokenType.Null:
                    return (JToken)null;

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

        }

        public static string GetRandomString(int maxSize)
        {
            char[] chars = new char[62];
            chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            byte[] data = new byte[1];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        public static string StringToSha256Base64(string str)
        {
            SHA256 sha256 = SHA256Managed.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static string CalcPasswordSha256Base64(string login, string password, bool isAdmin, string appSecuritySeed)
        {
            dynamic x = "v0N4VeAoDHLX4a6H";
            SHA256 sha256 = SHA256Managed.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(login + "\t" + password + "\t" + appSecuritySeed + "\t" + isAdmin.ToString() + "\t" + x);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

}
