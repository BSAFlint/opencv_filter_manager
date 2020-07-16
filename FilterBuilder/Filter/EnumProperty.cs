using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FilterBuilder.Filter
{
    public class EnumProperty
    {
        public Dictionary<string, int> Values { get; private set; } = new Dictionary<string, int>();

        public int Value { get; private set; }

        public string Description { get; set; }

        public EnumProperty(Type enumType)
        {
            string[] names = Enum.GetNames(enumType);
            var values = (int[])Enum.GetValues(enumType);

            for (int n = 0; n < names.Length; n++)
                Values[names[n]] = values[n];

            Value = values[0];
        }

        //Нам нужно добавить имена фильтров, как особый вид параметра source 
        // имена будет составлять GetAddedNames графа, и он их составляет только на список который уже существует на момент создаия параметров
        // так что их не нужно перетряхивать
        // значения будут только как ID, потому сами обьекты стоит получать
        public EnumProperty(string[] names)
        {
            for(int n = 0; n < names.Length; n++)
                Values[names[n]] = n;

            Value = 0;
        }


        public void Set(string key)
        {
            if (Values.ContainsKey(key))
                Value = Values[key];
        }

        public override string ToString()
        {
            foreach (var el in Values)
                if (el.Value == Value)
                    return el.Key;

            return "unknow_";
        }
    }
}
