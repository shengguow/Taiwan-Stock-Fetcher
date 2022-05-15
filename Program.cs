using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using OneMemberPlus.Models;
using sttac.dal;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StockDataUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            DALFactory.addConnection<SqlConnection>("traderfarm", ConfigurationManager.AppSettings["traderfarm"].ToString());

            #region 更新加權指數
            using (TableContext tc = new TableContext())
            {
                var index = tc.lsTWIndex.OrderByDescending(s => s.twindexid).FirstOrDefault();
                var id = Convert.ToDateTime(index == null ? "2008/01/01" : index.twindexid);
                using (WebClient client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                    SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " +
                                      "Windows NT 5.2; .NET CLR 1.0.3705;)");
                    id = id.AddDays(1);
                    for (var i = id; i <= DateTime.Now; i = i.AddMonths(1))
                    {
                        var url = "https://www.twse.com.tw/indicesReport/MI_5MINS_HIST?response=json&date=" + i.ToString("yyyyMMdd");
                        var json = new JObject();
                        var jsonfile = Path.Combine(ClsPublic.JsonRoot, i.ToString("yyyyMMdd") + "_marketprice.json");
                        if (File.Exists(jsonfile))
                        {
                            json = JObject.Parse(File.ReadAllText(jsonfile));
                        }
                        else
                        {
                            json = JObject.Parse(Encoding.UTF8.GetString(client.DownloadData(url)));
                        }
                            
                        if (json["fields"] != null)
                        {
                            var fields = json["fields"].ToArray().ToList();
                            // "日期","開盤指數","最高指數","最低指數","收盤指數"
                            var d = GetIndex(fields, "日期");
                            var o = GetIndex(fields, "開盤指數");
                            var h = GetIndex(fields, "最高指數");
                            var l = GetIndex(fields, "最低指數");
                            var c = GetIndex(fields, "收盤指數");
                            var data = json["data"].ToArray().ToList();
                            for (var j = 0; j < data.Count; j++)
                            {
                                Console.WriteLine("Updating ");
                                try
                                {
                                    // 日期
                                    var dd = new DateTime(Convert.ToInt32(data[j][d].ToString().Split('/')[0]) + 1911, Convert.ToInt32(data[j][d].ToString().Split('/')[1]), Convert.ToInt32(data[j][d].ToString().Split('/')[2]));
                                    Console.WriteLine("Updating " + dd.ToString("yyyy/MM/dd"));
                                    tc.Database.ExecuteSqlCommand("MERGE into [TWINDEX] as target using (select '" + dd.ToString("yyyy/MM/dd") + "' as twindexid," + data[j][o].ToString().Replace(",", "") + " as o," + data[j][h].ToString().Replace(",", "") + " as h," + data[j][l].ToString().Replace(",", "") + " as l," + data[j][c].ToString().Replace(",", "") + " as c) AS source ON(target.twindexid = source.twindexid) WHEN NOT MATCHED THEN INSERT (twindexid, o,h,l,c) VALUES(source.twindexid,source.o,source.h,source.l,source.c);");
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }
                        
                        Thread.Sleep(1000);
                    }
                }
            }
            #endregion

            #region 更新加權指數成交金額、股數等資訊
            using (TableContext tc = new TableContext())
            {
                var index = tc.lsTWIndex.Where(s => s.amount.HasValue).OrderByDescending(s => s.twindexid).FirstOrDefault();
                var id = Convert.ToDateTime(index == null ? "2008/01/01" : index.twindexid);
                using (WebClient client = new WebClient())
                {
                    ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                    SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                    client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " +
                                      "Windows NT 5.2; .NET CLR 1.0.3705;)");
                    id = id.AddDays(1);
                    for (var i = id; i <= DateTime.Now; i = i.AddMonths(1))
                    {
                        var url = "https://www.twse.com.tw/exchangeReport/FMTQIK?response=json&date=" + i.ToString("yyyyMMdd");
                        var json = new JObject();
                        var jsonfile = Path.Combine(ClsPublic.JsonRoot, i.ToString("yyyyMMdd") + "_marketvol.json");
                        if (File.Exists(jsonfile))
                        {
                            json = JObject.Parse(File.ReadAllText(jsonfile));
                        }
                        else
                        {
                            var contents = Encoding.UTF8.GetString(client.DownloadData(url));
                            json = JObject.Parse(contents);
                            File.WriteAllText(jsonfile, contents);
                        }

                        if (json["fields"] != null)
                        {
                            var fields = json["fields"].ToArray().ToList();
                            // "日期","開盤指數","最高指數","最低指數","收盤指數"
                            var d = GetIndex(fields, "日期");
                            var stocks = GetIndex(fields, "成交股數");
                            var amount = GetIndex(fields, "成交金額");
                            var trades = GetIndex(fields, "成交筆數");
                            var increment = GetIndex(fields, "漲跌點數");
                            var data = json["data"].ToArray().ToList();
                            for (var j = 0; j < data.Count; j++)
                            {
                                Console.WriteLine("Updating ");
                                try
                                {
                                    // 日期
                                    var dd = new DateTime(Convert.ToInt32(data[j][d].ToString().Split('/')[0]) + 1911, Convert.ToInt32(data[j][d].ToString().Split('/')[1]), Convert.ToInt32(data[j][d].ToString().Split('/')[2]));
                                    Console.WriteLine("Updating " + dd.ToString("yyyy/MM/dd"));
                                    tc.Database.ExecuteSqlCommand("update [TWINDEX] set stocks=" + data[j][stocks].ToString().Replace(",", "") + ", amount=" + data[j][amount].ToString().Replace(",", "") + ",trades=" + data[j][trades].ToString().Replace(",", "") + ",increment=" + data[j][increment].ToString().Replace(",", "") + " where twindexid='" + dd.ToString("yyyy/MM/dd") + "'");
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }

                        Thread.Sleep(1000);
                    }
                }
            }

            #endregion

            #region 撈取股票資訊
            using (TableContext tc = new TableContext())
            {
                var jsonfile = "http://www.twse.com.tw/exchangeReport/MI_INDEX?response=json&date={DATE}&type=ALLBUT0999";
                var stockud = tc.lsLastUpdateData.FirstOrDefault(s => s.updatename == "STOCKINFO");
                var lastupdate = stockud!=null?stockud.lastupdate:new DateTime(2012,01,01);
                if (lastupdate.ToString("yyyyMMdd")!=DateTime.Now.ToString("yyyyMMdd"))
                {
                    for (var idate = lastupdate; DateTime.Now.Subtract(idate).TotalDays >= 0; idate = idate.AddDays(1))
                    {
                        using (WebClient client = new WebClient())
                        {
                            ServicePointManager.SecurityProtocol =
                            SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                            SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                            client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " +
                                      "Windows NT 5.2; .NET CLR 1.0.3705;)");
                            //client.Proxy = GetAvaiableProxy();
                            var tradedate = idate.ToString("yyyyMMdd");
                            var filename = Path.Combine(ClsPublic.JsonRoot, tradedate + ".json");
                            if (!File.Exists(filename))
                            {
                                var jsonurl = jsonfile.Replace("{DATE}", tradedate);
                                try
                                {
                                    var jsonresult = Encoding.UTF8.GetString(client.DownloadData(jsonurl));
                                    var json = JObject.Parse(jsonresult);
                                    if (json["stat"].ToString() == "OK")
                                    {
                                        File.WriteAllText(filename, jsonresult);
                                        Console.WriteLine(tradedate + ".json OK");
                                    }
                                    else
                                    {
                                        Console.WriteLine("stat=" + json["stat"].ToString() + "---" + tradedate + " no trade OK");
                                    }
                                    Thread.Sleep(5000);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message + "---" + tradedate + " no trade OK");
                                }
                            }
                            else
                            {
                                Console.WriteLine(tradedate + " trade OK");

                                stockud.lastupdate = idate;
                                tc.SaveChanges();
                            }

                        }

                        
                    }
                }                
            }
            #endregion

            #region 撈取上櫃股票資訊
            using (TableContext tc = new TableContext())
            {
                var jsonfile = "http://www.tpex.org.tw/web/stock/aftertrading/daily_close_quotes/stk_quote_result.php?l=zh-tw&o=json&d={DATE}&s=0,asc,0";
                var stockud = tc.lsLastUpdateData.FirstOrDefault(s => s.updatename == "DESKINFO");

                var lastupdate = stockud==null?new DateTime(2012,1,1):stockud.lastupdate;
                if (lastupdate.ToString("yyyyMMdd") !=DateTime.Now.ToString("yyyyMMdd"))
                {
                    for (var idate = lastupdate; DateTime.Now.Subtract(idate).TotalDays >= 0; idate = idate.AddDays(1))
                    {
                        using (WebClient client = new WebClient())
                        {
                            ServicePointManager.SecurityProtocol =
                            SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                            SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                            //client.Proxy = GetAvaiableProxy();
                            var tradedate = (idate.Year - 1911).ToString() + "/" + idate.ToString("MM/dd");
                            var filename = Path.Combine(ClsPublic.JsonRoot, idate.ToString("yyyyMMdd") + "_overdesk.json");
                            if (!File.Exists(filename))
                            {
                                var jsonurl = jsonfile.Replace("{DATE}", tradedate);
                                var jsonresult = Encoding.UTF8.GetString(client.DownloadData(jsonurl));
                                try
                                {
                                    var json = JObject.Parse(jsonresult);
                                    if (json["aaData"].ToArray().Length > 0)
                                    {
                                        File.WriteAllText(filename, jsonresult);
                                        Console.WriteLine(tradedate + ".json OK");
                                    }
                                    else
                                    {
                                        if (json["stat"] != null)
                                        {
                                            Console.WriteLine("stat=" + json["stat"].ToString() + "---" + tradedate + " no trade OK");
                                        }
                                        
                                    }

                                    Thread.Sleep(5000);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message + "---" + tradedate + " no trade OK");
                                }
                            }
                            else
                            {
                                Console.WriteLine(tradedate + " trade OK");
                                stockud.lastupdate = idate;
                                tc.SaveChanges();
                            }

                        }

                        
                    }
                }                
            }
            #endregion

            #region 更新股票股價
            // 以 Json 資料來初始化
            using (TableContext tc = new TableContext())
            {
                var sd = tc.lsStockData.Where(s => s.stocktype == "STOCK").OrderByDescending(s => s.stockdate).FirstOrDefault();
                // 更新資料
                var lastupdate = sd == null ? new DateTime(2008,1,1) : new DateTime(Convert.ToInt32(sd.stockdate.ToString().Substring(0, 4)), Convert.ToInt32(sd.stockdate.ToString().Substring(4, 2)), Convert.ToInt32(sd.stockdate.ToString().Substring(6, 2)));
                for (var idate = lastupdate; DateTime.Now.Subtract(idate).TotalDays >= 0; idate = idate.AddDays(1))
                {
                    var tradedate = idate.ToString("yyyyMMdd");
                    var filename = Path.Combine(ClsPublic.JsonRoot, tradedate + ".json");
                    FileInfo fi = new FileInfo(filename);
                    if (fi.Exists)
                    {
                        var json = JObject.Parse(File.ReadAllText(filename));
                        if (json["stat"].ToString() == "OK")
                        {
                            // "證券代號","證券名稱","成交股數","成交筆數","成交金額","開盤價","最高價","最低價","收盤價","漲跌(+/-)","漲跌價差","最後揭示買價","最後揭示買量","最後揭示賣價","最後揭示賣量","本益比"
                            var data2 = GetStockArray(json);
                            tradedate = fi.Name.Replace(fi.Extension, "");
                            for (var j = 0; j < data2.Count; j++)
                            {
                                var stockid = data2[j][0].ToString().Trim();
                                var vol = Convert.ToInt32(data2[j][2].ToString().Replace(",", "")) / 1000;
                                var amount = Convert.ToDouble(data2[j][4].ToString().Replace(",", "")) / 100000000.0;
                                var open = data2[j][5].ToString() == "--" ? -1 : Convert.ToDouble(data2[j][5].ToString());
                                var high = data2[j][5].ToString() == "--" ? -1 : Convert.ToDouble(data2[j][6].ToString());
                                var low = data2[j][5].ToString() == "--" ? -1 : Convert.ToDouble(data2[j][7].ToString());
                                var close = data2[j][5].ToString() == "--" ? -1 : Convert.ToDouble(data2[j][8].ToString());
                                //var increment = data2[j][10].ToString() == "--" ? 0 : Convert.ToDouble(data2[j][10].ToString());
                                var PERatio = Convert.ToDouble(data2[j][15].ToString());
                                //var ratio = Math.Round((increment) / (close - increment) * 100, 2) * (data2[j][9].ToString().Contains("+") ? 1 : -1);

                                try
                                {
                                    //tc.Database.ExecuteSqlCommand("MERGE STOCK_DATA AS target USING(SELECT '" + stockid + tradedate + "' as productid, '" + stockid + "' as stockno, " + open + " as o, " + high + " as h, " + low + " as l, " + close + " as c, '" + tradedate + "' as stockdate,'STOCK' as stocktype,'" + vol + "' as vol) AS s ON(target.stockdate = s.stockdate and target.stockno = s.stockno) WHEN NOT MATCHED BY TARGET THEN INSERT(productid, stockno, o, h, l, c, stockdate,stocktype,vol) VALUES (s.productid, s.stockno, s.o, s.h, s.l, s.c, s.stockdate,s.stocktype,s.vol);");
                                    tc.Database.ExecuteSqlCommand("INSERT INTO STOCK_DATA (productid, stockno, o, h, l, c, stockdate,stocktype,vol) VALUES ('" + stockid + tradedate + "', '" + stockid + "', " + open + ", " + high + ", " + low + ", " + close + ", '" + tradedate + "','STOCK','" + vol + "');");
                                    Console.WriteLine(stockid + "-" + tradedate + " OK");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(stockid + "-" + tradedate + " already updated");
                                }
                                //tc.lsPrice.Add(new Price() { priceid = tradedate + stockid, stockid = stockid, tradedate = tradedate, turnover = turnover, vol = vol, o = open, l = low, h = high, c = close, peratio = PERatio });
                            }

                            //tc.SaveChanges();
                        }
                    }
                }

            }
            #endregion

            #region 更新上櫃股票價格資訊
            // 以 Json 資料來初始化
            using (TableContext tc = new TableContext())
            {
                {
                    var sd = tc.lsStockData.Where(s => s.stocktype == "DESKTOP").OrderByDescending(s => s.stockdate).FirstOrDefault();
                    // 更新資料
                    var lastupdate = sd==null? new DateTime(2008, 1, 1) : new DateTime(Convert.ToInt32(sd.stockdate.ToString().Substring(0, 4)), Convert.ToInt32(sd.stockdate.ToString().Substring(4, 2)), Convert.ToInt32(sd.stockdate.ToString().Substring(6, 2)));
                    for (var idate = lastupdate; idate <= DateTime.Now; idate = idate.AddDays(1))
                    {
                        var tradedate = idate.ToString("yyyyMMdd");
                        var filename = Path.Combine(ClsPublic.JsonRoot, tradedate + "_overdesk.json");
                        FileInfo fi = new FileInfo(filename);
                        if (fi.Exists)
                        {
                            var json = JObject.Parse(File.ReadAllText(filename));
                            var data = json["aaData"].ToArray().ToList();
                            if (data.Count > 0)
                            {
                                // 0代號	1名稱	2收盤 	3漲跌	4開盤 	5最高 	6最低	7均價 	8成交股數  	9成交金額(元)	10成交筆數 	11最後買價	12最後賣價	13發行股數 	14次日參考價 	 15次日漲停價 	16次日跌停價

                                //var data2 = GetStockArray(json);
                                //tradedate = fi.Name.Replace(fi.Extension, "");
                                for (var j = 0; j < data.Count; j++)
                                {
                                    var stockid = data[j][0].ToString().Trim();
                                    var stockname = data[j][1].ToString().Trim();
                                    var vol = Convert.ToInt32(data[j][8].ToString().Replace(",", "")) / 1000;
                                    var amount = Convert.ToDouble(data[j][9].ToString().Replace(",", "")) / 100000000.0;
                                    var open = data[j][4].ToString().Replace("-", "").Replace(" ", "") == "" ? -1 : Convert.ToDouble(data[j][4].ToString());
                                    var high = data[j][5].ToString().Replace("-", "").Replace(" ", "") == "" ? -1 : Convert.ToDouble(data[j][5].ToString());
                                    var low = data[j][6].ToString().Replace("-", "").Replace(" ", "") == "" ? -1 : Convert.ToDouble(data[j][6].ToString());
                                    var close = data[j][2].ToString().Replace("-", "").Replace(" ", "") == "" ? -1 : Convert.ToDouble(data[j][2].ToString());
                                    //var increment = data[j][3].ToString().Replace("-", "").Replace(" ", "") == "" ? 0 : (data[j][3].ToString() == "除權息 " || data[j][3].ToString() == "除權 " || data[j][3].ToString() == "除息 " ? -1 : Convert.ToDouble(data[j][3].ToString()));
                                    //var ratio = Math.Round((increment) / (close - increment) * 100, 2) * (data[j][3].ToString().Contains("+") ? 1 : -1);

                                    // 剔除非權證類的股票
                                    if (!(stockid.Length > 4 && (stockname.Contains("購") || stockname.Contains("售"))))
                                    {
                                        try
                                        {
                                            //tc.Database.ExecuteSqlCommand("MERGE STOCK_DATA AS target USING(SELECT '"+stockid + tradedate + "' as productid, '"+stockid+"' as stockno, "+ open + " as o, " + high + " as h, " + low + " as l, " + close + " as c, '"+tradedate+ "' as stockdate,'DESKTOP' as stocktype,'"+ vol + "' as vol) AS s ON(target.stockdate = s.stockdate and target.stockno = s.stockno) WHEN NOT MATCHED BY TARGET THEN INSERT(productid, stockno, o, h, l, c, stockdate,stocktype,vol) VALUES (s.productid, s.stockno, s.o, s.h, s.l, s.c, s.stockdate,s.stocktype,s.vol);");
                                            tc.Database.ExecuteSqlCommand("INSERT INTO STOCK_DATA (productid, stockno, o, h, l, c, stockdate,stocktype,vol) VALUES ('" + stockid + tradedate + "', '" + stockid + "', " + open + ", " + high + ", " + low + ", " + close + ", '" + tradedate + "','DESKTOP','" + vol + "');");
                                            Console.WriteLine(stockid + "-" + tradedate + " OK");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(stockid + "-" + tradedate + " ERROR");
                                        }
                                    }
                                    //tc.lsPrice.Add(new Price() { priceid = tradedate + stockid, stockid = stockid, tradedate = tradedate, turnover = turnover, vol = vol, o = open, l = low, h = high, c = close, peratio = PERatio });
                                }
                            }
                        }

                        Console.WriteLine(idate.ToString("yyyy/MM/dd"));
                    }
                }
            }
            #endregion

            #region 更新股票名稱
            {
                using (var tc = new TableContext())
                {
                    // 更新上市股票名稱
                    var filename = Path.Combine(ClsPublic.JsonRoot, DateTime.Now.ToString("yyyyMMdd") + ".json");
                    if (File.Exists(filename))
                    {
                        var json = JObject.Parse(File.ReadAllText(filename));
                        if (json["stat"].ToString() == "OK")
                        {
                            // "證券代號","證券名稱","成交股數","成交筆數","成交金額","開盤價","最高價","最低價","收盤價","漲跌(+/-)","漲跌價差","最後揭示買價","最後揭示買量","最後揭示賣價","最後揭示賣量","本益比"
                            var data2 = GetStockArray(json);
                            for (var j = 0; j < data2.Count; j++)
                            {
                                var stockid = data2[j][0].ToString().Trim();
                                var stockname = data2[j][1].ToString().Trim();

                                if (!(stockid.Length > 4 && (stockname.Contains("購") || stockname.Contains("售"))))
                                {
                                    try
                                    {

                                        tc.Database.ExecuteSqlCommand("insert into STOCK_INFO (stock_id,stock_name) values ('" + stockid + "','" + stockname + "')");
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                    Console.WriteLine(stockname + " 更新完成");
                                }
                            }
                        }

                        filename = Path.Combine(ClsPublic.JsonRoot, DateTime.Now.ToString("yyyyMMdd") + "_overdesk.json");
                        FileInfo fi = new FileInfo(filename);
                        if (fi.Exists)
                        {
                            json = JObject.Parse(File.ReadAllText(filename));
                            var data = json["aaData"].ToArray().ToList();
                            if (data.Count > 0)
                            {
                                // 0代號	1名稱	2收盤 	3漲跌	4開盤 	5最高 	6最低	7均價 	8成交股數  	9成交金額(元)	10成交筆數 	11最後買價	12最後賣價	13發行股數 	14次日參考價 	 15次日漲停價 	16次日跌停價

                                //var data2 = GetStockArray(json);
                                //tradedate = fi.Name.Replace(fi.Extension, "");
                                for (var j = 0; j < data.Count; j++)
                                {
                                    var stockid = data[j][0].ToString().Trim();
                                    var stockname = data[j][1].ToString().Trim();

                                    if (!(stockid.Length > 4 && (stockname.Contains("購") || stockname.Contains("售"))))
                                    {
                                        try
                                        {

                                            tc.Database.ExecuteSqlCommand("insert into STOCK_INFO (stock_id,stock_name) values ('" + stockid + "','" + stockname + "')");
                                        }
                                        catch (Exception ex)
                                        {

                                        }
                                        Console.WriteLine(stockname + " 更新完成");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region 更新在外流通股票數
            {
                using (var Conn = DALFactory.getConnection("traderfarm"))
                {
                    var stocks = DALFactory.Query<StockInfo>(Conn, "select * from STOCK_INFO where update_volumn=1").ToList();
                    using (var client = new WebClient())
                    {
                        for (var i = 0; i < stocks.Count; i++)
                        {
                            try
                            {
                                var url = "https://tw.stock.yahoo.com/quote/" + stocks[i].stock_id + "/profile";
                                var html = Encoding.UTF8.GetString(client.DownloadData(url));
                                var doc = new HtmlDocument();
                                doc.LoadHtml(html);
                                var node = doc.DocumentNode.Descendants().Where(s => s.InnerText == "已發行普通股數").FirstOrDefault();
                                if (node == null)
                                {
                                    continue;
                                }
                                var text = node.ParentNode.ChildNodes[1].InnerText;
                                if (text != "-")
                                {
                                    DALFactory.Execute(Conn, "update STOCK_INFO set total_volumn=" + text.Replace(",", "") + " where stock_id='" + stocks[i].stock_id + "'");
                                    Console.WriteLine(stocks[i].stock_name + " 已更新(" + (i + 1) + "/" + (stocks.Count) + ")");
                                }
                                else
                                {
                                    Console.WriteLine(stocks[i].stock_name + " 無資訊");
                                }
                            }
                            catch (Exception ex)
                            {

                            }
                        }
                    }

                }

            }

            #endregion
        }

        public static List<JToken> GetStockArray(JObject json)
        {
            for (var i = 1; i <= 20; i++)
            {
                if (json["fields" + (i.ToString())]!=null)
                {
                    if (json["fields" + (i.ToString())][0].ToString() == "證券代號")
                    {
                        return json["data" + (i.ToString())].ToArray().ToList();
                    }
                }
                
            }

            throw new Exception("找不到 股票 DataSet");
        }

        public static int GetIndex(List<JToken> lsFields,string FieldName)
        {
            for (var i=0;i<lsFields.Count;i++)
            {
                if (lsFields[i].ToString() == FieldName)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
