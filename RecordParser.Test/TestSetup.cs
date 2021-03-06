using System.Globalization;

namespace RecordParser.Test
{
    public class TestSetup
    {
        public TestSetup()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }
    }
}
