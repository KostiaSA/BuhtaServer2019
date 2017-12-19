using HandlebarsDotNet;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuhtaServer.test
{
    public class TestHandlebars
    {

        public static void test1()
        {
            Console.WriteLine("-------- начинаем ------------");
            Console.WriteLine(BitConverter.ToString(Guid.NewGuid().ToByteArray()).Replace("-",""));
            Console.WriteLine(BitConverter.ToString(Convert.FromBase64String("0KLQtdGB0YLQkdCw0LfQsDY0LVRlc3RCYXNlNjQ=")).Replace("-", ""));

            ;

            var objStr = @"

{
  ""guid1"": ""<Guid>8D950D6B-0929-4DE1-B79C-EB06AB932CAF"",
  ""pic1"": ""<Uint8Array>0KLQtdGB0YLQkdCw0LfQsDY0LVRlc3RCYXNlNjQ="",
  ""createdDate"": ""<Date>2017-09-25 11:03:45.046"",
  ""createdDateTime"": ""<DateTime>0001-09-26 11:03:45.046"",
  ""arr"":[1,""жо'па"",null,567], 
  ""bol1"":true, 
  ""bol0"":false, 
  ""болbolй0"":true, 
  ""int1"":657454, 
  ""float2"":657454.7865, 
  ""description"": ""Справ'очник\"" орган\nизаций"",
  ""name"": ""СписокОрганизаций"",
  ""objectType"": ""query"",
  ""root"": {
                ""key"": ""key1"",
    ""tableId"":""buhta/core-tests/schema/Организация.table"",
    ""children"":[
      {
        ""key"": ""key2"",
        ""fieldSource"":""Номер""
      },
      {
        ""key"": ""key3"",
        ""fieldSource"":""Название""
      },
      {
        ""key"": ""key4"",
        ""fieldSource"":""Директор"",
        ""tableId"":""buhta/core-tests/schema/Сотрудник.table"",
        ""children"":[
          {
            ""key"": ""key5"",
            ""fieldSource"":""Номер"",
            ""fieldCaption"":""ДирНомер""
          },
          {
            ""key"": ""key6"",
            ""fieldSource"":""Фамилия"",
            ""fieldCaption"":""ДирФамилия""
          }
        ]
      }
    ]
  }
}


";



            Console.WriteLine(String.Join("\n =========================\n", SqlTemplate.emitSqlBatchFromTemplatePath("mssql", "buhta/core-tests/sql/test-query.sql", JObject.Parse(objStr))));
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(String.Join("\n=========================\n", SqlTemplate.emitSqlBatchFromTemplatePath("mysql", "buhta/core-tests/sql/test-query.sql", JObject.Parse(objStr))));
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(String.Join("\n=========================\n", SqlTemplate.emitSqlBatchFromTemplatePath("postgres", "buhta/core-tests/sql/test-query.sql", JObject.Parse(objStr))));


            Console.ReadKey();
            string[] x;
            for (int i = 0; i < 1000; i++)
            {
                Console.WriteLine("Ok---------------" + i);
                x = SqlTemplate.emitSqlBatchFromTemplatePath("mssql", "buhta/core-tests/sql/test-query.sql", JObject.Parse(objStr));
            }
            Console.WriteLine("Ok---------------");

            Console.ReadKey();
        }


        public static void test2()
        {
            Console.WriteLine("-------- начинаем ------------");
            Console.ReadKey();

            string source =
            @"<div class=""entry"">
  <h1>{{title}}</h1>
  <div class=""body"">
    {{body}}
  </div>
  <div class=""body"">
    {{body}}
  </div>
</div>";

            var template = Handlebars.Compile(source);

            var data = new
            {
                title = "My new post",
                body = "This is my first post!"
            };

            var result = template(data);

            Console.WriteLine(result);

            Console.ReadKey();

        }
    }
}
