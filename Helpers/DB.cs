using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Odbc;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

/// <summary>
/// Summary description for DB
/// </summary>
namespace EQArchitect
{
    public class DB
    {
        public static string MakeConnectionString()
        {
            return MakeConnectionString("", 0, "", "", "");
        }
        public static string MakeConnectionString(string Server, int Port, string Database, string Username, string Password)
        {
            bool _isDefault = false;
            
            System.Web.SessionState.HttpSessionState Session = HttpContext.Current.Session;
            
            if ((ToText(Server) == "") || (Port <= 0) || (ToText(Database) == "") || (ToText(Username) == "") || (ToText(Password) == ""))
            {
                Server = DefaultServer;
                Port = DefaultPort;
                Database = DefaultDatabase;
                Username = DefaultUsername;
                Password = DefaultPassword;

                _isDefault = true;
            }

            if (_isDefault)
            {
                Session["DBServerName"] = "(Demo)";
                Session["DBServerPort"] = "(Default)";
                Session["DBServerDatabase"] = "(Default)";
                Session["DBServerUsername"] = "(Default)";
                Session["DBServerPasswordBlank"] = "(Default)";
                Session["DBServerPassword"] = "";
                Session["DBServerHelp"] = "Click to connect to your own server";
            }
            else
            {
                Session["DBServerName"] = Server;
                Session["DBServerPort"] = Port.ToString();
                Session["DBServerDatabase"] = Database;
                Session["DBServerUsername"] = Username;
                Session["DBServerPasswordBlank"] = Password == "" ? "(Blank)" : "(Specified)";
                Session["DBServerPassword"] = Password;
                Session["DBServerHelp"] = string.Format("{0}:{1}/{2} ({3})", Server, Port, Database, Username);
            }

            Session["DBConnString"] = ConfigurationManager.ConnectionStrings["ODBCConnStr"].ConnectionString.
                            Replace("{Server}", Server).
                            Replace("{Port}", Port.ToString()).
                            Replace("{Database}", Database).
                            Replace("{Username}", Username).
                            Replace("{Password}", Password);

            return (string)Session["DBConnString"];
        }
        
        public static string DefaultServer = "localhost";
        public static int DefaultPort = 3306;
        public static string DefaultDatabase = "eqademo";
        public static string DefaultUsername = "eqademo";
        public static string DefaultPassword = "eqademo";

        public static bool IsDemo()
        {
            return (string)HttpContext.Current.Session["DBServerName"] == "(Demo)";
        }
        
        public static OdbcConnection Connect()
        {
            HttpContext.Current.Session["DBError"] = "";

            string _connString = (string)HttpContext.Current.Session["DBConnString"];

            if (_connString == null)
            {
                _connString = MakeConnectionString();
            }

            try
            {
                OdbcConnection _dbConn = new OdbcConnection(_connString);
                _dbConn.Open();

                HttpContext.Current.Session["DBConnError"] = "";

                return _dbConn;
            }
            catch (Exception ex)
            {
                HttpContext.Current.Session["DBConnError"] = ex.Message;
            }

            return null;
        }

        public static OdbcConnection ConnectTo(string Server, int Port, string Database, string Username, string Password)
        {
            MakeConnectionString(Server, Port, Database, Username, Password);

            return Connect();
        }

        public static bool ConnectionGood()
        {
            OdbcConnection _db = Connect();

            bool _OK = (_db != null);

            if (_OK)
            {
                _db.Close();
            }

            return _OK;
        }

        public static DataTableReader GetDataReader(string Query)
        {
            DataTable _data = GetData(Query);

            if (_data == null)
            {
                return null;
            }

            return new DataTableReader(_data);
        }

        public static OdbcDataReader OpenDataStream(string Query)
        {
            OdbcConnection _db = DB.Connect();

            if (_db != null)
            {
                OdbcCommand _cmd = new OdbcCommand();

                try
                {
                    _cmd.Connection = _db;
                    _cmd.CommandType = CommandType.Text;
                    _cmd.CommandText = Query;

                    return _cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }
                catch (Exception ex)
                {
                    HttpContext.Current.Session["DBError"] = ex.Message;
                }

                _cmd.Dispose();
                _db.Close();
            }

            return null;
        }

        public static DataTable GetData(string Query)
        {
            HttpContext.Current.Session["DBError"] = "";

            if (ToText(Query) == "")
            {
                return new DataTable();
            }

            using (OdbcConnection _db = Connect())
            {
                DataTable _dt = new DataTable();

                if (_db != null)
                {
                    using (OdbcCommand _cmd = new OdbcCommand())
                    {
                        try
                        {
                            _cmd.Connection = _db;
                            _cmd.CommandType = CommandType.Text;
                            _cmd.CommandText = Query;

                            OdbcDataAdapter _adap = new OdbcDataAdapter(_cmd);

                            _adap.Fill(_dt);
                        }
                        catch (Exception ex)
                        {
                            HttpContext.Current.Session["DBError"] = ex.Message;
                        }
                    }
                }

                return _dt;
            }
        }

        public static object GetDataValue(string Query)
        {
            HttpContext.Current.Session["DBError"] = "";

