using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ManufacturingCostManagement.Web.Localization
{
    public static class HtmlExtensions
    {
        public static string T(this IHtmlHelper html, string key) => AppText.Get(key);
        public static string T(this IHtmlHelper html, string key, params object[] args)
            => string.Format(AppText.Get(key), args);
        public static HtmlString Th(this IHtmlHelper html, string key) => new HtmlString(AppText.Get(key));
    }
}
