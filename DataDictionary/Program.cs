using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Smo;

namespace DataDictionary
{
    class Program
    {
        static string serverName = "";
        static string dbName = "";
        static string userName = "";
        static string password = "";

        static void Main(string[] args)
        {
            ReadArguments(args);

            string defaultTemplateContent = ReadTemplate(@"\Templates\defaultTemplate.html");
            string tablesListTemplateContent = ReadTemplate(@"\Templates\TablesListTemplate.html");
            string tableDetailsListTemplateContent = ReadTemplate(@"\Templates\TableDetailsListTemplate.html");
            string columnsListTemplateContent = ReadTemplate(@"\Templates\ColumnsListTemplate.html");

            string connectionString = String.Format("Data Source={2}; Initial Catalog={3}; User ID={0}; Password={1}", userName, password, serverName, dbName);

            StringBuilder sbTablesListContent = new StringBuilder();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string tableQuery = @"SELECT object_id AS TableId, name AS TableName FROM sys.tables ORDER BY 2";
                
                string columnQuery = @"SELECT t.name AS TableName, clmns.name AS [ColumnName], 
	                        CAST(ISNULL(cik.index_column_id, 0) AS bit) AS [InPrimaryKey],
	                        CAST(ISNULL((select TOP 1 1 from sys.foreign_key_columns AS colfk where colfk.parent_column_id = clmns.column_id and colfk.parent_object_id = clmns.object_id), 0) AS bit) AS [IsForeignKey],
	                        usrt.name AS [DataType],
	                        ISNULL(baset.name, N'') AS [SystemType],
	                        CAST(CASE WHEN baset.name IN (N'nchar', N'nvarchar') AND clmns.max_length <> -1 THEN clmns.max_length/2 ELSE clmns.max_length END AS int) AS [Length],
	                        CAST(clmns.precision AS int) AS [NumericPrecision],
	                        CAST(clmns.scale AS int) AS [NumericScale],
	                        clmns.is_nullable AS [Nullable]
                        FROM sys.all_columns AS clmns 
	                        INNER JOIN sys.tables AS t ON t.object_id = clmns.object_id
	                        LEFT OUTER JOIN sys.indexes AS ik ON ik.object_id = clmns.object_id and 1 = ik.is_primary_key
	                        LEFT OUTER JOIN sys.index_columns AS cik ON cik.index_id = ik.index_id and cik.column_id = clmns.column_id and cik.object_id = clmns.object_id and 0 = cik.is_included_column
	                        LEFT OUTER JOIN sys.types AS usrt ON usrt.user_type_id = clmns.user_type_id
	                        LEFT OUTER JOIN sys.types AS baset ON (baset.user_type_id = clmns.system_type_id and baset.user_type_id = baset.system_type_id) or ((baset.system_type_id = clmns.system_type_id) and (baset.user_type_id = clmns.user_type_id) and (baset.is_user_defined = 0) and (baset.is_assembly_type = 1)) 
	                        LEFT OUTER JOIN sys.xml_schema_collections AS xscclmns ON xscclmns.xml_collection_id = clmns.xml_collection_id
	                        LEFT OUTER JOIN sys.schemas AS s2clmns ON s2clmns.schema_id = xscclmns.schema_id
                        ORDER BY t.name, clmns.column_id ASC";

                string defaultValueQuery = @"SELECT t.name AS TableName, c.name AS ColumnName, d.definition AS DefaultValue
                        FROM sys.columns c 
                            INNER JOIN sys.default_constraints d ON c.default_object_id = d.object_id
                            INNER JOIN sys.tables AS t ON t.object_id = c.object_id
						ORDER BY t.name, c.name";

            string tableListTemplateContent = swTableList.ReadToEnd();
            swTableList.Close();
            swTableList = null;
            fsTableList.Close();
            fsTableList = null;

            FileStream fsTableDetailsList = new FileStream(Environment.CurrentDirectory + @"\Templates\TableDetailsListTemplate.html", FileMode.Open, FileAccess.Read);
            StreamReader swTableDetailsList = new StreamReader(fsTableDetailsList);

            string tableDetailsListTemplateContent = swTableDetailsList.ReadToEnd();
            swTableDetailsList.Close();
            swTableDetailsList = null;
            fsTableDetailsList.Close();
            fsTableDetailsList = null;

            FileStream fsColumnsList = new FileStream(Environment.CurrentDirectory + @"\Templates\ColumnsListTemplate.html", FileMode.Open, FileAccess.Read);
            StreamReader swColumnsList = new StreamReader(fsColumnsList);

            string columnsListTemplateContent = swColumnsList.ReadToEnd();
            swColumnsList.Close();
            swColumnsList = null;
            fsColumnsList.Close();
            fsColumnsList = null;

            // Create TableList
            StringBuilder sbTablesListContent = new StringBuilder();
            StringBuilder sbTableDetailsListContent = new StringBuilder();
            StringBuilder sbColumnsListContent = new StringBuilder();
            
            foreach (Table objTable in objDatabase.Tables)
            {
                string tableContent = tableListTemplateContent;
                tableContent = tableContent.Replace("{TableId}", objTable.ID.ToString());
                tableContent = tableContent.Replace("{TableName}", objTable.Name);
                tableContent = tableContent.Replace("{CreateDate}", objTable.CreateDate.ToString());
                
                string tableDetailsContent = tableDetailsListTemplateContent;
                tableDetailsContent = tableDetailsContent.Replace("{TableId}", objTable.ID.ToString());
                tableDetailsContent = tableDetailsContent.Replace("{TableName}", objTable.Name);

                sbColumnsListContent.Clear();
                //Retrieve Table Properties
                foreach (Column col in objTable.Columns)
                {
                    string columnContent = columnsListTemplateContent;
                    string columnPK = String.Empty;
                    if (col.InPrimaryKey)
                    {
                        columnPK = "Yes";
                    }
                    columnContent = columnContent.Replace("{ColumnPK}", columnPK);
                    columnContent = columnContent.Replace("{ColumnName}", col.Name);
                    columnContent = columnContent.Replace("{ColumnDataType}", col.DataType.Name);
                    columnContent = columnContent.Replace("{ColumnSize}", col.DataType.MaximumLength.ToString());
                    string columnIdentity = String.Empty;
                    if (col.Identity)
                    {
                        columnIdentity = "Yes";
                    }
                    columnContent = columnContent.Replace("{ColumnIdentity}", columnIdentity);
                    string columnNullable = String.Empty;
                    if (col.Nullable)
                    {
                        columnNullable = "Yes";
                    }
                    columnContent = columnContent.Replace("{ColumnAllowNull}", columnNullable);
                    columnContent = columnContent.Replace("{ColumnDefault}", col.DefaultConstraint?.Text);

                    string columnDescription = String.Empty;
                    if (col.ExtendedProperties.Count > 0 && col.ExtendedProperties["MS_Description"] != null)
                    {
                        columnDescription = col.ExtendedProperties["MS_Description"].Value.ToString();
                    }
                    columnContent = columnContent.Replace("{ColumnDescription}", columnDescription);

                    sbColumnsListContent.Append(columnContent);
                }

                tableContent = tableContent.Replace("{ColumnsList}", sbColumnsListContent.ToString());
                tableDetailsContent = tableDetailsContent.Replace("[ColumnsList]", sbColumnsListContent.ToString());
                defaultTemplateContent = defaultTemplateContent.Replace("{ColumnsList}", sbColumnsListContent.ToString());

                sbTablesListContent.Append(tableContent);
                sbTableDetailsListContent.Append(tableDetailsContent);                
            }

            //Create default.html page.
            defaultTemplateContent = defaultTemplateContent.Replace("{TableCount}", objDatabase.Tables.Count.ToString());
            defaultTemplateContent = defaultTemplateContent.Replace("{TablesList}", sbTablesListContent.ToString());
            defaultTemplateContent = defaultTemplateContent.Replace("{TableDetailsList}", sbTableDetailsListContent.ToString());

            FileStream fsDefault = new FileStream("default.html", FileMode.Create, FileAccess.Write);
            StreamWriter swDefault = new StreamWriter(fsDefault);

            swDefault.Write(defaultTemplateContent);

            swDefault.Close();
            swDefault = null;
            fsDefault.Close();
            fsDefault = null;
            ExitOut(0);
        }

