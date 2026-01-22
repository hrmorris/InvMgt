using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace InvoiceManagement.ModelBinders
{
    /// <summary>
    /// Custom model binder to handle DD/MM/YYYY date format from client
    /// </summary>
    public class DateTimeModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            DateTime dateTime;

            // Try DD/MM/YYYY format first
            if (DateTime.TryParseExact(value, "dd/MM/yyyy", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.None, 
                out dateTime))
            {
                bindingContext.Result = ModelBindingResult.Success(dateTime);
                return Task.CompletedTask;
            }

            // Try DD/MM/YYYY HH:mm:ss format
            if (DateTime.TryParseExact(value, "dd/MM/yyyy HH:mm:ss", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.None, 
                out dateTime))
            {
                bindingContext.Result = ModelBindingResult.Success(dateTime);
                return Task.CompletedTask;
            }

            // Try YYYY-MM-DD format (ISO/default HTML date input)
            if (DateTime.TryParseExact(value, "yyyy-MM-dd", 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.None, 
                out dateTime))
            {
                bindingContext.Result = ModelBindingResult.Success(dateTime);
                return Task.CompletedTask;
            }

            // Try general parsing as fallback
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
            {
                bindingContext.Result = ModelBindingResult.Success(dateTime);
                return Task.CompletedTask;
            }

            // If parsing fails
            bindingContext.ModelState.TryAddModelError(
                modelName,
                $"Invalid date format. Please use DD/MM/YYYY format.");

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Model binder provider for DateTime types
    /// </summary>
    public class DateTimeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(DateTime) || 
                context.Metadata.ModelType == typeof(DateTime?))
            {
                return new DateTimeModelBinder();
            }

            return null;
        }
    }
}

