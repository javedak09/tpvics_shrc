﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Configuration;

namespace tpvics_shrc
{
    public partial class frmLogin : Form
    {
        public frmLogin()
        {
            InitializeComponent();
        }


        private void setSqliteVersion()
        {
            var new_version = ConfigurationSettings.AppSettings["dbver"];

            string stm = "PRAGMA user_version = " + new_version;

            CConnection cn = new CConnection();

            cn.MConnOpen();

            SQLiteCommand cmd = new SQLiteCommand(stm, cn.cn);
            SQLiteDataReader dr = cmd.ExecuteReader();

            cn.MConnClose();
        }


        private Int64 getSqliteVersion()
        {
            string stm = "PRAGMA user_version";

            CConnection cn = new CConnection();

            cn.MConnOpen();

            SQLiteCommand cmd = new SQLiteCommand(stm, cn.cn);

            SQLiteDataReader dr = cmd.ExecuteReader();

            if (dr.Read())
            {
                //Console.WriteLine($"SQLite version: {dr.GetInt64(0)}");

                return dr.GetInt64(0);
            }

            cn.MConnClose();

            return -1;
        }



        private void alterTableQuery(string qry)
        {
            CConnection cn = new CConnection();

            cn.MConnOpen();

            SQLiteCommand cmd = new SQLiteCommand(qry, cn.cn);
            SQLiteDataReader dr = cmd.ExecuteReader();

            cn.MConnClose();
        }



        private void dbupgrade()
        {
            var curr_version = getSqliteVersion();

            var new_version = ConfigurationSettings.AppSettings["dbver"];


            if (curr_version != -1 && curr_version > Convert.ToInt64(new_version))
            {
                switch (curr_version)
                {
                    case 1:
                        var qry = "ALTER TABLE camp_patient_dtl add column mh01704 text";
                        alterTableQuery(qry);

                        setSqliteVersion();
                        goto default;

                    default:
                        break;

                }

            }
        }


        private void frmLogin_Load(object sender, EventArgs e)
        {
            this.Text = CVariables.AppName1;

            //setSqliteVersion();

            //dbupgrade();

            //checkColumn("mh010a");

            getDistrict_Hardcoded();

            //getDistrict_FromSqliteDB();
        }



        private bool checkColumn(string colName)
        {
            bool iserror = false;

            try
            {
                CConnection cn = new CConnection();

                SQLiteDataAdapter da = new SQLiteDataAdapter("select " + colName + " from camp_patient_dtl ", cn.cn);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables[0].Columns[colName].ToString() == "")
                {
                    //alterTableQuery_new(colName);
                }

                iserror = true;
            }

            catch (Exception ex)
            {
                alterTableQuery_new(colName);
                iserror = false;
            }

            return iserror;
        }





        private void alterTableQuery_new(string colName)
        {
            CConnection cn = new CConnection();

            cn.MConnOpen();

            SQLiteCommand cmd = new SQLiteCommand("ALTER TABLE camp_patient_dtl add column " + colName + " TEXT ", cn.cn);
            SQLiteDataReader dr = cmd.ExecuteReader();

            cn.MConnClose();
        }


