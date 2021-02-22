using SQLite;
using System;

namespace TodoList.Model
{
    [Table("Todos")]
    class ToDo
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime BeginningDate { get; set; }
        public DateTime EndingDate { get; set; }
        public bool Done { get; set; }
    }
}
