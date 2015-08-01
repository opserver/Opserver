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
            string name = prof == null ? null : ("BindModel:" + bindingContext.ModelName);
            using (prof.Step(name))
            {
                Type modelType = bindingContext.ModelType;
                if (modelType == formCollectionType || bindingContext.ModelMetadata.IsComplexType)
                {
                    return base.BindModel(controllerContext, bindingContext);
                }
                else
                {
                    try
                    {
                        ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
                        object result = valueProviderResult?.RawValue;
                        if (result == null) return null;
                        Array arr = result as Array;

                        if (arr != null && !modelType.IsArray && arr.Length == 1)
                        {
                            result = arr.GetValue(0);
                        }
                        if (result != null && result.GetType() != modelType)
                        {

                            Type underlyingType = Nullable.GetUnderlyingType(modelType) ?? modelType;
                            if (result is string)
                            {
                                string s = (string)result;
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
                                catch
                                { }// best attempt only
                            }
                            result = Convert.ChangeType(result, underlyingType, valueProviderResult.Culture);
                        }
                        return result;
                    }
                    catch // (Exception ex)
                    {
                        // GlobalApplication.LogException(ex);
                        return base.BindModel(controllerContext, bindingContext);
                    }
                }

            }
        }
    }
}