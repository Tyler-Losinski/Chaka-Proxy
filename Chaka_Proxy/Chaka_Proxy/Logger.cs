using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chaka_Proxy
{
    class Logger
    {
        private  string sLogFormat;
        private  string sLogTime;

        public Logger() 
        {
            //sLogFormat used to create log files format :
            // dd/mm/yyyy hh:mm:ss AM/PM ==> Log Message
            sLogFormat = DateTime.Now.ToShortDateString().ToString() + " " + DateTime.Now.ToLongTimeString().ToString() + " ==> ";

            //this variable used to create log filename format "
            //for example filename : ErrorLogYYYYMMDD
            string sYear = DateTime.Now.Year.ToString();
            string sMonth = DateTime.Now.Month.ToString();
            string sDay = DateTime.Now.Day.ToString();
            sLogTime = sYear + "-" + sMonth + "-" + sDay;
            //Creates an ErrorLog folder if it doesn't exist
            FileInfo file = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "\\LogFiles\\");
            file.Directory.Create();
        }

        /// <summary>
        /// Logs an error
        /// </summary>
        /// <param name="className">This is the class the error occured in</param>
        /// <param name="methodName">This is the method the error occured in</param>
        /// <param name="sErrMsg">This is the error message</param>
        public void LogRequest(string browserIP, string URL, string size)
        {

            try
            {
                StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogFiles\\" + "proxy " + sLogTime + ".log" +".txt", true);

                sw.WriteLine(sLogFormat + " " + browserIP + " " + URL + " " + size);
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error logging a log: " + ex.Message);
            }
        }
    }
}
