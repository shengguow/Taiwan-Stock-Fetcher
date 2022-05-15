using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace OneMemberPlus.Models
{
    public class TableContext : DbContext
    {
        public DbSet<FutureInfo> lsFutureInfo
        {
            get;
            set;
        }

        public DbSet<StockData> lsStockData { get; set; }
        public DbSet<StockVolWeight> lsStockVolWeight { get; set; }
        public DbSet<TXTFVolWeight> lsTXTFVolWeight { get; set; }
        public DbSet<LastUpdateData> lsLastUpdateData { get; set; }

        public DbSet<TWIndex> lsTWIndex { get; set; }

        public TableContext() : base("DefaultConnection")
        {
            Database.SetInitializer<TableContext>(null);
        }
    }
}