        static void ReadArguments(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
            }

            foreach (string arg in args)
            {
                switch (arg.Substring(0, 2).ToUpper())
                {
                    case "/S":
                    case "-S":
                        serverName = arg.Substring(2).Trim();
                        break;
                    case "/U":
                    case "-U":
                        userName = arg.Substring(2).Trim();
                        break;
                    case "/P":
                    case "-P":
                        password = arg.Substring(2).Trim();
                        break;
                    case "/D":
                    case "-D":
                        dbName = arg.Substring(2).Trim();
                        break;
                    case "/H":
                    case "-H":
                    case "/?":
                    case "-?":
                        ShowHelp();
                        break;
                    default:
                        // do other stuff...
                        break;
                }
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Start Usage Details:");
            Console.WriteLine("Pass the following arguments:");
            Console.WriteLine("-H, /H, -? or /?: This message");
            Console.WriteLine("-S or /S: SQL Server");
            Console.WriteLine("-U or /U: Username");
            Console.WriteLine("-P or /P: Password");
            Console.WriteLine("-D or /D: Database");
            Console.WriteLine("End Usage Details:");
            ExitOut(0);
        }

        static void ExitOut(int exitCodeToShow)
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(exitCodeToShow);
        }

        static string ReadTemplate(string templateFilePath)
        {
            FileStream fsTemplate = new FileStream(Environment.CurrentDirectory + templateFilePath, FileMode.Open, FileAccess.Read);
            StreamReader swTemplate = new StreamReader(fsTemplate);

            string templateContent = swTemplate.ReadToEnd();
            swTemplate.Close();
            swTemplate = null;
            fsTemplate.Close();
            fsTemplate = null;

            return templateContent;
        }

        static void WriteContent(string content, string directory, string fileName)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            FileStream fs = new FileStream(directory + "/" + fileName, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);

            sw.Write(content);

            sw.Close();
            sw = null;
            fs.Close();
            fs = null;
        }

        static DataTable PopulateDataTableWithQueryResults(SqlConnection connObj, string query)
        {
            DataTable dt = new DataTable();
            // Use SqlDataAdapter to fill the DataTable
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connObj))
            {
                adapter.Fill(dt);
            }

            return dt;
        }
    }
}
