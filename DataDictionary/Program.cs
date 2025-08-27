using System;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;

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

            string connectionString = String.Format("Data Source={2}; Initial Catalog={3}; User ID={0}; Password={1}", userName, password, serverName, dbName);
            
            //Read all templates.
            FileStream fsDefaultTemplate = new FileStream(Environment.CurrentDirectory + @"\Templates\defaultTemplate.html", FileMode.Open, FileAccess.Read);
            StreamReader swDefaultTemplate = new StreamReader(fsDefaultTemplate);

            string defaultTemplateContent = swDefaultTemplate.ReadToEnd();
            swDefaultTemplate.Close();
            swDefaultTemplate = null;
            fsDefaultTemplate.Close();
            fsDefaultTemplate = null;
        
            FileStream fsTableList = new FileStream(Environment.CurrentDirectory + @"\Templates\TablesListTemplate.html", FileMode.Open, FileAccess.Read);
            StreamReader swTableList = new StreamReader(fsTableList);

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
    }
}
