using Microsoft.AspNetCore.Mvc.Filters;
using InvoiceManagement.Services;

namespace InvoiceManagement.Filters
{
    public class CurrencyViewDataFilter : IAsyncActionFilter
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyViewDataFilter(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Get currency settings before the action executes
            var currencySettings = await _currencyService.GetCurrencySettingsAsync();
            
            // Add to ViewData so it's available in all views
            if (context.Controller is Microsoft.AspNetCore.Mvc.Controller controller)
            {
                controller.ViewData["CurrencySymbol"] = currencySettings.Symbol;
                controller.ViewData["CurrencyCode"] = currencySettings.Code;
                controller.ViewData["CurrencyName"] = currencySettings.Name;
                controller.ViewData["CurrencySettings"] = currencySettings;
            }

            await next();
        }
    }
}

