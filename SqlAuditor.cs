using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace sqlaudit_runner
{
    public class SqlAuditor
    {
        protected  const string LOG_TYPE = "EncryptionExceptions";
        ILogger _Logger;
        IConfiguration _Config;
        

        public SqlAuditor(ILoggerFactory logger, IConfiguration config) {

            _Logger = logger.CreateLogger<SqlAuditor>();
            _Config = config;
        }

        public void Run() 
        {
            List<AuditRecord> badColumns = GetConfidentialNotEncrypedRecords();

            if (badColumns.Count == 0)
            {
                _Logger.LogInformation("No Exceptions found in this Run");
                return;
            }

            SendExceptionsToLogAnalytics(badColumns);
        }

        /**
         *         SEND DATA TO LOG ANALYTICS
         */
        private void SendExceptionsToLogAnalytics(List<AuditRecord> badColumns)
        {
            string workspaceId = _Config.GetValue<String>("LAWorkspaceId");
            // For sharedKey, use either the primary or the secondary Connected Sources client authentication key   
            string workspaceKey = _Config.GetValue<String>("LAKey");

            _Logger.LogInformation("Sending found records to LogAnalytics Id: {0}", workspaceId);

            LogAnalyticsWrapper lasender = new LogAnalyticsWrapper(workspaceId, workspaceKey);
            string result = lasender.SendLogEntries<AuditRecord>(badColumns, LOG_TYPE);

            _Logger.LogInformation("Sent found records to LogAnalytics HTTP Status: {0}", result);
        }

        /**
        *      RUN AUDIT QUERY
        */
        protected List<AuditRecord> GetConfidentialNotEncrypedRecords() 
        {
            List<AuditRecord> badGuysList = new List<AuditRecord>();

            try
            {
                var ConnectionString = _Config.GetConnectionString("DBToAudit");

                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    Console.WriteLine("\nConnecting to  Database {0}:", connection.Database);
                    Console.WriteLine("=========================================\n");

                    connection.Open();

                    // Construct Query to see sensitive columns that are not ecrypted
                    StringBuilder sb = new StringBuilder();
                    sb.Append("SELECT schema_name(O.schema_id) AS schema_name, ");
                    sb.Append("O.NAME AS table_name,");
                    sb.Append("C.NAME AS column_name,");
                    sb.Append("information_type,label,encryption_type, encryption_type_desc, encryption_algorithm_name,");
                    sb.Append("column_encryption_key_id,column_encryption_key_database_name ");
                    sb.Append("FROM sys.sensitivity_classifications sc ");
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
                                AuditRecord record = new AuditRecord
                                {
                                    SchemaName = reader.GetStringNullCheck(0),
                                    TableName = reader.GetStringNullCheck(1),
                                    ColumnName = reader.GetStringNullCheck(2),
                                    InfromationType = reader.GetStringNullCheck(3),
                                    Label = reader.GetStringNullCheck(4),
                                    EncryptionType = reader.GetStringNullCheck(5),
                                    EncryptionAlgorithm = reader.GetStringNullCheck(7),
                                    EncryptionKeyId = reader.GetStringNullCheck(8)
                                };
                                badGuysList.Add(record);
                            }
                        }
                        _Logger.LogInformation("Found Not Encrypted sensitive data Columns {0}", badGuysList.Count);
                    }
                }
            }
            catch (SqlException e)
            {
                _Logger.LogError(e.ToString());
                throw e;
            }

            return badGuysList;
        }
    }

    static class DataReaderExtensions
    {
        public static string GetStringNullCheck(this IDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}
