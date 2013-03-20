using System;
using System.IO;
using System.Linq;
using System.Text;
using CommandLineParser.Exceptions;
using Microsoft.SqlServer.Management.Smo;

namespace ExportSQL
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var parser = new CommandLineParser.CommandLineParser();
            var commandParams = new CommandParams();
            parser.ExtractArgumentAttributes(commandParams);

            try
            {
                parser.ParseCommandLine(args);
                // For Debugging
                //parser.ShowParsedArguments();
            }
            catch (CommandLineException e)
            {
                Console.WriteLine(e.Message);
                parser.ShowUsage();
                return;
            }

            try
            {
                DeleteFile(commandParams.outputFilePath);
                ExtractScript(commandParams);
            }
            catch (Exception e)
            {
                Console.WriteLine("error : " + e);
            }
        }

        private static void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }

        private static void ExtractScript(CommandParams commandParams)
        {
            var server = new Server(commandParams.connection);
            server.ConnectionContext.LoginSecure = false;
            server.ConnectionContext.Login = commandParams.id;
            server.ConnectionContext.Password = commandParams.password;

            var scripter = new Scripter(server);
            SetScriptOptions(commandParams.outputFilePath, scripter);

            var db = server.Databases[commandParams.dbName];

            CreateScriptFileHeader(commandParams.outputFilePath, commandParams.dbName);
            ExtractTableScript(scripter, db);
            ExtractStoredProcedureScript(scripter, db);
            ExtractViewsScript(scripter, db);
        }

        private static void CreateScriptFileHeader(string filePath, string instanceName)
        {
            var createdFile = File.Create(filePath);
            string fileHeader = "USE [" + instanceName + "]\r\n" + "GO\r\n";
            byte[] bytes = GetEncoding().GetBytes(fileHeader);
            createdFile.Write(bytes, 0, bytes.Count());
            createdFile.Close();
        }

        private static void SetScriptOptions(string filePath, Scripter scripter)
        {
            // File Options
            scripter.Options.AnsiFile = true;
            scripter.Options.ToFileOnly = true;
            scripter.Options.FileName = filePath;
            scripter.Options.AppendToFile = true;

            // Key Options
            scripter.Options.NoCollation = true;
            scripter.Options.DriDefaults = true;
            scripter.Options.DriForeignKeys = true;
            scripter.Options.DriIndexes = true;
            scripter.Options.Indexes = true;

            // Other Options
            scripter.Options.IncludeHeaders = true;
            // USE Database 옵션 사용 안함(append 속성 때문에 CreateScriptFileHeader()로 대체)
            scripter.Options.IncludeDatabaseContext = false;
            scripter.Options.Encoding = GetEncoding();
            scripter.Options.WithDependencies = false;
        }

        private static Encoding GetEncoding()
        {
            return Encoding.UTF8;
        }

        private static void ExtractTableScript(Scripter scripter, Database db)
        {
            var dbCount = db.Tables.Count;
            if (dbCount == 0)
            {
                Console.WriteLine("Table was not exist");
                return;
            }

            var tables = new Table[dbCount];
            db.Tables.CopyTo(tables, 0);

            var tree = scripter.DiscoverDependencies(tables, true);
            var dependencyWalker = new DependencyWalker();
            var dependencyCollection = dependencyWalker.WalkDependencies(tree);

            foreach (var dependencyCollectionNode in dependencyCollection)
            {
                var dbName = dependencyCollectionNode.Urn.GetAttribute("Name");
                foreach (var table in db.Tables.Cast<Table>().Where(table => dbName == table.Name))
                {
                    Console.WriteLine("Table Names : " + table.Name);
                    table.Script(scripter.Options);
                }
            }
        }

        private static void ExtractStoredProcedureScript(Scripter scripter, Database db)
        {
            var storedProceduresList = db.StoredProcedures.Cast<StoredProcedure>().Where(storedProcedure => !storedProcedure.IsSystemObject).ToList();

            if (storedProceduresList.Count == 0)
            {
                Console.WriteLine("Stored Procedure was not exist");
                return;
            }

            foreach (var storedProcedure in storedProceduresList)
            {
                Console.WriteLine("Stored Procedure Name : " + storedProcedure.Name);
                storedProcedure.Script(scripter.Options);
            }
        }

        private static void ExtractViewsScript(Scripter scripter, Database db)
        {
            var viewList = db.Views.Cast<View>().Where(view => !view.IsSystemObject).ToList();

            if (viewList.Count == 0)
            {
                Console.WriteLine("View was not exist");
                return;
            }

            foreach (var view in viewList)
            {
                Console.WriteLine("View Name : " + view.Name);
                view.Script(scripter.Options);
            }
        }
    }
}