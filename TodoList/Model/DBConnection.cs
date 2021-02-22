using SQLite;
using System;
using System.IO;
using Windows.Storage;

namespace TodoList.Model
{
    class DBConnection
    {
        public static async System.Threading.Tasks.Task<SQLiteConnection> connectAsync()
        {
            string databaseFileName = await GetLocalDbFilePathAsync("TodosDB.db");
            var db = new SQLiteConnection(databaseFileName);
            return db;
        }

        public static async System.Threading.Tasks.Task<string> GetLocalDbFilePathAsync(string filename)
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path);
            return Path.Combine(path, filename);
        }
    }
}
