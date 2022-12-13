using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Agora.SDK;

namespace addresses
{
    public class DataManager
    {
        public static DataManager Instance { get; } = new();
        public string OutputDir { get; }
        public string InputDir { get; }

        public TFTData? CurrentYearDB;

        public Dictionary<string, Dictionary<string, List<string>>> Errors = new();

        public static NameIndexer NameIndexer { get; } = new NameIndexer();

        private DataManager() 
        {
            OutputDir = Config["Data:OutputDir"];
            Directory.CreateDirectory(OutputDir);

            InputDir = Config["Data:InputDir"];
            if (!Directory.Exists(InputDir))
                throw new Exception("Input directory not found.");
        }

        public void Initialize()
        {
            CurrentYearDB = new(Config["Data:TFTFilename"]);
            Errors.Clear();
        }

        public void Analyze()
        {            
            if (CurrentYearDB == null)
                throw new NullReferenceException("CurrentYearDB is null.");

            $"Analyzing Data for '{CurrentYearDB.TFTFilename}'".LogInfo();

            ChildFirstNameAnalyzer.Run(CurrentYearDB!);

            DuplicatesAnalyzer dupAn = new(CurrentYearDB!);
            dupAn.Run();

            SimilarEntryAnalyzer simAn = new(CurrentYearDB!);
            simAn.Run();

            FraudAnalyzer fraudAn = new(CurrentYearDB!);
            fraudAn.Run();

            StatisticsAnalyzer statistics = new();
            statistics.Run();

            BannedAnalyzer bannedAnalyzer = new();
            bannedAnalyzer.Run();

            $"Analyzing Data for '{CurrentYearDB.TFTFilename}' - Done".LogInfo();
        }

        internal void AddErrors(string filename, string controlNumber, List<string> errors)
        {
            if( !Errors.TryGetValue(filename, out var dict ))
            {
                dict = Errors[filename] = new();
            }
            if( !dict.TryGetValue(controlNumber, out var list))
            {
                list = dict[controlNumber] = new List<string>();
            }
            list.AddRange(errors);
        }

        public void Register()
        {
            Registrar registrar = new();

            registrar.Run();
        }
    }
}
