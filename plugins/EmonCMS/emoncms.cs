#region license
//Copyright (c) 2011-2014 <comparator@gmx.de>; Wassili Hense

//This file is part of the X13.Home project.
//https://github.com/X13home

//BSD License
//See LICENSE.txt file for license details.
#endregion license
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Net;

namespace X13.PLC
{

    [Export(typeof(IStatement))]
    [ExportMetadata("declarer", "EmonCMS")]
    public class EmonCMS : IStatement
    {
        private DVar<bool> _push;
        private DVar<string> _feed;
        private DVar<string> _key;
        private DVar<string> _node;
        private DVar<string> _url;
        

        public void Load()
        {
            var m = Topic.root.Get<string>("/etc/declarers/func/EmonCMS");
            m.value = "pack://application:,,/EmonCMS;component/Images/fu_emon.png";
            m.Get<string>("_description").value = "v Export to EmonCMS";
            m.Get<string>("Push").value = "Az";
            m.Get<string>("A").value = "Bg";
            m.Get<string>("B").value = "Cg";
            m.Get<string>("C").value = "Dg";
            m.Get<string>("D").value = "Eg";
            m.Get<string>("E").value = "Fg";
            m.Get<string>("F").value = "Gg";
            m.Get<string>("G").value = "Hg";
            m.Get<string>("rename").value = "|R";
            m.Get<string>("remove").value = "}D";
        }

        public void Init(DVar<PiStatement> model)
        {

            BiultInStatements.AddPin<double>(model, "A");
            _push = BiultInStatements.AddPin<bool>(model, "Push");
            _url = BiultInStatements.AddPin<string>(model, "_url");
            _feed = BiultInStatements.AddPin<string>(model, "_feed");
            _key = BiultInStatements.AddPin<string>(model, "_key");
            _node = BiultInStatements.AddPin<string>(model, "_node");
        }

        public void Calculate(DVar<PiStatement> model, Topic source)
        {
        
            if (string.IsNullOrEmpty(_feed.value) || string.IsNullOrEmpty(_key.value) || !_push.value)
            {             
                return;
            }

            if (source == _push)
            {
                
                string valS = "";
                foreach (var inp in model.children.Where(z => z is DVar<double> && z.name.Length == 1 && z.name[0] >= 'A' && z.name[0] <= 'G').Cast<DVar<double>>())
                {
                    valS = inp.value.ToString(CultureInfo.InvariantCulture);
                    {
                        int i = Math.Max(valS.IndexOf('.'), 6);
                        if (i < valS.Length)
                        {
                            valS = valS.Substring(0, i);
                        }
                    }
                    
                }

                Log.Debug("1 - Emon _feed.value: ({0}) ue: ({1})", _feed.value, valS);
               
                ThreadPool.QueueUserWorkItem((o) => Send(_key.value, _feed.value, valS, _node.value, _url.value));
            }
            else if (source.valueType == typeof(double) && source.name.Length == 1 && source.name[0] >= 'A' && source.name[0] <= 'G')
            {            
                string p = _feed.value + "_" + source.name;
                string v = (source as DVar<double>).value.ToString(CultureInfo.InvariantCulture);
                
                Log.Debug("2 - Emon  p ({0}) - ({1}) ", p, v);
                
                ThreadPool.QueueUserWorkItem((o) => Send(_key.value, p, v, _node.value, _url.value));
            }


        }
        private void Send(string apiKey, string feedId, string sample, string node, string emonurl)
        {
            try
            {
                
                //example  api not corect
                //http://ai.prolv.net/input/post.json?node=1&json={temp:-3.5}&apikey=b8778908fafbf3ace0c641e517aacb0


                string string_put;
                string_put = emonurl + "?node="+ node +"&json={" + feedId + ":" + sample + "}&apikey=" + apiKey;
               
                Log.Debug("Emon string_put: ({0})", string_put);
               
                var request = (HttpWebRequest)WebRequest.Create(string_put);
                // request line
                request.Method = "PUT";

                request.Timeout = 5000;     // 5 seconds
                // send request and receive response
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {

                    }
                }
                request = null;
            }
            catch (Exception ex)
            {
                Log.Warning("Emon({0}) - {1}", feedId, ex.Message);
            }
        }

        public void DeInit()
        {
        }
        
    }

}