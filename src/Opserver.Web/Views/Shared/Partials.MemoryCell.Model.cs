using Opserver.Data.SQL;

namespace Opserver.Views.Shared
{
    public class PartialsMemoryCellModel
    {
        public SQLInstance Instance { get; }
        public int DecimalPlaces { get; }

        public PartialsMemoryCellModel(SQLInstance instance, int decimalPlaces = 0) =>
            (Instance, DecimalPlaces) = (instance, decimalPlaces);
    }
}
