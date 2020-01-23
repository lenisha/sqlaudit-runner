using System;
using System.Collections.Generic;
using System.Text;

namespace sqlaudit_runner
{
    public class AuditRecord
    {
        public string SchemaName { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string InfromationType { get; set; }
        public string Label { get; set; }

        // Not populated for noew (as we output those that are not encrypted)
        public string EncryptionType { get; set; }
        public string EncryptionAlgorithm { get; set; }
        public string EncryptionKeyId { get; set; }
    }

}
