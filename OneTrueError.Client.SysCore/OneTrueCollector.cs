using OneTrueError.Client.Contracts;
using OneTrueError.Client.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneTrueError.Client.SysCore
{
    public class OneTrueCollector
    {
        private ObjectToContextCollectionConverter _converter;
        internal List<ContextCollectionDTO> Items { get; set; }

        public OneTrueCollector()
        {
            _converter = new ObjectToContextCollectionConverter();
            Items = new List<ContextCollectionDTO>();
        }

        public void Collect(string name, object instance)
        {
            var item = _converter.Convert(name, instance);
            this.Items.Add(item);
        }

        public void CollectDictionary<Value>(string name, Dictionary<string, Value> instance)
        {
            var keys = instance.Keys.ToArray();
            foreach (var key in keys)
            {
                if (instance[key] == null)
                    instance.Remove(key);
            }
            Collect(name, instance);
        }
    }
}
