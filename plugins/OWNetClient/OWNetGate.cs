using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using org.owfs.ownet;
using System.Text.RegularExpressions;
using System.Globalization;

namespace X13.Periphery
{
    [Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public class OWNetGate : ITopicOwned
    {
        internal protected Topic _owner;
        private bool _run;
        
        internal void Start()
        {
            _run = true;
            System.Threading.ThreadPool.QueueUserWorkItem(Connect); //check every two minutes  for present server
            System.Threading.ThreadPool.QueueUserWorkItem(Add);
            System.Threading.ThreadPool.QueueUserWorkItem(Read);
        }
        internal void Stop()
        {
            _run = false;
        }

       private void Connect(object o) 
        {    
            while (_run)
            {
                Topic owServers = Topic.root.Get("/dev/OWNet");
                var cServ = owServers.children.Where(e => e.name != "0_Empty");

                foreach (var serv in cServ)
                {
                    Log.Debug("Owner: [{0}]", serv.name);
                    Log.Debug("Path: [{0}]", serv.path);


                    string url = serv.Get<string>(serv.path + "/setUrl").value;
                    long port = serv.Get<long>(serv.path + "/setPort").value;
                    string tag = serv.Get<string>(serv.path + "/setTag").value;

                    Log.Debug("setUrl: [{0}]", url);
                    Log.Debug("setPort: [{0}]", port);
                    Log.Debug("setTag: [{0}]", tag);                 

                    bool isConnect = false;

                    int int_port = Convert.ToInt16(port);

                    OWNet owS = new OWNet(url, int_port);
                    owS.PersistentConnection = false;

                    var owSconnect = Topic.root.Get<bool>(serv.path + "/setConnect");
                    owSconnect.saved = false;
                    owSconnect.value = false;
                    var _declarer = Topic.root.Get<string>(serv.path + "/_declarer");
                    _declarer.value = "OWNetServ";

                    try
                    {
                        owS.Connect();
                        isConnect = true;                   
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("OWNet server ping - Failed [{0}, {1}] - {2}", url, int_port, ex.Message);
                    }

                    if (isConnect == true)
                    {
                        Log.Debug("OWNet server ping - ok: [{0}, {1}]", url, int_port);
                        owSconnect.value = true;
                    }

                    owS.Disconnect(); 
                }

                Thread.Sleep(120000);
            }

        }

       private void Add(object o)
       {
           while (_run)
           {
             
               Topic owServers = Topic.root.Get("/dev/OWNet");
               var cServ = owServers.children.Where(e => e.name != "0_Empty");

               foreach (var serv in cServ)
               {
                   //Log.Debug("Owner: [{0}]", serv.name);
                   //Log.Debug("Path: [{0}]", serv.path);

                   string url = serv.Get<string>(serv.path + "/setUrl").value;
                   long port = serv.Get<long>(serv.path + "/setPort").value;
                   string tag = serv.Get<string>(serv.path + "/setTag").value;
                   bool connect = serv.Get<bool>(serv.path + "/setConnect").value;

                   //Log.Debug("setUrl: [{0}]", url);
                   //Log.Debug("setPort: [{0}]", port);
                   //Log.Debug("setTag: [{0}]", tag);
                   //Log.Debug("connect: [{0}]", connect);

                   if (connect == true)
                   {
                       bool isConnect = false;
                       int int_port = Convert.ToInt16(port);

                       OWNet owS = new OWNet(url, int_port);
                       owS.PersistentConnection = false;

                       try
                       {
                           owS.Connect();
                           isConnect = true;
                       }
                       catch (Exception ex)
                       {
                           Log.Debug("OWNet.Failed [{0}, {1}] - {2}", url, int_port, ex.Message);
                       }

                       if (isConnect == true)
                       {                          
                           ///add devices
                           String[] folders = owS.DirAll("/");
                           foreach (String s in folders)
                           {
                            Match match = Regex.Match(s, @"^\W\d", RegexOptions.IgnoreCase);

                               //  if (s.StartsWith("/")) show all parametrs
                               if (match.Success)
                                {
                               
                               
                               
                               /// If dev exist system  just set  present. no add any parametr (addres temperature and more.)

                               var owDlist = this.GetDevices("All");                            
                               string toCompare = serv + s;
                               var sDev = owDlist.Find(toCompare.Equals);  //Compare for add new dev. if dev  exist no need add. just add active.

                               var sDev_all = Topic.root.Get<bool>(toCompare + "/0all"); // if  true - allow shows for all value

                               if (sDev_all.value  == true)
                               { sDev = null; }


                               if (sDev == null)
                               {

                                     
                                   try
                                   {
                                       // ^\W\d - first no word symbol , second digital, get only format /**.***** (/28.***)


                                           Topic.root.Subscribe(serv.path + s, Dummy);
                                           Log.Debug("Add dev [{0}] ", s);
                                           var owDevPresent = Topic.root.Get<bool>(serv.path + s + "/0Present");
                                           owDevPresent.saved = false;
                                           owDevPresent.value = true;
                                           var owDevName = Topic.root.Get<string>(serv.path + s + "/0Name");
                                           owDevName.saved = true;
                                           owDevName.value = "Dev Name";
                                           var all = Topic.root.Get<bool>(serv.path + s + "/0all"); // if  true - allow shows for all value
                                           all.saved = true;
                                           all.value = false;
                                           var _declarer = Topic.root.Get<string>(serv.path + s + "/_declarer");
                                           _declarer.value = "OWNet_Dev";


                                           String[] sdevs = owS.DirAll(s);
                                           foreach (String s2 in sdevs)
                                           {

                                                Topic.root.Subscribe(serv.path + s2, Dummy);

                                                var get_value = Topic.root.Get<double>(serv.path + s2 + "/get_value");
                                                var get = Topic.root.Get<bool>(serv.path + s2 + "/get"); // if  true - allow get it value
                                                get.saved = true;


                                                if (s2.Contains("temp"))
                                                {
                                                    var _declarer2 = Topic.root.Get<string>(serv.path + s2 + "/_declarer");
                                                    _declarer2.value = "OWNet_T";
                                                }

                                                if (s2.Contains("humi"))
                                                {
                                                    var _declarer2 = Topic.root.Get<string>(serv.path + s2 + "/_declarer");
                                                    _declarer2.value = "OWNet_H";
                                                }

                                    

                                           }
                                     

                                   }
                                   catch
                                   {
                                       Log.Debug("Empty Device [{0}] ", s);
                                   }

                               }
                               else 
                               {
                                   var owDevPresent = Topic.root.Get<bool>(serv.path + s + "/0Present");
                                   owDevPresent.saved = false;
                                   owDevPresent.value = true;
                                   var all = Topic.root.Get<bool>(serv.path + s + "/0all"); // if  true - allow shows for all value
                                   all.saved = true;
                                   all.value = false;



                               }


                           } 

                           }

                   }

                       owS.Disconnect();
                  
                   }
                
               }

               Thread.Sleep(30000);
           }

       }

       private void Read(object o)
       {
           while (_run)
           {

               var readDevs = this.GetDevices("Read"); //"/dev/OWNet/RSPI owserver/09.CCFC23030000/temperature"

               foreach (var bSplit in readDevs)
               {
                   var  aSplit = bSplit.Split('/');

                   string owSpath = "/" + aSplit[1] + "/" + aSplit[2] + "/" + aSplit[3];
                   string readValue = "/" + aSplit[4] + "/" + aSplit[5];

                   string url = Topic.root.Get<string>(owSpath + "/setUrl").value;
                   long port = Topic.root.Get<long>(owSpath + "/setPort").value;
                   int int_port = Convert.ToInt16(port);

                   OWNet owS = new OWNet(url, int_port);
                   owS.PersistentConnection = false;

                   bool owSconnect = false;
                   string getValueS = "0";
                   double getValueD = 0;

                   try
                   {
                       owS.Connect();
                       owSconnect = true;                    
                   }
                   catch (Exception ex)
                   {
                       Log.Debug("OWNet.Failed [{0}, {1}] - {2}", url, int_port, ex.Message);
                   }

                   if (owSconnect == true)
                   {

                       try
                       {
                           getValueS = owS.Read(readValue);

                       }
                       catch
                       {  }
                       Thread.Sleep(2000);
                       try
                       {
                           
                           getValueS = owS.Read(readValue);
                       }
                       catch
                       { }
                   }

   
                   string uiSep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
                   
                   if (uiSep.Equals(","))
                   {
                       double.TryParse(getValueS.Replace(" ", "").Replace(".", ","), out getValueD);
                   }
                   else
                   {
                       double.TryParse(getValueS.Replace(" ", "").Replace(",", "."), out getValueD);
                   }

                   Log.Debug("string [{0}]: double[{1}] , Separ[{2}] ", getValueS, getValueD, uiSep);




                   owS.Disconnect();

                   if (getValueS != "0")
                   {
                       var get_value = Topic.root.Get<double>(bSplit + "/get_value");

                       if (getValueD != get_value.value)
                       {
                           get_value.value = getValueD;
                       }
                       else
                       { get_value.value = 0; }
                                        
                   }

                   
               }
               Thread.Sleep(60000);
           }
       }      

       private List<string> GetServers(string Type)
       {
           ///
           // Retur List Servers Ownet: ALL server in topic /dev/OWNet exclude 0_Epty, Active only active with (setConnect == true)  
           ///

           var owSpath = new List<string>();
  
           Topic owServers = Topic.root.Get("/dev/OWNet");
           var cServ = owServers.children.Where(e => e.name != "0_Empty");

           if (Type == "All")
           {
               
               foreach (var serv in cServ)
               {                                 
                    owSpath.Add(serv.path);                 
               }
              
               return (owSpath);
           }

           if (Type == "Active")
           {

               foreach (var serv in cServ)
               {
                   bool connect = serv.Get<bool>(serv.path + "/setConnect").value;

                   if (connect == true)
                   {
                       owSpath.Add(serv.path);
                   }               
               }

               return (owSpath);
           }

           return null;
       }


       private List<string> GetDevices(string Type)
       {

           ///
           // Retur 1-wire device path List  : ALL device in topic every server exclude 0_Epty, Active only active with (setConnect == true)  
           ///

           var owDpath = new List<string>();
           var cServ = this.GetServers("Active");

           if (Type == "All")
               {
                    foreach (var serv in cServ)
                        {
                            Topic owServers = Topic.root.Get(serv);                          
                            var cDevs = owServers.children.Where(d => d != null);

                            foreach (var dev in cDevs)
                            {
                               // string test = dev.path - serv;

                                string onlyDev = dev.path.Replace(serv, "");
                                Match match = Regex.Match(onlyDev, @"^\W\d", RegexOptions.IgnoreCase);

                                   //  if (s.StartsWith("/")) show all parametrs
                               if (match.Success)
                               {
                                   owDpath.Add(dev.path);
                               }
                               
                            }
                        }
                 return (owDpath);
                }

           
           if (Type == "Active")
                {
                    foreach (var serv in cServ)
                        {
                            Topic owServers = Topic.root.Get(serv);
                            var cDevs = owServers.children.Where(d => d != null);

                            foreach (var dev in cDevs)
                            {

                                
                                string onlyDev = dev.path.Replace(serv, "");
                                Match match = Regex.Match(onlyDev, @"^\W\d", RegexOptions.IgnoreCase);

                                   //  if (s.StartsWith("/")) show all parametrs
                                if (match.Success)
                                {

                                    bool connect = dev.Get<bool>(dev.path + "/0Present").value;

                                    if (connect == true)
                                    {
                                        owDpath.Add(dev.path);
                                    }
                                }
                            }

                        }
               return (owDpath);
           
               }

           if (Type == "Read")
           {
               var activeDevs = this.GetDevices("Active"); //"/dev/OWNet/RSPI owserver/09.CCFC23030000"

               foreach (var activeDev in activeDevs)
               {
                   Topic owServers = Topic.root.Get(activeDev);
                   var cDevs = owServers.children.Where(d => d != null);

                   foreach (var dev in cDevs)
                   {

   
                       string sCheck = dev.path.Replace(owServers.path, "");

                       if (!sCheck.StartsWith("/0") && !sCheck.StartsWith("/_")) 
                                                     
                               {


                                  bool get = dev.Get<bool>(dev.path + "/get").value;

                                 if (get == true)
                                   {
                                       owDpath.Add(dev.path);
                                       //Log.Debug("Get : [{0}]", dev.path);
                                    }

                                }
                   }

               }
               return (owDpath);

           }
               return null;

       }

        
        public void SetOwner(Topic owner)
        {
           // Log.Info("Owner: [{0}]", owner);

            if (!object.ReferenceEquals(owner, _owner))
            {
                
                _owner = owner;
                if (_owner != null)
                {

                   /// _tPresent = _owner.Get<bool>("_present");
                   /// _tPresent.saved = false;

                    //_owner.Get<string>("_declarer", _owner).value = _decl;

                }

            }
        }


        private void Dummy(Topic src, TopicChanged arg)
        {
        }


    }

}