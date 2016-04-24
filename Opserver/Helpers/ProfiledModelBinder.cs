using System;
using System.Web.Mvc;
using StackExchange.Profiling;

namespace StackExchange.Opserver.Helpers
{
    public class ProfiledModelBinder : DefaultModelBinder
    {
        private static readonly Type formCollectionType = typeof(FormCollection);
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var prof = MiniProfiler.Current;
            string name = prof == null ? null : "BindModel:" + bindingContext.ModelName;
            using (prof.Step(name))
            {
                Type modelType = bindingContext.ModelType;
                if (modelType == formCollectionType || bindingContext.ModelMetadata.IsComplexType)
                {
                    return base.BindModel(controllerContext, bindingContext);
                }

                try
                {
                    ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
                    object result = valueProviderResult?.RawValue;
                    if (result == null) return null;
                    var arr = result as Array;

                    if (arr != null && !modelType.IsArray && arr.Length == 1)
                    {
                        result = arr.GetValue(0);
                    }
                    if (result == null || result.GetType() == modelType)
                        return result;

                    var underlyingType = Nullable.GetUnderlyingType(modelType) ?? modelType;
                    var s = result as string;
                    if (s != null)
                    {
                        try
                        {
                            if (underlyingType.IsEnum)
                            {
                                // note this early exit
                                return Enum.Parse(underlyingType, s, true);
                            }
                            if (underlyingType == typeof(bool))
                            {
                                // note early exit
                                if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
                                if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
                            }
                        }
                        catch { /* best attempt only */ }
                    }
                    result = Convert.ChangeType(result, underlyingType, valueProviderResult.Culture);
                    return result;
                }
                catch
                {
                    return base.BindModel(controllerContext, bindingContext);
                }
            }
        }
    }
}