        private void frmLogin_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                SendKeys.Send("{TAB}");
            }
        }


        private bool checkDoctorsExists()
        {
            bool isexists = false;

            try
            {
                CConnection cn = new CConnection();

                SQLiteDataAdapter da = new SQLiteDataAdapter("select * from camp_doctors ", cn.cn);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        isexists = true;
                    }
                }

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return isexists;
        }



        private void getDistrict_FromSqliteDB()
        {
            try
            {
                CConnection cn = new CConnection();

                SQLiteDataAdapter da = new SQLiteDataAdapter("select * from districts ", cn.cn);
                DataSet ds = new DataSet();
                da.Fill(ds);

                Dictionary<string, string> comboSource = new Dictionary<string, string>();
                comboSource.Add("0", "Select District");

                for (int a = 0; a <= ds.Tables[0].Rows.Count - 1; a++)
                {
                    comboSource.Add(ds.Tables[0].Rows[a]["dist_id"].ToString(), ds.Tables[0].Rows[a]["district"].ToString());
                }

                ddlDistrict.DataSource = new BindingSource(comboSource, null);
                ddlDistrict.DisplayMember = "Value";
                ddlDistrict.ValueMember = "Key";

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }



        private void getDistrict_Hardcoded()
        {
            try
            {
                Dictionary<string, string> comboSource = new Dictionary<string, string>();
                comboSource.Add("Select District", "0");
                comboSource.Add("Peshawar", "124");
                comboSource.Add("District Korangi", "335");
                comboSource.Add("Karachi East", "336");
                comboSource.Add("Karachi West", "337");
                comboSource.Add("Killa Abdullah", "411");
                comboSource.Add("Pishin", "412");
                comboSource.Add("Quetta", "413");
                comboSource.Add("Lahore", "920");
                comboSource.Add("Rawalpindi", "921");
                comboSource.Add("Training Purpose", "925");


                ddlDistrict.DataSource = new BindingSource(comboSource, null);
                ddlDistrict.DisplayMember = "Key";
                ddlDistrict.ValueMember = "Value";

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }



        private void downloadDistricts()
        {

            try
            {

                //var request = (HttpWebRequest)WebRequest.CreateHttp("https://vcoe1.aku.edu/naunehal/api/getdata.php");
                var request = (HttpWebRequest)WebRequest.CreateHttp(CVariables.getServerURL + CVariables.getDataFileName);


                //request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)";


                int winBuild = Environment.OSVersion.Version.Build;
                String userAgent = "NET/5.0 (Windows; Build/" + winBuild + ")";


                request.UserAgent = userAgent;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = new JavaScriptSerializer().Serialize(new
                    {
                        table = "districts",
                        check = "1",
                        filter = " dist_id='" + ddlDistrict.SelectedValue.ToString() + "'"
                    });

                    streamWriter.Write(json);
                }


                var response = (HttpWebResponse)request.GetResponse();


                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                var obj = JsonConvert.DeserializeObject<List<Districts>>(responseString);



                CConnection cn = new CConnection();

                SQLiteDataAdapter da = null;
                DataSet ds = null;

                da = new SQLiteDataAdapter("delete from districts", cn.cn);
                ds = new DataSet();
                da.Fill(ds);


                string qry = "";


                for (int a = 0; a <= obj.Count - 1; a++)
                {

                    qry = "insert into districts (dist_id, district, province) values('" + obj[a].dist_id + "', '" + obj[a].district + "', '" + obj[a].province + "')";

                    da = new SQLiteDataAdapter(qry, cn.cn);

                    ds = new DataSet();
                    da.Fill(ds);

                }

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtUserID.Focus();
            }

            finally
            {

            }
        }


        private void downloadUsers()
        {
            try
            {

                //var request = (HttpWebRequest)WebRequest.CreateHttp("https://vcoe1.aku.edu/naunehal/api/getdata.php");
                var request = (HttpWebRequest)WebRequest.CreateHttp(CVariables.getServerURL + CVariables.getDataFileName);

                //request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)";



                int winBuild = Environment.OSVersion.Version.Build;
                String userAgent = "NET/5.0 (Windows; Build/" + winBuild + ")";


                request.UserAgent = userAgent;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = new JavaScriptSerializer().Serialize(new
                    {
                        table = "users",
                        check = "1"
                    });

                    streamWriter.Write(json);
                }


                var response = (HttpWebResponse)request.GetResponse();


                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                var obj = JsonConvert.DeserializeObject<List<Users>>(responseString);



                CConnection cn = new CConnection();

                SQLiteDataAdapter da = null;
                DataSet ds = null;

                da = new SQLiteDataAdapter("delete from users", cn.cn);
                ds = new DataSet();
                da.Fill(ds);


                string qry = "";


                for (int a = 0; a <= obj.Count - 1; a++)
                {


                    qry = "insert into users (id, username, password, dist_id) values('" + obj[a].id + "', '" + obj[a].username + "', '" + obj[a].password + "', '" + obj[a].dist_id + "')";

                    da = new SQLiteDataAdapter(qry, cn.cn);

                    ds = new DataSet();
                    da.Fill(ds);

                }


                //MessageBox.Show("Users downloaded", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtUserID.Focus();
            }

            finally
            {

            }
        }



        private void downloadCamps()
        {
            try
            {
                //var request = (HttpWebRequest)WebRequest.CreateHttp("https://vcoe1.aku.edu/naunehal/api/getData.php");
                var request = (HttpWebRequest)WebRequest.CreateHttp(CVariables.getServerURL + CVariables.getDataFileName);

                string dist_id = "";


                //string param_json = "{\"table\":\"camp\", \"check\":\"1\", \"locked\":\"0\", \"status\":\"e\", \"cdate\":\"03-04-2021\" }";


                //var postData = "table=" + Uri.EscapeDataString("camp");
                //postData += "&check=" + Uri.EscapeDataString("1");
                //postData += "&locked=" + Uri.EscapeDataString("0");
                //postData += "&status=" + Uri.EscapeDataString("e");
                //postData += "&cdate=" + Uri.EscapeDataString("03-04-2021");



                //var data = Encoding.ASCII.GetBytes(postData);



                int winBuild = Environment.OSVersion.Version.Build;
                String userAgent = "NET/5.0 (Windows; Build/" + winBuild + ")";


                //request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)";


                request.UserAgent = userAgent;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                //request.ContentLength = data.Length;

                //using (var stream = request.GetRequestStream())
                //{
                //    stream.Write(data, 0, data.Length);
                //}




                //using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                //{
                //    string json = new JavaScriptSerializer().Serialize(new
                //    {
                //        table = "camps",
                //        filter = " camp_status = 'Conducted' AND execution_date <= '2021-04-13' AND dist_id='" + ddlDistrict.SelectedValue.ToString() + "'"

                //    });

                //    streamWriter.Write(json);
                //}



                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = new JavaScriptSerializer().Serialize(new
                    {
                        table = "camps",
                        check = "1",
                        filter = " camp_status = 'Conducted' AND execution_date <= '" + DateTime.Now.ToString("yyyy-MM-dd") + "' AND dist_id='" + ddlDistrict.SelectedValue.ToString() + "' AND locked=0 "

                    });

                    streamWriter.Write(json);
                }


                var response = (HttpWebResponse)request.GetResponse();


                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();




                var obj = JsonConvert.DeserializeObject<List<Camp_Patient>>(responseString);


                CConnection cn = new CConnection();

                SQLiteDataAdapter da = null;
                DataSet ds = null;


                da = new SQLiteDataAdapter("delete from campdatadown", cn.cn);
                ds = new DataSet();
                da.Fill(ds);


                string qry = "";


                for (int a = 0; a <= obj.Count - 1; a++)
                {

                    qry = "insert into campdatadown (idCamp, dist_id, district, ucCode, ucName, area_no, area_name, plan_date, camp_no, camp_status," +
                        " remarks, execution_date, " +
                        "execution_duration, colflag, locked, lockedBy, idDoctor, doctor_name) " +
                        "values('" +
                        obj[a].idCamp + "', '" +
                        obj[a].dist_id + "', '" +
                        obj[a].district + "', '" +
                        obj[a].ucCode + "', '" +
                        obj[a].ucName + "', '" +
                        obj[a].area_no + "', '" +
                        obj[a].area_name + "', '" +
                        obj[a].plan_date + "', '" +
                        obj[a].camp_no + "', '" +
                        obj[a].camp_status + "', '" +
                        obj[a].remarks + "', '" +
                        obj[a].execution_date + "', '" +
                        obj[a].execution_duration + "', '" +
                        obj[a].colflag + "', '" +
                        obj[a].locked + "', '" +
                        obj[a].lockedBy + "', '" +
                        obj[a].idDoctor + "', '" +
                        obj[a].doctor_name + "')";

                    da = new SQLiteDataAdapter(qry, cn.cn);
                    ds = new DataSet();
                    da.Fill(ds);

                }

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtUserID.Focus();
            }

            finally
            {

            }
        }



        private void downloadDoctors()
        {
            try
            {
                //var request = (HttpWebRequest)WebRequest.CreateHttp("https://vcoe1.aku.edu/naunehal/api/getData.php");
                var request = (HttpWebRequest)WebRequest.CreateHttp(CVariables.getServerURL + CVariables.getDataFileName);


                //string param_json = "{\"table\":\"camp\", \"select\":\"idCamp\", \"iddoctor\", \"doctor_name\", \"check\":\"\" }";



                //var postData = "table=" + Uri.EscapeDataString("camp");
                //postData += "&check=" + Uri.EscapeDataString("1");
                //postData += "&locked=" + Uri.EscapeDataString("0");
                //postData += "&status=" + Uri.EscapeDataString("e");
                //postData += "&cdate=" + Uri.EscapeDataString("03-04-2021");



                //var data = Encoding.ASCII.GetBytes(postData);

                //request.UserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 7.1; Trident/5.0)";


                int winBuild = Environment.OSVersion.Version.Build;
                String userAgent = "NET/5.0 (Windows; Build/" + winBuild + ")";


                request.UserAgent = userAgent;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                //request.ContentLength = data.Length;

                //using (var stream = request.GetRequestStream())
                //{
                //    stream.Write(data, 0, data.Length);
                //}




                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = new JavaScriptSerializer().Serialize(new
                    {
                        table = "camp_dr",
                        select = " idCamp, iddoctor, staff_name ",
                        check = ""

                    });

                    streamWriter.Write(json);
                }


                var response = (HttpWebResponse)request.GetResponse();


                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();




                var obj = JsonConvert.DeserializeObject<List<Doctor>>(responseString);


                CConnection cn = new CConnection();

                SQLiteDataAdapter da = null;
                DataSet ds = null;


                da = new SQLiteDataAdapter("delete from camp_doctors", cn.cn);
                ds = new DataSet();
                da.Fill(ds);


                string qry = "";


                for (int a = 0; a <= obj.Count - 1; a++)
                {

                    qry = "insert into camp_doctors (idCamp, iddoctor, staff_name) values('" + obj[a].idCamp + "', '" + obj[a].iddoctor + "', '" + obj[a].staff_name + "')";

                    da = new SQLiteDataAdapter(qry, cn.cn);
                    ds = new DataSet();
                    da.Fill(ds);

                }



                MessageBox.Show("Finished downloading data ", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtUserID.Focus();
            }

            finally
            {

            }
        }


        private void cmdDownloadUsers_Click(object sender, EventArgs e)
        {
            if (ddlDistrict.SelectedValue == "0")
            {
                MessageBox.Show("In order to download select district first ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ddlDistrict.Focus();
            }
            else
            {
                //downloadDistricts();
                //getDistrict_FromSqliteDB();

                //getDistrict_Hardcoded();

                cmdDownloadUsers.Text = "Please wait Downloading Data";

                downloadCamps();
                downloadUsers();
                downloadDoctors();

                cmdDownloadUsers.Text = "Download Data";
            }
        }




        class User
        {

            public string username { get; set; }
            public string password { get; set; }
            public string dist_id { get; set; }
            public string full_name { get; set; }
            public string auth_level { get; set; }
            public string enabled { get; set; }
            public string designation { get; set; }
            public string deviceid { get; set; }
            public string formdate { get; set; }
            public string approve_date { get; set; }
            public string approved_by { get; set; }
            public string dd { get; set; }
            public string colflag { get; set; }

        }



        class Doctor
        {
            public string idCamp { get; set; }
            public string iddoctor { get; set; }
            public string staff_name { get; set; }
        }



        class Users
        {
            public string id { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string dist_id { get; set; }
        }



        class Districts
        {
            public string dist_id { get; set; }
            public string district { get; set; }
            public string province { get; set; }
        }


        class Camp_Patient
        {

            public string idCamp { get; set; }
            public string dist_id { get; set; }
            public string district { get; set; }
            public string ucCode { get; set; }
            public string ucName { get; set; }
            public string area_no { get; set; }
            public string area_name { get; set; }
            public string plan_date { get; set; }
            public string camp_no { get; set; }
            public string camp_status { get; set; }
            public string remarks { get; set; }
            public string execution_date { get; set; }
            public string execution_duration { get; set; }
            public string colflag { get; set; }
            public string locked { get; set; }
            public string lockedBy { get; set; }
            public string idDoctor { get; set; }
            public string doctor_name { get; set; }

        }



        class ucs_model
        {

            public string uc_code { get; set; }
            public string uc_name { get; set; }
            public string district_code { get; set; }


        }

        private void cmdLogin_Click(object sender, EventArgs e)
        {
            if (checkDoctorsExists() == true)
            {
                Login();
            }
            else
            {
                MessageBox.Show("Doctors does not exist please download first", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        private void Login()
        {
            if (string.IsNullOrEmpty(txtUserID.Text))
            {
                MessageBox.Show("Please enter user id ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtUserID.Focus();
            }
            else if (string.IsNullOrEmpty(txtPasswd.Text))
            {
                MessageBox.Show("Please enter password ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPasswd.Focus();
            }
            else if (ddlDistrict.SelectedValue == "0")
            {
                MessageBox.Show("Please select district ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ddlDistrict.Focus();
            }
            else
            {

                CConnection cn = new CConnection();

                SQLiteDataAdapter da = new SQLiteDataAdapter("select * from users where username='" + txtUserID.Text + "' and password='" + txtPasswd.Text + "' and dist_id='" + ddlDistrict.SelectedValue.ToString() + "'", cn.cn);
                DataSet ds = new DataSet();
                da.Fill(ds);

                if (ds != null)
                {
                    if (ds.Tables.Count > 0)
                    {
                        if (ds.Tables[0].Rows.Count > 0)
                        {

                            CVariables.UserID = ds.Tables[0].Rows[0][0].ToString();
                            CVariables.UserName = ds.Tables[0].Rows[0]["username"].ToString();
                            CVariables.GetPassword = ds.Tables[0].Rows[0]["password"].ToString();
                            //CVariables.GetDBName = "gbdata";


                            CVariables.IsUserFirstOrSecond = "User1";
                            CVariables.dist_id = ddlDistrict.SelectedValue.ToString();


                            CVariables.frmlogin1 = this;
                            this.Hide();

                            frmMain obj_main = new frmMain();
                            obj_main.Show();
                        }
                        else
                        {
                            MessageBox.Show("User does not exist ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            txtUserID.Focus();
                        }
                    }
                    else
                    {
                        MessageBox.Show("User does not exist ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        txtUserID.Focus();
                    }
                }
                else
                {
                    MessageBox.Show("User does not exist ", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtUserID.Focus();
                }
            }
        }

        private void cmdCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }



        public void upload_forms_master()
        {
            List<forms_data_master> datas = new List<forms_data_master>();

            datas = fetchData_master();
            int total = datas.Count;

            if (datas.Count > 0)
            {

                var data_obj = JsonConvert.SerializeObject(datas);



                var table_var = "[{\"table\":\"camp_patient\", \"check\":\"users\"}, " + data_obj + "]";

                string requestParams = table_var.ToString();
                HttpWebRequest webRequest;


                //webRequest = (HttpWebRequest)WebRequest.Create("https://vcoe1.aku.edu/naunehal/api/sync.php");
                webRequest = (HttpWebRequest)WebRequest.Create(CVariables.getServerURL + CVariables.getSyncFileName);


                int winBuild = Environment.OSVersion.Version.Build;
                String userAgent = "NET/5.0 (Windows; Build/" + winBuild + ")";


                webRequest.Method = "POST";
                webRequest.UserAgent = userAgent;
                webRequest.ContentType = "application/json";


                //  byte[] byteArray = Encoding.UTF8.GetBytes(requestParams);
                using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                {
                    streamWriter.Write(requestParams);
                }


                var result = "";

                var httpResponse = (HttpWebResponse)webRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }


                //JavaScriptSerializer js = new JavaScriptSerializer();
                //SyncedClass sync = js.Deserialize<SyncedClass>("{\"error\":\"fsdfsdf\"}");

                //List<SyncedClass> list = JsonConvert.DeserializeObject<List<SyncedClass>>(result);
                //updateCampPatient_Detail(list[0].id);


                var message = JsonConvert.DeserializeObject<List<SyncedClass>>(result);


                var errormeg = "";
                var statusmessage = "";



                int errcount = 0;
                int statuscount = 0;
                int dupliatecount = 0;
                string id;
                List<string> errormsg = new List<string>();
                var displaymessage = "";


                try
                {
                    foreach (var item in message)
                    {

                        //messg = "\n error: "+item.error;
                        //messg += "\n message: " + item.message;
                        //messg += "\n status: " + item.status;



                        errormeg = item.error;
                        statusmessage = item.status;


                        if (errormeg == "1")
                        {
                            errcount++;
                            errormsg.Add("ID: " + item.id + " : " + item.message);
                        }



                        if (statusmessage == "1")
                        {
                            statuscount++;
                            id = item.id;

                            updateCampPatient_Master(id);
                        }
                        if (statusmessage == "2")
                        {
                            dupliatecount++;
                            id = item.id;

                            updateCampPatient_Master(id);
                        }

                    }

                    displaymessage = "\n  Total:" + total + "\n  Duplicate:" + dupliatecount + "\n  Successfull:" + statuscount + "\n  Error:" + errcount;
                    foreach (var errtext in errormsg)
                    {
                        displaymessage += "\n" + errtext;
                    }
                    MessageBox.Show("Data Upload" + displaymessage, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);



                }
                catch (Exception ex)
                {
                    MessageBox.Show("Data Upload Failed" + ex + result, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No new record upload", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }



            //if (result == null)
            //{
            //    MessageBox.Show("Data Upload", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //else
            //{
            //    MessageBox.Show("Data Upload" + result, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}


        }



        public void upload_forms()
        {
            List<forms_data> datas = new List<forms_data>();

            datas = fetchData();
            int total = datas.Count;

            if (datas.Count > 0)
            {

                var data_obj = JsonConvert.SerializeObject(datas);



                var table_var = "[{\"table\":\"camp_patient_dtl\", \"check\":\"users\"}, " + data_obj + "]";

                string requestParams = table_var.ToString();
                HttpWebRequest webRequest;


                //webRequest = (HttpWebRequest)WebRequest.Create("https://vcoe1.aku.edu/naunehal/api/sync.php");
                webRequest = (HttpWebRequest)WebRequest.Create(CVariables.getServerURL + CVariables.getSyncFileName);


                int winBuild = Environment.OSVersion.Version.Build;
                String userAgent = "NET/5.0 (Windows; Build/" + winBuild + ")";


                webRequest.Method = "POST";
                webRequest.UserAgent = userAgent;
                webRequest.ContentType = "application/json";


                //  byte[] byteArray = Encoding.UTF8.GetBytes(requestParams);
                using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                {
                    streamWriter.Write(requestParams);
                }


                var result = "";

                var httpResponse = (HttpWebResponse)webRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }


                //JavaScriptSerializer js = new JavaScriptSerializer();
                //SyncedClass sync = js.Deserialize<SyncedClass>("{\"error\":\"fsdfsdf\"}");

                //List<SyncedClass> list = JsonConvert.DeserializeObject<List<SyncedClass>>(result);
                //updateCampPatient_Detail(list[0].id);


                var message = JsonConvert.DeserializeObject<List<SyncedClass>>(result);


                var errormeg = "";
                var statusmessage = "";



                int errcount = 0;
                int statuscount = 0;
                int dupliatecount = 0;
                string id;
                List<string> errormsg = new List<string>();
                var displaymessage = "";


                try
                {
                    foreach (var item in message)
                    {

                        //messg = "\n error: "+item.error;
                        //messg += "\n message: " + item.message;
                        //messg += "\n status: " + item.status;



                        errormeg = item.error;
                        statusmessage = item.status;


                        if (errormeg == "1")
                        {
                            errcount++;
                            errormsg.Add("ID: " + item.id + " : " + item.message);
                        }



                        if (statusmessage == "1")
                        {
                            statuscount++;
                            id = item.id;

                            updateCampPatient_Detail(id);
                        }
                        if (statusmessage == "2")
                        {
                            dupliatecount++;
                            id = item.id;

                            updateCampPatient_Detail(id);
                        }

                    }

                    displaymessage = "\n  Total:" + total + "\n  Duplicate:" + dupliatecount + "\n  Successfull:" + statuscount + "\n  Error:" + errcount;
                    foreach (var errtext in errormsg)
                    {
                        displaymessage += "\n" + errtext;
                    }
                    MessageBox.Show("Data Upload" + displaymessage, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);



                }
                catch (Exception ex)
                {
                    MessageBox.Show("Data Upload Failed" + ex + result, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("No new record upload", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }



            //if (result == null)
            //{
            //    MessageBox.Show("Data Upload", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //else
            //{
            //    MessageBox.Show("Data Upload" + result, "Information", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}


        }


        private void updateCampPatient_Master(string id)
        {

            CDBOperations obj_op = null;
            CConnection cn = new CConnection();


            try
            {
                obj_op = new CDBOperations();

                SQLiteDataAdapter da = new SQLiteDataAdapter("update camp_patient set synced=1, synceddate='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "' where id = '" + id + "'", cn.cn);
                DataSet ds = new DataSet();
                da.Fill(ds);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            finally
            {
                obj_op = null;
            }

        }


        private void updateCampPatient_Detail(string id)
        {

            CDBOperations obj_op = null;
            CConnection cn = new CConnection();


            try
            {
                obj_op = new CDBOperations();

                SQLiteDataAdapter da = new SQLiteDataAdapter("update camp_patient_dtl set synced=1, synceddate='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "' where id = '" + id + "'", cn.cn);
                DataSet ds = new DataSet();
                da.Fill(ds);
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            finally
            {
                obj_op = null;
            }

        }


        private void cmdUploadData_Click(object sender, EventArgs e)
        {
            upload_forms_master();
            upload_forms();
        }


        public List<forms_data> fetchData()
        {
            CConnection cn = new CConnection();

            List<forms_data> forms = new List<forms_data>();

            try
            {
                SQLiteDataAdapter da = new SQLiteDataAdapter("select * from camp_patient_dtl where synced is null or synced = ''", cn.cn);
                DataSet ds = new DataSet();
                da.Fill(ds);


                for (int a = 0; a <= ds.Tables[0].Rows.Count - 1; a++)
                {

                    forms_data fd = new forms_data();

                    fd._id = ds.Tables[0].Rows[a]["id"].ToString();
                    fd.deviceid = SystemInformation.ComputerName;
                    fd.sysdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    fd.id = ds.Tables[0].Rows[a]["id"].ToString();
                    fd.form_id = ds.Tables[0].Rows[a]["form_id"].ToString();
                    fd.mh01 = ds.Tables[0].Rows[a]["mh01"].ToString();
                    fd.mh02 = ds.Tables[0].Rows[a]["mh02"].ToString();
                    fd.mh03 = ds.Tables[0].Rows[a]["mh03"].ToString();
                    fd.mh04 = ds.Tables[0].Rows[a]["mh04"].ToString();
                    fd.mh05 = ds.Tables[0].Rows[a]["mh05"].ToString();
                    fd.mh06 = ds.Tables[0].Rows[a]["mh06"].ToString();
                    fd.mh07 = ds.Tables[0].Rows[a]["mh07"].ToString();
                    fd.mh08 = ds.Tables[0].Rows[a]["mh08"].ToString();
                    fd.mh08a = ds.Tables[0].Rows[a]["mh08a"].ToString();


                    fd.mh09y = ds.Tables[0].Rows[a]["mh09y"].ToString();
                    fd.mh09m = ds.Tables[0].Rows[a]["mh09m"].ToString();
                    fd.mh09d = ds.Tables[0].Rows[a]["mh09d"].ToString();

                    fd.mh010 = ds.Tables[0].Rows[a]["mh010"].ToString();
                    fd.mh010a = ds.Tables[0].Rows[a]["mh010a"].ToString();


                    fd.mh011 = ds.Tables[0].Rows[a]["mh011"].ToString();
                    fd.mh012 = ds.Tables[0].Rows[a]["mh012"].ToString();
                    fd.chkWeight = ds.Tables[0].Rows[a]["chkWeight"].ToString();
                    fd.mh013 = ds.Tables[0].Rows[a]["mh013"].ToString();
                    fd.mh014 = ds.Tables[0].Rows[a]["mh014"].ToString();
                    fd.mh015 = ds.Tables[0].Rows[a]["mh015"].ToString();
                    fd.chkHeight = ds.Tables[0].Rows[a]["chkHeight"].ToString();
                    fd.mh016 = ds.Tables[0].Rows[a]["mh016"].ToString();
                    fd.chkMUAC = ds.Tables[0].Rows[a]["chkMUAC"].ToString();


                    fd.mh01701 = ds.Tables[0].Rows[a]["mh01701"].ToString();
                    fd.mh01702 = ds.Tables[0].Rows[a]["mh01702"].ToString();
                    fd.mh01703 = ds.Tables[0].Rows[a]["mh01703"].ToString();
                    fd.mh01704 = ds.Tables[0].Rows[a]["mh01704"].ToString();
                    fd.mh01705 = ds.Tables[0].Rows[a]["mh01705"].ToString();

                    fd.mh017077 = ds.Tables[0].Rows[a]["mh017077"].ToString();
                    fd.mh017077x = ds.Tables[0].Rows[a]["mh017077x"].ToString();


                    fd.mh01801 = ds.Tables[0].Rows[a]["mh01801"].ToString();
                    fd.mh01802 = ds.Tables[0].Rows[a]["mh01802"].ToString();
                    fd.mh01803 = ds.Tables[0].Rows[a]["mh01803"].ToString();
                    fd.mh01804 = ds.Tables[0].Rows[a]["mh01804"].ToString();
                    fd.mh01805 = ds.Tables[0].Rows[a]["mh01805"].ToString();
                    fd.mh01806 = ds.Tables[0].Rows[a]["mh01806"].ToString();
                    fd.mh01807 = ds.Tables[0].Rows[a]["mh01807"].ToString();
                    fd.mh01808 = ds.Tables[0].Rows[a]["mh01808"].ToString();
                    fd.mh01809 = ds.Tables[0].Rows[a]["mh01809"].ToString();
                    fd.mh018010 = ds.Tables[0].Rows[a]["mh018010"].ToString();
                    fd.mh018011 = ds.Tables[0].Rows[a]["mh018011"].ToString();
                    fd.mh018012 = ds.Tables[0].Rows[a]["mh018012"].ToString();
                    fd.mh018013 = ds.Tables[0].Rows[a]["mh018013"].ToString();
                    fd.mh018014 = ds.Tables[0].Rows[a]["mh018014"].ToString();
                    fd.mh018015 = ds.Tables[0].Rows[a]["mh018015"].ToString();
                    fd.mh018016 = ds.Tables[0].Rows[a]["mh018016"].ToString();
                    fd.mh018077 = ds.Tables[0].Rows[a]["mh018077"].ToString();
                    fd.mh018077x = ds.Tables[0].Rows[a]["mh018077x"].ToString();

                    fd.chkNoneDiag = ds.Tables[0].Rows[a]["chkNoneDiag"].ToString();


                    fd.mh01901 = ds.Tables[0].Rows[a]["mh01901"].ToString();
                    fd.mh01902 = ds.Tables[0].Rows[a]["mh01902"].ToString();
                    fd.mh01903 = ds.Tables[0].Rows[a]["mh01903"].ToString();
                    fd.mh01904 = ds.Tables[0].Rows[a]["mh01904"].ToString();
                    fd.mh01905 = ds.Tables[0].Rows[a]["mh01905"].ToString();
                    fd.mh01906 = ds.Tables[0].Rows[a]["mh01906"].ToString();
                    fd.mh01907 = ds.Tables[0].Rows[a]["mh01907"].ToString();
                    fd.mh01908 = ds.Tables[0].Rows[a]["mh01908"].ToString();
                    fd.mh01909 = ds.Tables[0].Rows[a]["mh01909"].ToString();
                    fd.mh019010 = ds.Tables[0].Rows[a]["mh019010"].ToString();
                    fd.mh019011 = ds.Tables[0].Rows[a]["mh019011"].ToString();
                    fd.mh019012 = ds.Tables[0].Rows[a]["mh019012"].ToString();
                    fd.mh019013 = ds.Tables[0].Rows[a]["mh019013"].ToString();
                    fd.mh019014 = ds.Tables[0].Rows[a]["mh019014"].ToString();
                    fd.mh019015 = ds.Tables[0].Rows[a]["mh019015"].ToString();
                    fd.mh019077 = ds.Tables[0].Rows[a]["mh019077"].ToString();
                    fd.mh019077x = ds.Tables[0].Rows[a]["mh019077x"].ToString();
                    fd.chkNone = ds.Tables[0].Rows[a]["chkNone"].ToString();


                    fd.mh020 = ds.Tables[0].Rows[a]["mh020"].ToString();
                    fd.mh021 = ds.Tables[0].Rows[a]["mh021"].ToString();
                    fd.mh022 = ds.Tables[0].Rows[a]["mh022"].ToString();
                    fd.mh023 = ds.Tables[0].Rows[a]["mh023"].ToString();
                    fd.mh024 = ds.Tables[0].Rows[a]["mh024"].ToString();
                    fd.mh025 = ds.Tables[0].Rows[a]["mh025"].ToString();


                    fd.mh02601 = ds.Tables[0].Rows[a]["mh02601"].ToString();
                    fd.mh02602 = ds.Tables[0].Rows[a]["mh02602"].ToString();
                    fd.mh02603 = ds.Tables[0].Rows[a]["mh02603"].ToString();
                    fd.mh02604 = ds.Tables[0].Rows[a]["mh02604"].ToString();
                    fd.mh02605 = ds.Tables[0].Rows[a]["mh02605"].ToString();
                    fd.mh02606 = ds.Tables[0].Rows[a]["mh02606"].ToString();
                    fd.mh027a = ds.Tables[0].Rows[a]["mh027a"].ToString();

                    fd.mh02607 = ds.Tables[0].Rows[a]["mh02607"].ToString();
                    fd.mh02608 = ds.Tables[0].Rows[a]["mh02608"].ToString();
                    fd.mh02609 = ds.Tables[0].Rows[a]["mh02609"].ToString();
                    fd.mh026010 = ds.Tables[0].Rows[a]["mh026010"].ToString();
                    fd.mh026011 = ds.Tables[0].Rows[a]["mh026011"].ToString();
                    fd.mh027b = ds.Tables[0].Rows[a]["mh027b"].ToString();

                    fd.mh026012 = ds.Tables[0].Rows[a]["mh026012"].ToString();
                    fd.mh026013 = ds.Tables[0].Rows[a]["mh026013"].ToString();
                    fd.mh026014 = ds.Tables[0].Rows[a]["mh026014"].ToString();
                    fd.mh026015 = ds.Tables[0].Rows[a]["mh026015"].ToString();
                    fd.mh026016 = ds.Tables[0].Rows[a]["mh026016"].ToString();
                    fd.mh026017 = ds.Tables[0].Rows[a]["mh026017"].ToString();
                    fd.mh026018 = ds.Tables[0].Rows[a]["mh026018"].ToString();
                    fd.mh026019 = ds.Tables[0].Rows[a]["mh026019"].ToString();
                    fd.chkVaccination = ds.Tables[0].Rows[a]["chkVaccination"].ToString();


                    fd.mh027 = ds.Tables[0].Rows[a]["mh027"].ToString();
                    fd.mh028 = ds.Tables[0].Rows[a]["mh028"].ToString();
                    fd.mh029 = ds.Tables[0].Rows[a]["mh029"].ToString();

                    fd.mh033 = ds.Tables[0].Rows[a]["mh033"].ToString();
                    fd.mh032 = ds.Tables[0].Rows[a]["mh032"].ToString();

                    fd.mh030 = ds.Tables[0].Rows[a]["mh030"].ToString();
                    fd.mh031 = ds.Tables[0].Rows[a]["mh031"].ToString();

                    fd.mh030a = ds.Tables[0].Rows[a]["mh030a"].ToString();


                    fd.mh01101 = ds.Tables[0].Rows[a]["mh01101"].ToString();
                    fd.mh01102 = ds.Tables[0].Rows[a]["mh01102"].ToString();
                    fd.mh01103 = ds.Tables[0].Rows[a]["mh01103"].ToString();
                    fd.mh01104 = ds.Tables[0].Rows[a]["mh01104"].ToString();


                    fd.uccode = ds.Tables[0].Rows[a]["uccode"].ToString();
                    fd.dist_id = ds.Tables[0].Rows[a]["dist_id"].ToString();
                    fd.databy = "desktop";
                    fd.userid = ds.Tables[0].Rows[a]["userid"].ToString();
                    fd.entrydate = ds.Tables[0].Rows[a]["entrydate"].ToString();
                    fd.master_id = ds.Tables[0].Rows[a]["master_id"].ToString();


                    forms.Add(fd);

                }

            }

            catch (Exception ex)
            {

            }

            finally
            {

            }

            return forms;
        }



        public List<forms_data_master> fetchData_master()
        {
            CConnection cn = new CConnection();

            List<forms_data_master> forms = new List<forms_data_master>();

            try
            {
                SQLiteDataAdapter da = new SQLiteDataAdapter("select * from camp_patient where synced is null or synced = ''", cn.cn);
                DataSet ds = new DataSet();
                da.Fill(ds);


                for (int a = 0; a <= ds.Tables[0].Rows.Count - 1; a++)
                {

                    forms_data_master fd = new forms_data_master();

                    fd._id = ds.Tables[0].Rows[a]["id"].ToString();
                    fd.deviceid = SystemInformation.ComputerName;
                    fd.sysdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    fd.id = ds.Tables[0].Rows[a]["id"].ToString();
                    fd.mh01 = ds.Tables[0].Rows[a]["mh01"].ToString();
                    fd.mh02 = ds.Tables[0].Rows[a]["mh02"].ToString();
                    fd.mh03 = ds.Tables[0].Rows[a]["mh03"].ToString();
                    fd.mh04 = ds.Tables[0].Rows[a]["mh04"].ToString();
                    fd.mh05 = ds.Tables[0].Rows[a]["mh05"].ToString();
                    fd.uccode = ds.Tables[0].Rows[a]["uccode"].ToString();
                    fd.dist_id = ds.Tables[0].Rows[a]["dist_id"].ToString();
                    fd.databy = "desktop";
                    fd.userid = ds.Tables[0].Rows[a]["userid"].ToString();
                    fd.entrydate = ds.Tables[0].Rows[a]["entrydate"].ToString();
                    fd.form_id = ds.Tables[0].Rows[a]["form_id"].ToString();


                    forms.Add(fd);

                }

            }

            catch (Exception ex)
            {

            }

            finally
            {

            }

            return forms;
        }



        public class forms_data
        {
            public string _id;
            public string form_id;
            public string deviceid;
            public string sysdate;
            public string id;
            public string mh01;
            public string mh02;
            public string mh03;
            public string mh04;
            public string mh05;
            public string mh06;
            public string mh07;
            public string mh08;
            public string mh08a;
            public string mh09y;
            public string mh09m;
            public string mh09d;
            public string mh010;
            public string mh010a;
            public string mh011;
            public string mh012;
            public string chkWeight;
            public string mh013;
            public string mh014;
            public string mh015;
            public string chkHeight;
            public string mh016;
            public string chkMUAC;
            public string mh01701;
            public string mh01702;
            public string mh01703;
            public string mh01704;
            public string mh01705;
            public string mh017077;
            public string mh017077x;

            public string mh01801;
            public string mh01802;
            public string mh01803;
            public string mh01804;
            public string mh01805;
            public string mh01806;
            public string mh01807;
            public string mh01808;
            public string mh01809;
            public string mh018010;
            public string mh018011;
            public string mh018012;
            public string mh018013;
            public string mh018014;
            public string mh018015;
            public string mh018016;
            public string mh018077;
            public string mh018077x;
            public string chkNoneDiag;


            public string mh01901;
            public string mh01902;
            public string mh01903;
            public string mh01904;
            public string mh01905;
            public string mh01906;
            public string mh01907;
            public string mh01908;
            public string mh01909;
            public string mh019010;
            public string mh019011;
            public string mh019012;
            public string mh019013;
            public string mh019014;
            public string mh019015;
            public string mh019077;
            public string mh019077x;
            public string chkNone;


            public string mh020;
            public string mh021;
            public string mh022;
            public string mh023;
            public string mh024;
            public string mh025;

            public string mh02601;
            public string mh02602;
            public string mh02603;
            public string mh02604;
            public string mh02605;
            public string mh02606;
            public string mh02607;
            public string mh02608;
            public string mh02609;
            public string mh026010;
            public string mh026011;
            public string mh026012;
            public string mh026013;
            public string mh026014;
            public string mh026015;
            public string mh026016;
            public string mh026017;
            public string mh026018;
            public string mh026019;
            public string chkVaccination;

            public string mh027;
            public string mh028;
            public string mh029;


            public string mh033;
            public string mh032;

            public string mh030;
            public string mh030a;
            public string mh031;


            public string mh01101;
            public string mh01102;
            public string mh01103;
            public string mh01104;

            public string mh027a;
            public string mh027b;

            public string uccode;
            public string dist_id;
            public string databy;
            public string userid;
            public string entrydate;

            public string master_id;

        }



        public class forms_data_master
        {
            public string _id;
            public string form_id;
            public string deviceid;
            public string sysdate;
            public string id;
            public string mh01;
            public string mh02;
            public string mh03;
            public string mh04;
            public string mh05;

            public string uccode;
            public string dist_id;
            public string databy;

            public string userid;
            public string entrydate;

        }



        public class SyncMsg
        {
            public string syncdata { get; set; }
        }


        public class SyncedClass
        {
            public string error { get; set; }
            public string message { get; set; }
            public string status { get; set; }
            public string id { get; set; }
        }


    }

}
