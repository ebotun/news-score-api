using Microsoft.EntityFrameworkCore;
using NewsScoreApi.Models;

namespace NewsScoreApi.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<NewsScoreRange> NewsScoreRanges { get; set; }
}

