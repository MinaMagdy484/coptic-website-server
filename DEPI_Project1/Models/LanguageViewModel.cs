using Microsoft.AspNetCore.Mvc.Rendering;
    public class LanguageViewModel
    {
        public string SelectedLanguage { get; set; }
        public IEnumerable<SelectListItem> Languages { get; set; }
    }

