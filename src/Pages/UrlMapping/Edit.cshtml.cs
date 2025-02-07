using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shorten.Areas.Domain;

namespace Shorten.Pages_UrlMapping
{
    public class EditModel : PageModel
    {
        private readonly Shorten.Areas.Domain.ShortenContext _context;

        public EditModel(Shorten.Areas.Domain.ShortenContext context)
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

            var urlmapping =  await _context.UrlMappings.FirstOrDefaultAsync(m => m.Id == id);
            if (urlmapping == null)
            {
                return NotFound();
            }
            UrlMapping = urlmapping;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(UrlMapping).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UrlMappingExists(UrlMapping.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool UrlMappingExists(int id)
        {
            return _context.UrlMappings.Any(e => e.Id == id);
        }
    }
}
