using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessWatcher
{
    public class ApplicationContext : DbContext // это класс, который содержит набор таблиц
    {
        public DbSet<BannWord> BannWords { get; set; } = null!;
        public DbSet<BannApp> BannApps { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) // виртуальный метод выполнится автоматически
        {
            optionsBuilder.UseSqlite("Data Source=BaseMVVM_Word_App.db");
        }
    }

}
