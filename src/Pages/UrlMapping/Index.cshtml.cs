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
    public class IndexModel : PageModel
    {
        private readonly Shorten.Areas.Domain.ShortenContext _context;

        public IndexModel(Shorten.Areas.Domain.ShortenContext context)
        {
            _context = context;
        }

        public IList<UrlMapping> UrlMapping { get;set; } = default!;

        public async Task OnGetAsync()
        {
            UrlMapping = await _context.UrlMappings.ToListAsync();
        }
    }
}