            if (ToText(Query) == "")
            {
                return null;
            }

            using (OdbcConnection _db = Connect())
            {
                if (_db != null)
                {
                    using (OdbcCommand _cmd = new OdbcCommand())
                    {
                        try
                        {
                            _cmd.Connection = _db;
                            _cmd.CommandType = CommandType.Text;
                            _cmd.CommandText = Query;

                            return _cmd.ExecuteScalar();
                        }
                        catch (Exception ex)
                        {
                            HttpContext.Current.Session["DBError"] = ex.Message;
                        }
                    }
                }
            }
            
            return null;
        }

        public static int Execute(string Query, List<OdbcParameter> Parms)
        {
            if (Parms == null)
            {
                return Execute(Query, (OdbcParameter[])null);
            }
            else
            {
                return Execute(Query, Parms.ToArray());
            }
        }
        public static int Execute(string Query, OdbcParameter[] Parms)
        {
            HttpContext.Current.Session["DBError"] = "";

            if (ToText(Query) == "")
            {
                return -1;
            }

            using (OdbcConnection _db = Connect())
            {
                if (_db != null)
                {
                    using (OdbcCommand _cmd = new OdbcCommand())
                    {
                        try
                        {
                            _cmd.Connection = _db;
                            _cmd.CommandType = CommandType.Text;
                            _cmd.CommandText = Query;

                            if (Parms != null)
                            {
                                _cmd.Parameters.AddRange(Parms);
                            }

                            return _cmd.ExecuteNonQuery();
                        }
                        catch (Exception _ex)
                        {
                            HttpContext.Current.Session["DBError"] = _ex.Message;
                        }
                    }
                }
            }

            return -1;
        }

        public static int GetInt(object Source, string FieldName)
        {
            DataRowView _row = null;
            
            try
            {
                if (Source is ListViewItemEventArgs)
                {
                    _row = (DataRowView)((ListViewDataItem)((ListViewItemEventArgs)Source).Item).DataItem;
                }
                else if (Source is FormView)
                {
                    _row = (DataRowView)((FormView)Source).DataItem;
                }

                return ToInt(_row[FieldName]);
            }
            catch
            {
                return 0;
            }
        }

        public static string GetText(Object Source, string FieldName)
        {
            DataRowView _row = null;

            try
            {
                if (Source is ListViewItemEventArgs)
                {
                    _row = (DataRowView)((ListViewDataItem)((ListViewItemEventArgs)Source).Item).DataItem;
                }
                else if (Source is FormView)
                {
                    _row = (DataRowView)((FormView)Source).DataItem;
                }

                return _row[FieldName].ToString();
            }
            catch
            {
                return "";
            }
        }

        public static int ToInt(object Value)
        {
            if (Value == null)
            {
                return 0;
            }
            
            if (Value is int)
            {
                return (int)Value;
            }

            if (Value is long)
            {
                return (int)(long)Value;
            }

            if (Value is uint)
            {
                return (int)(uint)Value;
            }

            if (Value is ulong)
            {
                return (int)(ulong)Value;
            }

            if (Value is short)
            {
                return (int)(short)Value;
            }

            if (Value is ushort)
            {
                return (int)(ushort)Value;
            }

            if (Value is float)
            {
                return (int)(float)Value;
            }

            if (Value is double)
            {
                return (int)(double)Value;
            }

            try
            {
                return int.Parse(Value.ToString());
            }
            catch
            {
                return 0;
            }
        }

        public static float ToFloat(object Value)
        {
            if (Value == null)
            {
                return 0.0f;
            }
            
            if (Value is float)
            {
                return (float)Value;
            }

            if (Value is long)
            {
                return (float)(long)Value;
            }

            if (Value is uint)
            {
                return (float)(uint)Value;
            }

            if (Value is ulong)
            {
                return (float)(ulong)Value;
            }

            if (Value is short)
            {
                return (float)(short)Value;
            }

            if (Value is ushort)
            {
                return (float)(ushort)Value;
            }

            if (Value is float)
            {
                return (float)Value;
            }

            if (Value is double)
            {
                return (float)(double)Value;
            }

            try
            {
                return float.Parse(Value.ToString());
            }
            catch
            {
                return 0;
            }
        }

        public static string ToText(object Value)
        {
            if (Value == null)
            {
                return "";
            }

            return Value.ToString().Trim();
        }

        public static int GetInt(string Query)
        {
            return ToInt(GetDataValue(Query));
        }
        
        public static string GetText(string Query)
        {
            return ToText(GetDataValue(Query));
        }

        public static bool HasField(Object Source, string FieldName)
        {
            DataRowView _row = null;

            try
            {
                if (Source is ListViewItemEventArgs)
                {
                    _row = (DataRowView)((ListViewDataItem)((ListViewItemEventArgs)Source).Item).DataItem;
                }
                else if (Source is FormView)
                {
                    _row = (DataRowView)((FormView)Source).DataItem;
                }

                return _row.DataView.Table.Columns.Contains(FieldName);
            }
            catch
            {
                return false;
            }
        }
    }
}
