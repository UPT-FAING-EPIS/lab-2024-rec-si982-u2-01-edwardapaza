using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Shorten.Areas.Domain;

namespace Shorten.Pages_UrlMapping
{
    public class DeleteModel : PageModel
    {
        private readonly Shorten.Areas.Domain.ShortenContext _context;

        public DeleteModel(Shorten.Areas.Domain.ShortenContext context)
        {
            _context = context;
        }

        [BindProperty]
        public UrlMapping UrlMapping { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var urlmapping = await _context.UrlMappings.FirstOrDefaultAsync(m => m.Id == id);

            if (urlmapping == null)
            {
                return NotFound();
            }
            else
            {
                UrlMapping = urlmapping;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var urlmapping = await _context.UrlMappings.FindAsync(id);
            if (urlmapping != null)
            {
                UrlMapping = urlmapping;
                _context.UrlMappings.Remove(UrlMapping);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
