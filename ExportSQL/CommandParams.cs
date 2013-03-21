using CommandLineParser.Arguments;
using CommandLineParser.Validation;

namespace ExportSQL
{
    [ArgumentGroupCertification("o,c,i,p,d", EArgumentGroupCondition.AllUsed)]
    class CommandParams
    {
        [SwitchArgument('?', "help", true, Description = "show usage")]
        public bool help;

        [ValueArgument(typeof (string), 'o', "outputFilePath", Description = "Set output file path")]
        public string outputFilePath;

        [ValueArgument(typeof(string), 'c', "connection", Description = "Set server 'address,port'")]
        public string connection;

        [ValueArgument(typeof(string), 'i', "id", Description = "Set login user id")]
        public string id;

        [ValueArgument(typeof(string), 'p', "password", Description = "Set login password")]
        public string password;

        [ValueArgument(typeof(string), 'd', "dbName", Description = "Set database name")]
        public string dbName;

        [SwitchArgument('u', "useDbContext", true, Description = "Set use database context[true, false]")]
        public bool dbcontext;
    }
}
