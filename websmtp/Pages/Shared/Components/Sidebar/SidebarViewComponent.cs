using Microsoft.AspNetCore.Mvc;

public class SidebarViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        return View();
    }
}