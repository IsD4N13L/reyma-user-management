using Microsoft.Data.SqlClient;
using Serilog;
using System.Text;
using System.Text.RegularExpressions;

namespace UserManagement.Resources
{
    public static class QueryKitToSqlConverter
    {
        public static string ConvertToSqlWhereClause(string queryKitFilter)
        {
            if (string.IsNullOrEmpty(queryKitFilter))
            {
                return "1 = 1";
            }

            string sqlFilter = queryKitFilter;

            // Eliminar espacios en blanco y caracteres ocultos
            sqlFilter = Regex.Replace(sqlFilter, @"\s+", " ").Trim();

            // Operadores de conteo
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)#==", "COUNT(*) = ");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)#!=", "COUNT(*) <> ");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)#>", "COUNT(*) > ");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)#<", "COUNT(*) < ");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)#>=", "COUNT(*) >= ");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)#<=", "COUNT(*) <= ");

            // Operadores de coincidencia con distinción de mayúsculas y minúsculas
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)==(?!\*)", "=");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!=(?!\*)", "<>");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)>=(?!\*)", ">=");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)<=(?!\*)", "<=");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)>(?!\*)", ">");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)<(?!\*)", "<");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)_=(?!\*)", "LIKE '");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!_=(?!\*)", "NOT LIKE '");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)_-\=(?!\*)", "LIKE '%");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!_-\=(?!\*)", "NOT LIKE '%");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)\@=(?!\*)", "LIKE '%");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!\@=(?!\*)", "NOT LIKE '%");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)\~\~(?!\*)", "SOUNDEX(");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!\~\~(?!\*)", "SOUNDEX(");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)\^\$", "IN (");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!\^\$", "NOT IN (");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)\^\^", "IN (");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!\^\^", "NOT IN (");

            // Operadores de coincidencia sin distinción de mayúsculas y minúsculas
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)==\*", "LIKE '");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!=\*", "NOT LIKE '");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)_\=\*", "LIKE '");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!_\=\*", "NOT LIKE '");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)_\-\=\*", "LIKE '%");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!_\-\=\*", "NOT LIKE '%");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)\@\=\*", "LIKE '%");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!\@\=\*", "NOT LIKE '%");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)\^\$\*", "IN (");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!\^\$\*", "NOT IN (");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)\^\^\*", "IN (");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)!\^\^\*", "NOT IN (");

            // Reemplazar operadores lógicos
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)&&", "AND");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)\|\|", "OR");

            // Agregar comodines para LIKE y SOUNDEX
            sqlFilter = Regex.Replace(sqlFilter, @"LIKE '(.*?)'", "LIKE '$1%'");
            sqlFilter = Regex.Replace(sqlFilter, @"SOUNDEX\((.*?)\)", "SOUNDEX($1)) = SOUNDEX('");
            sqlFilter = Regex.Replace(sqlFilter, @"SOUNDEX\((.*?)\) = SOUNDEX\('(.*?)'\)", "SOUNDEX($1) = SOUNDEX('$2')");

       
            // Manejar listas de valores para IN/NOT IN
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)(IN|NOT IN)\s*\(\s*\[(.*?)\]", m =>
            {
                string operatorName = m.Groups[1].Value.ToUpper();
                string values = m.Groups[2].Value;
                // Eliminar espacios en blanco dentro de la lista
                values = Regex.Replace(values, @"\s+", "");
                string[] valueArray = values.Split(',');
                string sqlValues = "";
                foreach (string value in valueArray)
                {
                    string trimmedValue = value.Trim();
                    if (trimmedValue.StartsWith("\"") && trimmedValue.EndsWith("\""))
                    {
                        sqlValues += $"'{trimmedValue.Trim('"')}', ";
                    }
                    else
                    {
                        sqlValues += $"{trimmedValue}, ";
                    }
                }
                return $"{operatorName} ({sqlValues.TrimEnd(',', ' ')})";
            });

            // Manejo de valores booleanos
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)true", "1");
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)false", "0");

            // Manejo de fechas
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z)", "'$1'");

            // Agregar COLLATE SQL_Latin1_General_CP1_CI_AI para comparaciones sin distinción de mayúsculas y minúsculas
            sqlFilter = Regex.Replace(sqlFilter, @"(?i)(LIKE|NOT LIKE) '(.*?)'", m => $"{m.Groups[1].Value} '{m.Groups[2].Value}' COLLATE SQL_Latin1_General_CP1_CI_AI");


            return sqlFilter;
        }

        public static (string whereClause, List<SqlParameter> parameters) GenerateWhereClause(string filter)
        {

            Log.Information("Filter {0}", filter);

            var parameters = new List<SqlParameter>();
            var output = new StringBuilder();
            int paramIndex = 0;
            var operatorStack = new Stack<string>();
            var tokenQueue = new Queue<string>(Tokenize(filter));

            var operatorsMap = new Dictionary<string, (string sql, Func<string, string, string> format)>()
        {
            { "==", ("= ", (col, val) => $"{col} = {val}") },
            { ">=", (">= ", (col, val) => $"{col} >= {val}") },
            { "<=", ("<= ", (col, val) => $"{col} <= {val}") },
            { "<", ("< ", (col, val) => $"{col} < {val}") },
            { ">", ("> ", (col, val) => $"{col} > {val}") },
            { "_=", ("LIKE ", (col, val) => $"{col} LIKE {val} + '%'") }, // StartsWith
            { "&&", ("AND", null) },
            { "||", ("OR", null) },
            { "^^", ("IN", (col, val) => $"{col} IN ({val})") }
        };

            while (tokenQueue.Count > 0)
            {
                var token = tokenQueue.Dequeue().Trim();

                if (token == "(")
                {
                    operatorStack.Push(token);
                    output.Append("(");
                }
                else if (token == ")")
                {
                    while (operatorStack.Peek() != "(")
                    {
                        output.Append($" {operatorStack.Pop()} ");
                    }
                    operatorStack.Pop(); // Remove '('
                    output.Append(")");
                }
                else if (operatorsMap.ContainsKey(token))
                {
                    while (operatorStack.Count > 0 && operatorStack.Peek() != "(")
                    {
                        output.Append($" {operatorStack.Pop()} ");
                    }
                    operatorStack.Push(token);
                }
                else // Column, Value, o Operando
                {
                    var column = token;
                    var opToken = tokenQueue.Dequeue().Trim();
                    var valueToken = tokenQueue.Dequeue().Trim();

                    if (operatorsMap.TryGetValue(opToken, out var opInfo))
                    {
                        string paramName = $"@p{paramIndex++}";
                        object parsedValue = ParseValue(valueToken);
                        parameters.Add(new SqlParameter(paramName, parsedValue));

                        string condition = opInfo.format != null
                            ? opInfo.format(column, paramName)
                            : $"{column} {opInfo.sql}{paramName}";

                        output.Append(condition);
                    }
                }
            }

            while (operatorStack.Count > 0)
            {
                output.Append($" {operatorStack.Pop()} ");
            }

            return (output.ToString().Trim(), parameters);
        }

        private static IEnumerable<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            var buffer = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in input)
            {
                if (c == '"' || c == '\'') inQuotes = !inQuotes;

                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (buffer.Length > 0)
                    {
                        tokens.Add(buffer.ToString());
                        buffer.Clear();
                    }
                }
                else if (IsOperatorChar(c) && !inQuotes)
                {
                    if (buffer.Length > 0)
                    {
                        tokens.Add(buffer.ToString());
                        buffer.Clear();
                    }
                    tokens.Add(c.ToString());
                }
                else
                {
                    buffer.Append(c);
                }
            }

            if (buffer.Length > 0) tokens.Add(buffer.ToString());
            return tokens;
        }

        private static bool IsOperatorChar(char c) => "=<>!&|".Contains(c);
        private static object ParseValue(string value) => value switch
        {
            var v when v.StartsWith("\"") || v.StartsWith("'") => v.Trim('"', '\''),
            var v when v == "true" || v == "false" => bool.Parse(v),
            var v when int.TryParse(v, out int num) => num,
            _ => value
        };
    }
}
