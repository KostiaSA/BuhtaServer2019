using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuhtaServer
{
    public class BuhtaConfig
    {
        public string serverUniqueName;
        public string appSecuritySeed;
        public List<Database> databases = new List<Database>();

        public Database GetDatabase(string dbName)
        {
            return databases.Find(db => db.Name.Equals(dbName, StringComparison.InvariantCultureIgnoreCase));
        }


        public void CheckOnStartup()
        {
            if (serverUniqueName == null || serverUniqueName == "")
                Program.RegisterStartupError("'buhtaSettings.json': не указан или неверный ServerUniqueName");

            if (appSecuritySeed == null || appSecuritySeed == "")
                Program.RegisterStartupError("'buhtaSettings.json': не указан или неверный appSecuritySeed");

            var authOk = false;
            var mainOk = false;
            foreach (var db in databases)
            {
                if (db.Name == "main")
                    mainOk = true;
                if (db.Name == "auth")
                {
                    authOk = true;
                    App.AuthDb = db;
                }
                try
                {
                    Utils.ExecuteSql(db.Name, "SELECT 1 AS A");
                    Console.WriteLine("проверка коннекта к базе данных " + db.Name + "(" + db.SqlName + "," + db.Dialect + "): Ok");
                }
                catch
                {
                    Program.RegisterStartupError("'buhtaSettings.json': нет коннекта к базе данных " + db.Name + "(" + db.SqlName + "," + db.Dialect + ")");
                }
            }

            if (!authOk)
                Program.RegisterStartupError("'buhtaSettings.json': в настройках нет базы данных 'auth'");
            if (!mainOk)
                Program.RegisterStartupError("'buhtaSettings.json': в настройках нет базы данных 'main'");

            InitDbAuthUsersOnStartup();
            InitDbAuthSessionsOnStartup();

        }

        public void InitDbAuthUsersOnStartup()
        {
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_CATALOG=" + Utils.StringAsSql(App.AuthDb.SqlName, App.AuthDb.Dialect) + " AND TABLE_NAME='buhta_auth_User'";
            var dataset = Utils.GetLoadedDataSet(App.AuthDb.Name, sql);
            var count = (int)dataset.Tables[0].Rows[0][0];
            if (count == 0)
            {
                string sql1;
                string sql2;
                if (App.AuthDb.Dialect == "mssql")
                    sql1 = @"
CREATE TABLE [buhta_auth_User](
  [userId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
  [login] NVARCHAR(50),
  [password] NVARCHAR(50),
  [isAdmin] BIT
)
";
                else
                if (App.AuthDb.Dialect == "mysql")
                    sql1 = @"
CREATE TABLE `buhta_auth_User`(
  `userId` BINARY(16) NOT NULL PRIMARY KEY,
  `login` VARCHAR(50),
  `password` VARCHAR(50),
  `isAdmin` BIT
)
";
                else
                if (App.AuthDb.Dialect == "postgres")
                    sql1 = @"
CREATE TABLE buhta_auth_User(
  userId UUID NOT NULL PRIMARY KEY,
  login VARCHAR(50),
  password VARCHAR(50),
  isAdmin boolean
)
";
                else
                    throw new Exception("invalid dialect: " + App.AuthDb.Dialect);

                var passwordBase64 = Utils.CalcPasswordSha256Base64("admin","admin",true,Program.BuhtaConfig.appSecuritySeed);
                sql2 = "INSERT INTO buhta_auth_User(userId,login,password,isAdmin) VALUES(" + Utils.GuidAsSql(new Guid("045F15A1-E06E-448E-83FF-5ADBEF7547D9"), App.AuthDb.Dialect) + "," + Utils.StringAsSql("admin", App.AuthDb.Dialect) + "," + Utils.StringAsSql(passwordBase64, App.AuthDb.Dialect) + ",1)";

                Utils.ExecuteSql(App.AuthDb.Name, sql1);
                Utils.ExecuteSql(App.AuthDb.Name, sql2);
            }
        }

        public void InitDbAuthSessionsOnStartup()
        {
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_CATALOG=" + Utils.StringAsSql(App.AuthDb.SqlName, App.AuthDb.Dialect) + " AND TABLE_NAME='buhta_auth_Session'";
            var dataset = Utils.GetLoadedDataSet(App.AuthDb.Name, sql);
            var count = (int)dataset.Tables[0].Rows[0][0];
            if (count == 0)
            {
                string sql1;
                if (App.AuthDb.Dialect == "mssql")
                    sql1 = @"
CREATE TABLE [buhta_auth_Session](
  [sessionId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
  [clientIp] NVARCHAR(50),
  [buhtaServerName] NVARCHAR(50),
  [createTime] DATETIME2,
  [lastTime] DATETIME2,
  [userId] UNIQUEIDENTIFIER,
  [authToken] NVARCHAR(32),
  [login] NVARCHAR(50),
  [isAdmin] BIT
)
";
                else
                if (App.AuthDb.Dialect == "mysql")
                    sql1 = @"
CREATE TABLE `buhta_auth_Session`(
  `sessionId` BINARY(16) NOT NULL PRIMARY KEY,
  `clientIp` VARCHAR(50),
  `buhtaServerName` VARCHAR(50),
  `createTime` DATETIME(3),
  `lastTime` DATETIME(3),
  `userId` BINARY(16),
  `authToken` VARCHAR(32),
  `login` VARCHAR(50),
  `isAdmin` BIT
)
";
                else
                if (App.AuthDb.Dialect == "postgres")
                    sql1 = @"
CREATE TABLE buhta_auth_Session(
  sessionId UUID NOT NULL PRIMARY KEY,
  clientIp VARCHAR(50),
  buhtaServerName VARCHAR(50),
  createTime TIMESTAMP,
  lastTime TIMESTAMP,
  userId UUID,
  authToken VARCHAR(32),
  login VARCHAR(50),
  isAdmin boolean
)
";
                else
                    throw new Exception("invalid dialect: " + App.AuthDb.Dialect);

                Utils.ExecuteSql(App.AuthDb.Name, sql1);
            }
        }
    }

    public class Database
    {
        public string Name;
        public string Dialect;
        public string Note;
        public string ConnectionString;
        public string SqlName;
    }
}


/*
CREATE TABLE [buhta_auth_Session](
  [sessionId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
  [clientIp] NVARCHAR(50),
  [buhtaServerName] NVARCHAR(50),
  [createTime] DATETIME2,
  [lastTime] DATETIME2,
  [userId] UNIQUEIDENTIFIER,
  [authToken] NVARCHAR(32),
  [login] NVARCHAR(50)
)

CREATE TABLE `buhta_auth_Session`(
  `sessionId` BINARY(16) NOT NULL PRIMARY KEY,
  `clientIp` VARCHAR(50),
  `buhtaServerName` VARCHAR(50),
  `createTime` DATETIME(3),
  `lastTime` DATETIME(3),
  `userId` BINARY(16),
  `authToken` VARCHAR(32),
  `login` VARCHAR(50)
)

CREATE TABLE "buhta_auth_Session"(
  "sessionId" UUID NOT NULL PRIMARY KEY,
  "clientIp" VARCHAR(50),
  "buhtaServerName" VARCHAR(50),
  "createTime" TIMESTAMP,
  "lastTime" TIMESTAMP,
  "userId" UUID,
  "authToken" VARCHAR(32),
  "login" VARCHAR(50)
)

CREATE TABLE [buhta_auth_User](
  [userId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
  [login] NVARCHAR(50),
  [password] NVARCHAR(50),
  [isAdmin] BIT
)

INSERT INTO [buhta_auth_User]([userId],[login],[password],[isAdmin]) 
VALUES(CONVERT(UNIQUEIDENTIFIER,'83483ab8-a374-48e0-9b36-cc42dac923e9'),N'admin',N'password',1)

CREATE TABLE `buhta_auth_User`(
  `userId` BINARY(16) NOT NULL PRIMARY KEY,
  `login` VARCHAR(50),
  `password` VARCHAR(50),
  `isAdmin` BIT
)

INSERT INTO `buhta_auth_User`(`userId`,`login`,`password`,`isAdmin`) 
VALUES(convert(0x83483ab8a37448e09b36cc42dac923e9,binary(16)),'admin','password',1)

CREATE TABLE [buhta_auth_Session](
  [sessionId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
  [clientIp] NVARCHAR(50),
  [buhtaServerName] NVARCHAR(50),
  [createTime] DATETIME2,
  [lastTime] DATETIME2,
  [userId] UNIQUEIDENTIFIER,
  [authToken] NVARCHAR(32),
  [login] NVARCHAR(50)
)

CREATE TABLE `buhta_auth_Session`(
  `sessionId` BINARY(16) NOT NULL PRIMARY KEY,
  `clientIp` VARCHAR(50),
  `buhtaServerName` VARCHAR(50),
  `createTime` DATETIME(3),
  `lastTime` DATETIME(3),
  `userId` BINARY(16),
  `authToken` VARCHAR(32),
  `login` VARCHAR(50)
)

CREATE TABLE "buhta_auth_Session"(
  "sessionId" UUID NOT NULL PRIMARY KEY,
  "clientIp" VARCHAR(50),
  "buhtaServerName" VARCHAR(50),
  "createTime" TIMESTAMP,
  "lastTime" TIMESTAMP,
  "userId" UUID,
  "authToken" VARCHAR(32),
  "login" VARCHAR(50)
)

CREATE TABLE [buhta_auth_User](
  [userId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
  [login] NVARCHAR(50),
  [password] NVARCHAR(50),
  [isAdmin] BIT
)

INSERT INTO [buhta_auth_User]([userId],[login],[password],[isAdmin]) 
VALUES(CONVERT(UNIQUEIDENTIFIER,'83483ab8-a374-48e0-9b36-cc42dac923e9'),N'admin',N'password',1)

CREATE TABLE `buhta_auth_User`(
  `userId` BINARY(16) NOT NULL PRIMARY KEY,
  `login` VARCHAR(50),
  `password` VARCHAR(50),
  `isAdmin` BIT
)

INSERT INTO `buhta_auth_User`(`userId`,`login`,`password`,`isAdmin`) 
VALUES(convert(0x83483ab8a37448e09b36cc42dac923e9,binary(16)),'admin','password',1)

CREATE TABLE "buhta_auth_User"(
  "userId" UUID NOT NULL PRIMARY KEY,
  "login" VARCHAR(50),
  "password" VARCHAR(50),
  "isAdmin" boolean
)

INSERT INTO "buhta_auth_User"("userId","login","password","isAdmin") 
VALUES(UUID '83483ab8-a374-48e0-9b36-cc42dac923e9','admin','password',TRUE)
CREATE TABLE "buhta_auth_User"(
  "userId" UUID NOT NULL PRIMARY KEY,
  "login" VARCHAR(50),
  "password" VARCHAR(50),
  "isAdmin" boolean
)

INSERT INTO "buhta_auth_User"("userId","login","password","isAdmin") 
VALUES(UUID '83483ab8-a374-48e0-9b36-cc42dac923e9','admin','password',TRUE)
 */
