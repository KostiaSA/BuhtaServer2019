using System;

namespace HandlebarsDotNet.Compiler
{
    public class HandlebarsUndefinedBindingException : Exception
    {
        // buhta
        public HandlebarsUndefinedBindingException(string path, string missingKey) : base("невозможно вычислить SQL-шаблон {{"+path+ "}}")
        {
            this.Path = path;
            this.MissingKey = missingKey;
        }

        public string Path { get; set; }

        public string MissingKey { get; set; }
    }
}
