using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using JonyBalls3.Services;
using System.Security.Claims;

namespace JonyBalls3.Filters
{
    public class ContractorStatusFilter : IAsyncActionFilter
    {
        private readonly ContractorService _contractorService;

        public ContractorStatusFilter(ContractorService contractorService)
        {
            _contractorService = contractorService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.Controller is Controller controller)
            {
                var user = context.HttpContext.User;
                if (user.Identity?.IsAuthenticated == true)
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var contractor = await _contractorService.GetContractorByUserIdAsync(userId);
                        controller.ViewBag.IsContractor = contractor != null;
                    }
                    else
                    {
                        controller.ViewBag.IsContractor = false;
                    }
                }
                else
                {
                    controller.ViewBag.IsContractor = false;
                }
            }

            await next();
        }
    }
}
