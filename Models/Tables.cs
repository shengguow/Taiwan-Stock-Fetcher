using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace OneMemberPlus.Models
{
    [Table("FUTURES_INFO")]
    public class FutureInfo
    {
        [Key]
        public string productid { get; set; }
        public string stockname { get; set; }
        public string stockno { get; set; }
        public string futureno { get; set; }
    }

    [Table("STOCK_DATA")]
    public class StockData
    {
        [Key]
        public string productid { get; set; }
        public string stockno { get; set; }
        public double o { get; set; }
        public double h { get; set; }
        public double l { get; set; }
        public double c { get; set; }
        public int vol { get; set; }
        public int stockdate { get; set; }
        public string stocktype { get; set; }
    }

    [Table("STOCK_VOL_WEIGHT")]
    public class StockVolWeight
    {
        [Key]
        public string stock_vol_weightid { get; set; }
        public string timename { get; set; }
        public double accumulation { get; set; }
        public DateTime createtime { get; set; }
    }

    [Table("TXTF_VOL_WEIGHT")]
    public class TXTFVolWeight
    {
        [Key]
        public string stock_vol_weightid { get; set; }
        public string timename { get; set; }
        public double accumulation { get; set; }
        public DateTime createtime { get; set; }
    }

    [Table("LASTUPDATE_DATA")]
    public class LastUpdateData
    {
        [Key]
        public string lastupdateid { get; set; }
        public string updatename { get; set; }
        public DateTime lastupdate { get; set; }

    }

    [Table("TWINDEX")]
    public class TWIndex
    {
        [Key]
        public string twindexid { get; set; }
        public double o { get; set; }
        public double h { get; set; }
        public double l { get; set; }
        public double c { get; set; }
        public double? amount { get; set; }
    }

    [Table("STOCK_INFO")]
    public class StockInfo
    {
        [Key]
        public string stock_id { get; set; }
        public string stock_name { get; set; }
        public int total_volumn { get; set; }

    }
}