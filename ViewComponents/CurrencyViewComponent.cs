using Microsoft.AspNetCore.Mvc;
using InvoiceManagement.Services;

namespace InvoiceManagement.ViewComponents
{
    public class CurrencyViewComponent : ViewComponent
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyViewComponent(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public async Task<IViewComponentResult> InvokeAsync(decimal amount)
        {
            var formattedAmount = await _currencyService.FormatAmountAsync(amount);
            return Content(formattedAmount);
        }
    }

    public class CurrencySymbolViewComponent : ViewComponent
    {
        private readonly ICurrencyService _currencyService;

        public CurrencySymbolViewComponent(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var symbol = await _currencyService.GetCurrencySymbolAsync();
            return Content(symbol);
        }
    }
}

