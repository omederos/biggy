using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Massive;
using System.Diagnostics;

namespace Biggy.Tasks
{
    public class Transaction
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Comment { get; set; }
        public string Identifier { get; set; }
    }

    public class MassiveTasks
    {
        string _connectionString = "northwind";
        string _tableName = "Transactions";
        string _tablePkName = "TransactionId";

        public void RunBenchmarks()
        {
            var sw = new Stopwatch();
            var exists = this.TransactionTableExists();
            Console.WriteLine("The Transactions table exists? " + exists);
            if(!exists)
            {
                Console.WriteLine("Creating transactions table . . .");
                this.CreateTransctionTable();
            }

            this.ClearTransactionTable();

            int qtyRecords = 10000;

            Console.WriteLine("MEMORY DATA EXCERCISES:");
            Console.WriteLine("=======================");
            this.MemoryDataExcercises(qtyRecords);

            // Uncomment these to see some differences between standard
            // looped inserts and the new bulk method:

            //// Do some reads and writes with the "skinny" data set:
            //Console.WriteLine(Environment.NewLine);
            //Console.WriteLine("SKINNY DATA SET:");
            //Console.WriteLine("================");
            //var data = this.getSkinnyTransactionSet(qtySample);
            //this.WriteBulkTransactions(data);
            //this.ClearTransactionTable();
            //this.SlowWriteTransactions(data);

            //this.ClearTransactionTable();

            //// Now try the "fat" data set:
            //Console.WriteLine(Environment.NewLine);
            //Console.WriteLine("FAT DATA SET:");
            //Console.WriteLine("================");
            //data = this.getFatTransactionSet(qtySample);
            //this.WriteBulkTransactions(data);
            //this.ClearTransactionTable();
            //this.SlowWriteTransactions(data);


        }


        void DropTransctionTable()
        {
            string sql = ""
            + "DROP TABLE Transactions ";
            var Model = new DynamicModel(_connectionString);
            Model.Execute(sql);
        }


        void CreateTransctionTable()
        {
            string sql = ""
            + "CREATE TABLE Transactions "
            + "(TransactionId int IDENTITY(1,1) PRIMARY KEY NOT NULL, "
            + "Amount Money NOT NULL, "
            + "Comment Text NOT NULL, "
            + "Identifier Text NOT NULL)";

            var Model = new DynamicModel(_connectionString);
            Model.Execute(sql);
        }


        bool TransactionTableExists()
        {
            bool exists = false;
            string sql = ""
                + "SELECT * FROM INFORMATION_SCHEMA.TABLES "
                + "WHERE TABLE_SCHEMA = 'dbo' "
                + "AND  TABLE_NAME = 'Transactions'";
            var Model = new DynamicModel(_connectionString);
            var query = Model.Query(sql);
            if(query.Count() > 0)
            {
                exists = true;
            }
            return exists;
        }


        void LogOutput(string operation, int count, long millis)
        {
            string logMessage = "\t --> {0} {1} in {2} ms";
            Console.WriteLine(logMessage, operation, count, millis);
        }


        void LogOutput(string message)
        {
            Console.WriteLine("\t --> " + message);
        }


        public void MemoryDataExcercises(int qtyRecords)
        {
            // Start from fresh:
            this.DropTransctionTable();
            this.CreateTransctionTable();

            // Load the empty table:
            var transactions = new MassiveList<Transaction>(_connectionString, _tableName, _tablePkName);
            transactions.Clear();

            var sw = new Stopwatch();
            sw.Start();

            // Insert a bunch of records:
            var data = this.getSkinnyTransactionSet(qtyRecords);
            transactions.AddRange(data);
            sw.Stop();
            this.LogOutput("Wrote", qtyRecords, sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();

            // Find a record by arbitrary field (NOT the PK):
            var item = transactions.First(t => t.Identifier == "AA-9000");
            sw.Stop();
            this.LogOutput("Found single by field content", 1, sw.ElapsedMilliseconds);
            this.LogOutput("Found item: " + item.Identifier);


            sw.Reset();
            sw.Start();

            // Query against some criteria:
            var query = from t in transactions where (t.Amount > 100 && t.Amount < 150) select t;
            this.LogOutput("Read queried values from memory", query.Count(), sw.ElapsedMilliseconds);

            Console.WriteLine("Queried Values:");
            foreach(var trans in query)
            {
                this.LogOutput("Id: " + trans.TransactionId + " Comment: " + trans.Comment + " Amount: " + trans.Amount);
            }
            sw.Stop();
            this.LogOutput("Wrote queried values out to console", query.Count(), sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();

            // Update the queried records:
            foreach(var trans in query)
            {
                trans.Amount = trans.Amount * 2;
                transactions.Update(trans);
            }
            sw.Stop();
            this.LogOutput("Updated", query.Count(), sw.ElapsedMilliseconds);

            // Read the queried records to the console:
            foreach (var trans in query)
            {
                this.LogOutput("New amount = " + trans.Amount);
            }
        }


        public void ClearTransactionTable()
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("CLEAR TRANSACTIONS TABLE");

            var sw = new Stopwatch();
            sw.Start();
            var transactions = new MassiveList<Transaction>(_connectionString, _tableName, _tablePkName);
            int count = transactions.Count;
            sw.Stop();
            this.LogOutput("Read", count, sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            transactions.Clear();
            sw.Stop();
            this.LogOutput("Cleared", count, sw.ElapsedMilliseconds);
        }


        public MassiveList<Transaction> ReadTransactionTable()
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("READ FROM TRANSACTIONS TABLE");
            var sw = new Stopwatch();
            sw.Start();
            var transactions = new MassiveList<Transaction>(_connectionString, _tableName, _tablePkName);
            sw.Stop();
            this.LogOutput("Read", transactions.Count, sw.ElapsedMilliseconds);
            return transactions;
        }


        public void WriteBulkTransactions(List<Transaction> newTransactions)
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("WRITE BULK TRANSACTIONS");
            int qty = newTransactions.Count;

            var sw = new Stopwatch();
            sw.Start();
            var transactions = new MassiveList<Transaction>(_connectionString, _tableName, _tablePkName);
            sw.Stop();
            this.LogOutput("Read", transactions.Count, sw.ElapsedMilliseconds);
            sw.Reset();
            sw.Start();
            int added = transactions.AddRange(newTransactions);
            sw.Stop();
            this.LogOutput("Wrote", newTransactions.Count, sw.ElapsedMilliseconds);
        }


        public void SlowWriteTransactions(List<Transaction> newTransactions)
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("WRITE LOOPED TRANSACTIONS");
            var sw = new Stopwatch();
            sw.Start();
            int qty = newTransactions.Count;
            var transactions = new MassiveList<Transaction>(_connectionString, _tableName, _tablePkName);
            sw.Stop();
            this.LogOutput("Read", transactions.Count, sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            foreach(var newTransaction in newTransactions)
            {
                transactions.Add((Transaction)newTransaction);
            }
            sw.Stop();
            this.LogOutput("Wrote", newTransactions.Count, sw.ElapsedMilliseconds);
        }


        List<Transaction> getSkinnyTransactionSet(int qty)
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("LOAD SKINNY TRANSACTION SET");
            var sw = new Stopwatch();
            sw.Start();
            var transactions = new List<Transaction>();
            for (int i = 1; i <= qty; i++)
            {
                var newTrans = new Transaction()
                {
                    Amount = i,
                    Comment = "Transaction no. " + i.ToString(),
                    Identifier = "AA-" + i.ToString()
                };
                transactions.Add(newTrans);
            }
            sw.Stop();
            this.LogOutput("Loaded skinny records - qty:", qty, sw.ElapsedMilliseconds);
            return transactions;
        }


        List<Transaction> getFatTransactionSet(int qty)
        {
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("LOAD FAT TRANSACTION SET");
            var sw = new Stopwatch();
            sw.Start();
            string bigTextData = ""
                + " :Massive is a wrapper for your DB tables and uses System.Dynamic extensively. If you try to use this with C# "
                + "3.5 or below, it will explode and you will be sad. Me too honestly - I like how this doesn't require any DLLs other "
                + "than what's in the GAC. Yippee."
                + " :Massive is a wrapper for your DB tables and uses System.Dynamic extensively. If you try to use this with C# "
                + "3.5 or below, it will explode and you will be sad. Me too honestly - I like how this doesn't require any DLLs other "
                + "than what's in the GAC. Yippee."
                + " :Massive is a wrapper for your DB tables and uses System.Dynamic extensively. If you try to use this with C# "
                + "3.5 or below, it will explode and you will be sad. Me too honestly - I like how this doesn't require any DLLs other "
                + "than what's in the GAC. Yippee."
                + " :Massive is a wrapper for your DB tables and uses System.Dynamic extensively. If you try to use this with C# "
                + "3.5 or below, it will explode and you will be sad. Me too honestly - I like how this doesn't require any DLLs other "
                + "than what's in the GAC. Yippee."
                + " :Massive is a wrapper for your DB tables and uses System.Dynamic extensively. If you try to use this with C# "
                + "3.5 or below, it will explode and you will be sad. Me too honestly - I like how this doesn't require any DLLs other "
                + "than what's in the GAC. Yippee.";

            var transactions = new List<Transaction>();
            for (int i = 1; i <= qty; i++)
            {
                var newTrans = new Transaction()
                {
                    Amount = i,
                    Comment = "Transaction no. " + i.ToString()
                    + " : " + bigTextData,
                    Identifier = "AA-" + i.ToString()
                };
                transactions.Add(newTrans);
            }
            sw.Stop();

            this.LogOutput("Loaded fat records - qty:", qty, sw.ElapsedMilliseconds);
            return transactions;
        }
    }
}
