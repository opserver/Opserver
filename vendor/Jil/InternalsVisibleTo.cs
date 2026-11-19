#if !STRONG_NAME
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("JilTests")]
[assembly: InternalsVisibleTo("Experiments")]
[assembly: InternalsVisibleTo("JilUnionConfigLookupTypes")]
#endif