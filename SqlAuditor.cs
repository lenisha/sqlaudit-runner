using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace sqlaudit_runner
{
    public class SqlAuditor
    {
        ILoggerFactory _LoggerFactory;
        IConfiguration _Config; 


        public SqlAuditor(ILoggerFactory logger, IConfiguration config) {

            _LoggerFactory = logger;
            _Config = config;
        }

        public void Run() 
        {
            try
            {
                
                var ConnectionString = _Config.GetConnectionString("DBToAudit");
          
                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    Console.WriteLine("\nConnecting to {0}:", ConnectionString);
                    Console.WriteLine("=========================================\n");

                    connection.Open();
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT schema_name(O.schema_id) AS schema_name, ");
                    sb.Append("O.NAME AS table_name,");
                    sb.Append("C.NAME AS column_name,");
                    sb.Append("information_type,label,encryption_type, encryption_type_desc, encryption_algorithm_name,");
                    sb.Append("column_encryption_key_id,column_encryption_key_database_name ");
                    sb.Append(" FROM sys.sensitivity_classifications sc ");
                    sb.Append("JOIN sys.objects O ON  sc.major_id = O.object_id ");
                    sb.Append("JOIN sys.columns C ON  sc.major_id = C.object_id  AND sc.minor_id = C.column_id ");
                    sb.Append("where encryption_type is null");
                  
                    String sql = sb.ToString();

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine("{0} {1}", reader.GetString(0), reader.GetString(1));
                            }
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
