using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace RecordParser.Generic
{
    public class MappingConfiguration
    {
        public MemberExpression prop { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public Expression fmask { get; set; }
        public Type type { get; set; }

        private string _mask;
        public string mask
        {
            get => _mask;
            set
            {
                _mask = value;
                fmask = null;

                var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

                if (underlyingType == typeof(string))
                {
                    fmask = ff(text => text.Trim());
                }
                else if (_mask != null)
                {
                    if (underlyingType == typeof(decimal))
                    {
                        var precision = _mask.Length - _mask.LastIndexOf(".") - 1;
                        fmask = ff(text => decimal.Parse(text) / (decimal)Math.Pow(10, precision));
                    }
                    else if (underlyingType == typeof(DateTime))
                    {
                        fmask = ff(text => DateTime.ParseExact(text, _mask, CultureInfo.InvariantCulture));
                    }
                }

                Expression ff<T>(Expression<Func<string, T>> ex) => ex;
            }
        }
    }
}
