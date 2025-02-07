using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shorten.Areas.Domain;

namespace Shorten.Pages_UrlMapping
{
    public class CreateModel : PageModel
    {
        private readonly Shorten.Areas.Domain.ShortenContext _context;

        public CreateModel(Shorten.Areas.Domain.ShortenContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public UrlMapping UrlMapping { get; set; } = default!;

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.UrlMappings.Add(UrlMapping);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
