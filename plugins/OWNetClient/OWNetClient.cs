using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using org.owfs.ownet;

namespace X13.Periphery
{
    [Export(typeof(IPlugModul))]
    [ExportMetadata("priority", 7)]
    [ExportMetadata("name", "OWNet")]
    public class OWNetClient : IPlugModul
    {
        private Topic _dev1w;
        private const long _version = 322;

        OWNetGate owS;

        public void Init()
        {
            owS = new OWNetGate();

            Topic.root.Subscribe("/etc/OWNet/#", Dummy);
            Topic.root.Subscribe("/etc/declarers/OWNet/#", Dummy);
            Topic.root.Subscribe("/dev/OWNet/+", Dummy);
        }

        public void Start()
        {

            _dev1w = Topic.root.Get("/dev/OWNet");
            var ver = Topic.root.Get<long>("/etc/OWNet/version");
            if (ver.value < _version)
            {
                ver.saved = true;
                ver.value = _version;
                Log.Debug("Load OWNet declarers");
                var st = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("X13.Periphery.OWNetClient.xst");
                if (st != null)
                {
                    using (var sr = new System.IO.StreamReader(st))
                    {
                        Topic.Import(sr, null);
                    }
                }

            }


           Topic.root.Subscribe("/dev/OWNet/0_Empty", Dummy);

           var url = Topic.root.Get<string>("/dev/OWNet/0_Empty/setUrl");
           var port = Topic.root.Get<long>("/dev/OWNet/0_Empty/setPort");
           var tag = Topic.root.Get<string>("/dev/OWNet/0_Empty/setTag");
           //var _declarer = Topic.root.Get<string>("/dev/OWNet/0_Empty/_declarer");
        

           url.value = "localhost";
           port.value = 4304;
           tag.value = "My RSPI";
           //_declarer.value = "OWNetServ"; //for imange -
           // Если в папке есть переменная "_declarer", то для неё ищется описание в /etc/declarers/+/<имя> тип string. 
           // Значение и берётся как URI картинки.


           Log.Info("Load OWNet plugin");

           owS.Start();
       
        }

 
        public void Stop()
        {
            owS.Stop();         
        }



        private void Dummy(Topic src, TopicChanged arg)
        {
        }


    }
}
