using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockDataUpdater
{
    public class ClsPublic
    {
        public static string JsonRoot = System.Configuration.ConfigurationManager.AppSettings["jsonroot"].ToString();
    }
}
